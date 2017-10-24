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

		// static message objects to avoid runtime-allocations
		static TinyNetRemovePlayerMessage s_TinyNetRemovePlayerMessage = new TinyNetRemovePlayerMessage();

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
			RegisterHandlerSafe(TinyNetMsgType.RemovePlayer, OnRemovePlayerMessage);
			//RegisterHandlerSafe(TinyNetMsgType.Animation, NetworkAnimator.OnAnimationServerMessage);
			//RegisterHandlerSafe(TinyNetMsgType.AnimationParameters, NetworkAnimator.OnAnimationParametersServerMessage);
			//RegisterHandlerSafe(TinyNetMsgType.AnimationTrigger, NetworkAnimator.OnAnimationTriggerServerMessage);
		}

		public override void TinyNetUpdate() {
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

		//============ Static Methods =======================//

		static public void Spawn(GameObject obj) {
			instance.SpawnObject(obj);
		}

		static bool GetNetworkIdentity(GameObject go, out TinyNetIdentity view) {
			view = go.GetComponent<TinyNetIdentity>();

			if (view == null) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("UNET failure. GameObject doesn't have NetworkIdentity."); }
				return false;
			}

			return true;
		}

		//============ Object Networking ====================//

		void SpawnObject(GameObject obj) {
			if (!isRunning) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("SpawnObject for " + obj + ", NetworkServer is not active. Cannot spawn objects without an active server."); }
				return;
			}

			TinyNetIdentity objNetworkIdentity;

			if (!GetNetworkIdentity(obj, out objNetworkIdentity)) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("SpawnObject " + obj + " has no TinyNetIdentity. Please add a TinyNetIdentity to " + obj); }
				return;
			}

			objNetworkIdentity.OnStartServer(false);

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("SpawnObject instance ID " + objNetworkIdentity.NetworkID + " asset GUID " + objNetworkIdentity.assetGUID); }

			//objNetworkIdentity.RebuildObservers(true);
			SendSpawnMessage(objNetworkIdentity, null);
		}

		/// <summary>
		/// Send a spawn message.
		/// </summary>
		/// <param name="netIdentity">The TinyNetIdentity of the object to spawn.</param>
		/// <param name="targetPeer">If null, send to all connected peers.</param>
		public void SendSpawnMessage(TinyNetIdentity netIdentity, TinyNetConnection targetConn) {
			if (netIdentity.ServerOnly) {
				return;
			}

			TinyNetObjectSpawnMessage msg = new TinyNetObjectSpawnMessage();
			msg.networkID = netIdentity.NetworkID;
			msg.assetId = TinyNetGameManager.instance.GetAssetIdFromAssetGUID(netIdentity.assetGUID);
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

		// default remove player handler
		void OnRemovePlayerMessage(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetRemovePlayerMessage);

			if (RemoveTinyNetConnection(s_TinyNetRemovePlayerMessage.connectId)) {
			} else {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("Received remove player message but could not find the connectId: " + s_TinyNetRemovePlayerMessage.connectId); }
			}
		}

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
				// this is now allowed
				if (TinyNetLogLevel.logDebug) { TinyLogger.LogWarning("Ready with no player object"); }
			}

			conn.isReady = true;

			var localConnection = conn as TinyNetLocalConnectionToClient;
			if (localConnection != null) {
				if (TinyNetLogLevel.logDev) { TinyLogger.Log("NetworkServer Ready handling ULocalConnectionToClient"); }

				// Setup spawned objects for local player
				// Only handle the local objects for the first player (no need to redo it when doing more local players)
				// and don't handle player objects here, they were done above
				foreach (TinyNetIdentity tinyNetId in _localIdentityObjects.Values) {
					// Need to call OnStartClient directly here, as it's already been added to the local object dictionary
					// in the above SetLocalPlayer call
					if (tinyNetId != null && tinyNetId.gameObject != null) {
						if (!tinyNetId.isClient) {
							ShowForConnection(tinyNetId, localConnection);

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
			msg.state = 0;
			SendMessageByChannelToTargetConnection(msg, SendOptions.ReliableOrdered, conn);

			foreach (TinyNetIdentity tinyNetId in _localIdentityObjects.Values) {

				if (tinyNetId == null) {
					if (TinyNetLogLevel.logWarn) { TinyLogger.LogWarning("Invalid object found in server local object list (null NetworkIdentity)."); }
					continue;
				}
				if (!tinyNetId.gameObject.activeSelf) {
					continue;
				}

				if (TinyNetLogLevel.logDebug) { TinyLogger.Log("Sending spawn message for current server objects name='" + tinyNetId.gameObject.name + "' netId=" + tinyNetId.NetworkID); }

				ShowForConnection(tinyNetId, localConnection);
			}

			msg.state = 1;
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

		//============ Connections Functions ================//

		public void ShowForConnection(TinyNetIdentity tinyNetId, TinyNetConnection conn) {
			if (conn.isReady) {
				instance.SendSpawnMessage(tinyNetId, conn);
			}
		}

		public void HideForConnection(TinyNetIdentity tinyNetId, TinyNetConnection conn) {
			TinyNetObjectDestroyMessage msg = new TinyNetObjectDestroyMessage();
			msg.networkID = tinyNetId.NetworkID;
			SendMessageByChannelToTargetConnection(msg, SendOptions.ReliableOrdered, conn);
		}

		//============ Connections Functions ================//

		public bool SpawnObjects() {
			if (isRunning) {
				TinyNetIdentity[] uvs = Resources.FindObjectsOfTypeAll<TinyNetIdentity>();

				foreach (var uv in uvs) {
					if (uv.gameObject.hideFlags == HideFlags.NotEditable || uv.gameObject.hideFlags == HideFlags.HideAndDontSave)
						continue;

					if (uv.sceneID == 0)
						continue;

					if (TinyNetLogLevel.logDebug) { TinyLogger.Log("SpawnObjects sceneID:" + uv.sceneID + " name:" + uv.gameObject.name); }

					uv.gameObject.SetActive(true);
				}

				foreach (var uv2 in uvs) {
					if (uv2.gameObject.hideFlags == HideFlags.NotEditable || uv2.gameObject.hideFlags == HideFlags.HideAndDontSave)
						continue;

					// If not a scene object
					if (uv2.sceneID == 0)
						continue;

					// What does this mean???
					if (uv2.isServer)
						continue;

					if (uv2.gameObject == null) {
						if (TinyNetLogLevel.logDebug) { TinyLogger.LogError("Log this? Something is wrong if this happens?"); }
						continue;
					}

					Spawn(uv2.gameObject);

					// these objects are server authority - even if "localPlayerAuthority" is set on them
					uv2.ForceAuthority(true);
				}
			}
			return true;
		}

		//============ Scenes Methods =======================//

		public virtual void OnServerSceneChanged(string sceneName) {
		}
	}
}