using UnityEngine;
using System.Collections;
using LiteNetLib;
using LiteNetLib.Utils;
using TinyBirdUtils;
using TinyBirdNet.Messaging;

namespace TinyBirdNet {

	public class TinyNetServer : TinyNetScene {

		public static TinyNetServer instance;

		public override string TYPE { get { return "SERVER"; } }

		//static TinyNetObjectStateUpdate recycleStateUpdateMessage = new TinyNetObjectStateUpdate();

		public TinyNetServer() : base() {
			instance = this;
		}

		protected override void RegisterMessageHandlers() {
			base.RegisterMessageHandlers();

			RegisterHandlerSafe(TinyNetMsgType.Ready, OnClientReadyMessage);
			//RegisterHandlerSafe(TinyNetMsgType.Command, OnCommandMessage);
			//RegisterHandlerSafe(TinyNetMsgType.LocalPlayerTransform, NetworkTransform.HandleTransform);
			//RegisterHandlerSafe(TinyNetMsgType.LocalChildTransform, NetworkTransformChild.HandleChildTransform);
			RegisterHandlerSafe(TinyNetMsgType.RequestAddPlayer, OnRequestAddPlayerMessage);
			RegisterHandlerSafe(TinyNetMsgType.RequestRemovePlayer, OnRequestRemovePlayerMessage);
			//RegisterHandlerSafe(TinyNetMsgType.Animation, NetworkAnimator.OnAnimationServerMessage);
			//RegisterHandlerSafe(TinyNetMsgType.AnimationParameters, NetworkAnimator.OnAnimationParametersServerMessage);
			//RegisterHandlerSafe(TinyNetMsgType.AnimationTrigger, NetworkAnimator.OnAnimationTriggerServerMessage);
		}

		public override void TinyNetUpdate() {
			if (currentFixedFrame % TinyNetGameManager.instance.NetworkEveryXFixedFrames != 0) {
				return;
			}

			foreach (var item in _localNetObjects) {
				item.Value.TinyNetUpdate();
			}
		}

		public virtual bool StartServer(int port, int maxNumberOfPlayers) {
			if (_netManager != null) {
				TinyLogger.LogError("StartServer() called multiple times.");
				return false;
			}

			_netManager = new NetManager(this, maxNumberOfPlayers, Application.version);
			_netManager.Start(port);

			ConfigureNetManager(true);

			TinyLogger.Log("[SERVER] Started server at port: " + port + " with maxNumberOfPlayers: " + maxNumberOfPlayers);

			return true;
		}

		protected override TinyNetConnection CreateTinyNetConnection(NetPeer peer) {
			TinyNetConnection tinyConn = TinyNetGameManager.instance.isListenServer ? new TinyNetLocalConnectionToClient(peer) : new TinyNetConnection(peer);

			tinyNetConns.Add(tinyConn);

			return tinyConn;
		}

		//============ Static Methods =======================//

		/// <summary>
		/// Just a shortcut to SpawnObject(obj)
		/// </summary>
		/// <param name="obj"></param>
		static public void Spawn(GameObject obj) {
			instance.SpawnObject(obj);
		}

		static bool GetTinyNetIdentity(GameObject go, out TinyNetIdentity view) {
			view = go.GetComponent<TinyNetIdentity>();

			if (view == null) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("UNET failure. GameObject doesn't have TinyNetIdentity."); }
				return false;
			}

			return true;
		}

		//============ Object Networking ====================//

		public bool SpawnWithClientAuthority(GameObject obj, TinyNetConnection conn) {
			Spawn(obj);

			var tni = obj.GetComponent<TinyNetIdentity>();
			if (tni == null) {
				// spawning the object failed.
				return false;
			}

			return tni.AssignClientAuthority(conn);
		}

		public void SpawnObject(GameObject obj) {
			if (!isRunning) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("SpawnObject for " + obj + ", NetworkServer is not active. Cannot spawn objects without an active server."); }
				return;
			}

			TinyNetIdentity objTinyNetIdentity;

			if (!GetTinyNetIdentity(obj, out objTinyNetIdentity)) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("SpawnObject " + obj + " has no TinyNetIdentity. Please add a TinyNetIdentity to " + obj); }
				return;
			}

			objTinyNetIdentity.OnStartServer(false);

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("SpawnObject instance ID " + objTinyNetIdentity.NetworkID + " asset GUID " + objTinyNetIdentity.assetGUID); }

			//objTinyNetIdentity.RebuildObservers(true);
			//SendSpawnMessage(objTinyNetIdentity, null);
			// Using ShowObjectToConnection prevents the server from sending spawn messages of objects that are already spawned.
			foreach (TinyNetConnection conn in tinyNetConns) {
				conn.ShowObjectToConnection(objTinyNetIdentity);
			}
		}

		/// <summary>
		/// Send a spawn message.
		/// </summary>
		/// <param name="netIdentity">The TinyNetIdentity of the object to spawn.</param>
		/// <param name="targetPeer">If null, send to all connected peers.</param>
		public void SendSpawnMessage(TinyNetIdentity netIdentity, TinyNetConnection targetConn = null) {
			if (netIdentity.ServerOnly) {
				return;
			}

			TinyNetObjectSpawnMessage msg = new TinyNetObjectSpawnMessage();
			msg.networkID = netIdentity.NetworkID;
			msg.assetIndex = TinyNetGameManager.instance.GetAssetIdFromAssetGUID(netIdentity.assetGUID);
			msg.position = netIdentity.transform.position;

			// Include state of TinyNetObjects.
			recycleWriter.Reset();
			netIdentity.SerializeAllTinyNetObjects(recycleWriter);

			if (recycleWriter.Length > 0) {
				msg.initialState = recycleWriter.CopyData();
			}

			if (targetConn != null) {
				SendMessageByChannelToTargetConnection(msg, SendOptions.ReliableOrdered, targetConn);
			} else {
				SendMessageByChannelToAllConnections(msg, SendOptions.ReliableOrdered);
			}
		}

		// Destroy methods

		void UnSpawnObject(GameObject obj) {
			if (obj == null) {
				if (TinyNetLogLevel.logDev) { TinyLogger.Log("NetworkServer UnspawnObject is null"); }
				return;
			}

			TinyNetIdentity objTinyNetIdentity;
			if (!GetTinyNetIdentity(obj, out objTinyNetIdentity)) return;

			UnSpawnObject(objTinyNetIdentity);
		}

		void UnSpawnObject(TinyNetIdentity tni) {
			DestroyObject(tni, false);
		}

		void DestroyObject(GameObject obj) {
			if (obj == null) {
				if (TinyNetLogLevel.logDev) { TinyLogger.Log("NetworkServer DestroyObject is null"); }
				return;
			}

			TinyNetIdentity objTinyNetIdentity;
			if (!GetTinyNetIdentity(obj, out objTinyNetIdentity)) return;

			DestroyObject(objTinyNetIdentity, true);
		}

		void DestroyObject(TinyNetIdentity tni, bool destroyServerObject) {
			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("DestroyObject instance:" + tni.NetworkID); }

			if (_localIdentityObjects.ContainsKey(tni.NetworkID)) {
				_localIdentityObjects.Remove(tni.NetworkID);
			}

			if (tni.connectionToOwnerClient != null) {
				tni.connectionToOwnerClient.RemoveOwnedObject(tni);
			}

			TinyNetObjectDestroyMessage msg = new TinyNetObjectDestroyMessage();
			msg.networkID = tni.NetworkID;
			SendMessageByChannelToAllObserversOf(tni, msg, SendOptions.ReliableOrdered);

			if (TinyNetGameManager.instance.isListenServer) {
				tni.OnNetworkDestroy();
			}

			// when unspawning, dont destroy the server's object
			if (destroyServerObject) {
				Object.Destroy(tni.gameObject);
			}

			tni.ReceiveNetworkID(0);
		}

		public void SendRPCToClientOwner(byte[] stream, int rpcMethodIndex, ITinyNetObject iObj) {
			var msg = new TinyNetRPCMessage();

			msg.networkID = iObj.NetworkID;
			msg.rpcMethodIndex = rpcMethodIndex;
			msg.parameters = stream;
			
			SendMessageByChannelToTargetConnection(msg, SendOptions.ReliableOrdered, iObj.NetIdentity.connectionToOwnerClient);
		}

		public void SendRPCToAllCLients(byte[] stream, int rpcMethodIndex, ITinyNetObject iObj) {
			var msg = new TinyNetRPCMessage();

			msg.networkID = iObj.NetworkID;
			msg.rpcMethodIndex = rpcMethodIndex;
			msg.parameters = stream;

			SendMessageByChannelToAllObserversOf(iObj.NetIdentity, msg, SendOptions.ReliableOrdered);
		}

		//============ TinyNetMessages Networking ===========//

		public virtual void SendStateUpdateToAllConnections(TinyNetBehaviour netBehaviour, SendOptions sendOptions) {
			recycleWriter.Reset();

			recycleWriter.Put(TinyNetMsgType.StateUpdate);
			recycleWriter.Put(netBehaviour.NetworkID);

			netBehaviour.TinySerialize(recycleWriter, false);

			for (int i = 0; i < tinyNetConns.Count; i++) {
				tinyNetConns[i].Send(recycleWriter, sendOptions);
			}
		}

		//============ TinyNetMessages Handlers =============//

		// default ready handler.
		void OnClientReadyMessage(TinyNetMessageReader netMsg) {
			if (TinyNetLogLevel.logDev) { TinyLogger.Log("Default handler for ready message from " + netMsg.tinyNetConn); }

			SetClientReady(netMsg.tinyNetConn);
		}

		//============ Clients Functions ====================//

		void SetClientReady(TinyNetConnection conn) {
			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("SetClientReady for conn:" + conn.ConnectId); }

			if (conn.isReady) {
				if (TinyNetLogLevel.logDebug) { TinyLogger.Log("SetClientReady conn " + conn.ConnectId + " already ready"); }
				return;
			}

			if (conn.playerControllers.Count == 0) {
				if (TinyNetLogLevel.logDebug) { TinyLogger.LogWarning("Ready with no player object"); }
			}

			conn.isReady = true;

			// This is only in case this is a listen server.
			TinyNetLocalConnectionToClient localConnection = conn as TinyNetLocalConnectionToClient;
			if (localConnection != null) {
				if (TinyNetLogLevel.logDev) { TinyLogger.Log("NetworkServer Ready handling TinyNetLocalConnectionToClient"); }

				// Setup spawned objects for local player
				// Only handle the local objects for the first player (no need to redo it when doing more local players)
				// and don't handle player objects here, they were done above
				foreach (TinyNetIdentity tinyNetId in _localIdentityObjects.Values) {
					// Need to call OnStartClient directly here, as it's already been added to the local object dictionary
					// in the above SetLocalPlayer call
					if (tinyNetId != null && tinyNetId.gameObject != null) {
						if (!tinyNetId.isClient) {
							localConnection.ShowObjectToConnection(tinyNetId);

							if (TinyNetLogLevel.logDev) { TinyLogger.Log("LocalClient.SetSpawnObject calling OnStartClient"); }
							tinyNetId.OnStartClient();
						}
					}
				}

				return;
			}

			// Spawn/update all current server objects
			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("Spawning " + _localIdentityObjects.Count + " objects for conn " + conn.ConnectId); }

			TinyNetObjectSpawnFinishedMessage msg = new TinyNetObjectSpawnFinishedMessage();
			msg.state = 0; //State 0 means we are starting the spawn messages 'spam'.
			SendMessageByChannelToTargetConnection(msg, SendOptions.ReliableOrdered, conn);

			foreach (TinyNetIdentity tinyNetId in _localIdentityObjects.Values) {

				if (tinyNetId == null) {
					if (TinyNetLogLevel.logWarn) { TinyLogger.LogWarning("Invalid object found in server local object list (null TinyNetIdentity)."); }
					continue;
				}
				if (!tinyNetId.gameObject.activeSelf) {
					continue;
				}

				if (TinyNetLogLevel.logDebug) { TinyLogger.Log("Sending spawn message for current server objects name='" + tinyNetId.gameObject.name + "' netId=" + tinyNetId.NetworkID); }

				conn.ShowObjectToConnection(tinyNetId);
			}

			msg.state = 1; //We finished spamming the spawn messages!
			SendMessageByChannelToTargetConnection(msg, SendOptions.ReliableOrdered, conn);
		}

		public void SetAllClientsNotReady() {
			for (int i = 0; i < tinyNetConns.Count; i++) {
				var conn = tinyNetConns[i];

				if (conn != null) {
					SetClientNotReady(conn);
				}
			}
		}

		void SetClientNotReady(TinyNetConnection conn) {
			if (conn.isReady) {
				if (TinyNetLogLevel.logDebug) { TinyLogger.Log("PlayerNotReady " + conn); }

				conn.isReady = false;

				TinyNetNotReadyMessage msg = new TinyNetNotReadyMessage();
				SendMessageByChannelToTargetConnection(msg, SendOptions.ReliableOrdered, conn);
			}
		}

		//============ Connections Methods ==================//

		/// <summary>
		/// Always call this from a TinyNetConnection ShowObjectToConnection, or you will have sync issues.
		/// </summary>
		/// <param name="tinyNetId"></param>
		/// <param name="conn"></param>
		public void ShowForConnection(TinyNetIdentity tinyNetId, TinyNetConnection conn) {
			if (conn.isReady) {
				instance.SendSpawnMessage(tinyNetId, conn);
			}
		}

		/// <summary>
		/// Always call this from a TinyNetConnection RemoveFromVisList, or you will have sync issues.
		/// </summary>
		/// <param name="tinyNetId"></param>
		/// <param name="conn"></param>
		public void HideForConnection(TinyNetIdentity tinyNetId, TinyNetConnection conn) {
			TinyNetObjectHideMessage msg = new TinyNetObjectHideMessage();
			msg.networkID = tinyNetId.NetworkID;

			SendMessageByChannelToTargetConnection(msg, SendOptions.ReliableOrdered, conn);
		}

		//============ Objects Methods ======================//

		/// <summary>
		/// Spawns all TinyNetIdentity objects in the scene.
		/// </summary>
		/// <returns>This actually always return true?</returns>
		public bool SpawnAllObjects() {
			if (isRunning) {
				TinyNetIdentity[] tnis = Resources.FindObjectsOfTypeAll<TinyNetIdentity>();

				foreach (var tni in tnis) {
					if (tni.gameObject.hideFlags == HideFlags.NotEditable || tni.gameObject.hideFlags == HideFlags.HideAndDontSave)
						continue;

					if (tni.sceneID == 0)
						continue;

					if (TinyNetLogLevel.logDebug) { TinyLogger.Log("SpawnObjects sceneID:" + tni.sceneID + " name:" + tni.gameObject.name); }

					tni.gameObject.SetActive(true);
				}

				foreach (var tni2 in tnis) {
					if (tni2.gameObject.hideFlags == HideFlags.NotEditable || tni2.gameObject.hideFlags == HideFlags.HideAndDontSave)
						continue;

					// If not a scene object
					if (tni2.sceneID == 0)
						continue;

					// What does this mean???
					if (tni2.isServer)
						continue;

					if (tni2.gameObject == null) {
						if (TinyNetLogLevel.logDebug) { TinyLogger.LogError("Log this? Something is wrong if this happens?"); }
						continue;
					}

					SpawnObject(tni2.gameObject);

					// these objects are server authority - even if "localPlayerAuthority" is set on them
					tni2.ForceAuthority(true);
				}
			}

			return true;
		}

		//============ Scenes Methods =======================//

		public virtual void OnServerSceneChanged(string sceneName) {
		}

		//============ Players Methods ======================//

		void OnRequestAddPlayerMessage(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetRequestAddPlayerMessage);

			if (s_TinyNetRequestAddPlayerMessage.amountOfPlayers <= 0) {
				if (TinyNetLogLevel.logDebug) { TinyLogger.LogError("OnRequestAddPlayerMessage() called with amountOfPlayers <= 0"); }
				return;
			}

			// Check here if you should create another player controller for that connection.

			int playerId = netMsg.tinyNetConn.playerControllers.Count;

			CreatePlayerAndAdd(netMsg.tinyNetConn, playerId);

			// Tell the origin client to add them too!
			s_TinyNetAddPlayerMessage.playerControllerId = (short)playerId;
			SendMessageByChannelToTargetConnection(s_TinyNetAddPlayerMessage, SendOptions.ReliableOrdered, netMsg.tinyNetConn);
		}

		void OnRequestRemovePlayerMessage(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetRequestRemovePlayerMessage);

			if (s_TinyNetRequestRemovePlayerMessage.playerControllerId <= 0) {
				if (TinyNetLogLevel.logDebug) { TinyLogger.LogError("OnRequestRemovePlayerMessage() called with playerControllerId <= 0"); }
				return;
			}

			RemovePlayerControllerFromConnection(netMsg.tinyNetConn, s_TinyNetRequestRemovePlayerMessage.playerControllerId);

			// Tell the origin client to remove them too!
			s_TinyNetRemovePlayerMessage.playerControllerId = s_TinyNetRequestRemovePlayerMessage.playerControllerId;
			SendMessageByChannelToTargetConnection(s_TinyNetRemovePlayerMessage, SendOptions.ReliableOrdered, netMsg.tinyNetConn);
		}
	}
}
