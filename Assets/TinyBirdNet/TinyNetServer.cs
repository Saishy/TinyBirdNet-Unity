using UnityEngine;
using System.Collections;
using LiteNetLib;
using LiteNetLib.Utils;
using TinyBirdUtils;
using TinyBirdNet.Messaging;

namespace TinyBirdNet {

	/// <summary>
	/// Represents the Scene of a server.
	/// </summary>
	/// <seealso cref="TinyBirdNet.TinyNetScene" />
	public class TinyNetServer : TinyNetScene {

		/// <summary>
		/// The singleton instance.
		/// </summary>
		public static TinyNetServer instance;

		/// <inheritdoc />
		public override string TYPE { get { return "SERVER"; } }

		//static TinyNetObjectStateUpdate recycleStateUpdateMessage = new TinyNetObjectStateUpdate();

		/// <summary>
		/// A writer to be used when writing state updates.
		/// </summary> 
		protected NetDataWriter _serializeWriter = new NetDataWriter();

		/// <summary>
		/// Initializes a new instance of the <see cref="TinyNetServer"/> class.
		/// </summary>
		public TinyNetServer() : base() {
			instance = this;
		}

		/// <inheritdoc />
		protected override void RegisterMessageHandlers() {
			base.RegisterMessageHandlers();

			TinyNetGameManager.instance.RegisterMessageHandlersServer();

			RegisterHandlerSafe(TinyNetMsgType.Connect, OnConnectMessage);
			RegisterHandlerSafe(TinyNetMsgType.Ready, OnClientReadyMessage);
			RegisterHandlerSafe(TinyNetMsgType.Input, OnPlayerInputMessage);
			//RegisterHandlerSafe(TinyNetMsgType.LocalPlayerTransform, NetworkTransform.HandleTransform);
			//RegisterHandlerSafe(TinyNetMsgType.LocalChildTransform, NetworkTransformChild.HandleChildTransform);
			RegisterHandlerSafe(TinyNetMsgType.RequestAddPlayer, OnRequestAddPlayerMessage);
			RegisterHandlerSafe(TinyNetMsgType.RequestRemovePlayer, OnRequestRemovePlayerMessage);
			//RegisterHandlerSafe(TinyNetMsgType.Animation, NetworkAnimator.OnAnimationServerMessage);
			//RegisterHandlerSafe(TinyNetMsgType.AnimationParameters, NetworkAnimator.OnAnimationParametersServerMessage);
			//RegisterHandlerSafe(TinyNetMsgType.AnimationTrigger, NetworkAnimator.OnAnimationTriggerServerMessage);
		}

		/// <inheritdoc />
		public override void TinyNetUpdate() {
			foreach (var item in _localIdentityObjects) {
				item.Value.TinyNetUpdate();
			}

			SendStateUpdatesToAll();
		}

		/// <summary>
		/// Starts the server.
		/// </summary>
		/// <param name="port">The port.</param>
		/// <param name="maxNumberOfPlayers">The maximum number of players.</param>
		/// <returns></returns>
		public virtual bool StartServer(int port, int maxNumberOfPlayers) {
			if (_netManager != null) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("[" + TYPE + "] StartServer() called multiple times."); }
				return false;
			}

			_netManager = new NetManager(this, maxNumberOfPlayers);
			_netManager.Start(port);

			ConfigureNetManager(true);

			if (TinyNetLogLevel.logDev) { TinyLogger.Log("[" + TYPE + "] Started server at port: " + port + " with maxNumberOfPlayers: " + maxNumberOfPlayers); }

			return true;
		}

		/// <inheritdoc />
		protected override TinyNetConnection CreateTinyNetConnection(NetPeer peer) {
			TinyNetConnection tinyConn;

			if ( ((string)peer.Tag).Equals(TinyNetGameManager.ApplicationGUIDString) ) {
				tinyConn = new TinyNetLocalConnectionToClient(peer);
				if (TinyNetLogLevel.logDev) { TinyLogger.Log("TinyNetServer::CreateTinyNetConnection created new TinyNetLocalConnectionToClient."); }
			} else {
				tinyConn = new TinyNetConnection(peer);
				if (TinyNetLogLevel.logDev) { TinyLogger.Log("TinyNetServer::CreateTinyNetConnection created new TinyNetConnection."); }
			}

			tinyNetConns.Add(tinyConn);

			return tinyConn;
		}

		//============ TinyNetEvents ========================//

		/// <summary>
		/// Called when a connection message is received.
		/// </summary>
		/// <param name="netMsg">The net message.</param>
		protected virtual void OnConnectMessage(TinyNetMessageReader netMsg) {
			if (TinyNetGameManager.instance.isClient && TinyNetClient.instance.connToHost.ConnectId == netMsg.tinyNetConn.ConnectId) {
				return;
			}

			if (TinyNetGameManager.networkSceneName != null && TinyNetGameManager.networkSceneName != "") {
				TinyNetStringMessage msg = new TinyNetStringMessage(TinyNetGameManager.networkSceneName);
				msg.msgType = TinyNetMsgType.Scene;
				netMsg.tinyNetConn.Send(msg, DeliveryMethod.ReliableOrdered);
			}
		}

		//============ Static Methods =======================//

		/// <summary>
		/// Just a shortcut to SpawnObject(obj)
		/// </summary>
		/// <param name="obj">The object to spawn.</param>
		static public void Spawn(GameObject obj) {
			instance.SpawnObject(obj);
		}

		static bool GetTinyNetIdentity(GameObject go, out TinyNetIdentity view) {
			view = go.GetComponent<TinyNetIdentity>();

			if (view == null) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("TinyBirdNet failure. GameObject doesn't have TinyNetIdentity."); }
				return false;
			}

			return true;
		}

		//============ Object Networking ====================//

		/// <summary>
		/// Spawns the object with client authority.
		/// </summary>
		/// <param name="obj">The object to spawn.</param>
		/// <param name="conn">The connection that will own it.</param>
		/// <returns></returns>
		public bool SpawnWithClientAuthority(GameObject obj, TinyNetConnection conn) {
			Spawn(obj);

			var tni = obj.GetComponent<TinyNetIdentity>();
			if (tni == null) {
				// spawning the object failed.
				return false;
			}

			return tni.AssignClientAuthority(conn);
		}

		/// <summary>
		/// Spawns the object.
		/// </summary>
		/// <param name="obj">The object to spawn.</param>
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

			objTinyNetIdentity.OnNetworkCreate();

			objTinyNetIdentity.OnStartServer(false);

			AddTinyNetIdentityToList(objTinyNetIdentity);

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("SpawnObject instance ID " + objTinyNetIdentity.TinyInstanceID + " asset GUID " + objTinyNetIdentity.assetGUID); }

			// Using ShowObjectToConnection prevents the server from sending spawn messages of objects that are already spawned.
			for (int i = 0; i < tinyNetConns.Count; i++) {
				tinyNetConns[i].ShowObjectToConnection(objTinyNetIdentity);
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
			msg.networkID = netIdentity.TinyInstanceID.NetworkID;
			msg.assetIndex = TinyNetGameManager.instance.GetAssetIdFromAssetGUID(netIdentity.assetGUID);
			msg.position = netIdentity.transform.position;
			msg.frameTick = CurrentGameTick;

			// Include state of TinyNetObjects.
			s_recycleWriter.Reset();
			netIdentity.TinySerialize(s_recycleWriter, true);

			if (s_recycleWriter.Length > 0) {
				msg.initialState = s_recycleWriter.CopyData();
			}

			if (targetConn != null) {
				SendMessageByChannelToTargetConnection(msg, DeliveryMethod.ReliableOrdered, targetConn);
			} else {
				SendMessageByChannelToAllConnections(msg, DeliveryMethod.ReliableOrdered);
			}
		}

		// Destroy methods

		/// <summary>
		/// Unspawn an object.
		/// </summary>
		/// <param name="obj">The object to unspawn.</param>
		public void UnSpawnObject(GameObject obj) {
			if (obj == null) {
				if (TinyNetLogLevel.logDev) { TinyLogger.Log("NetworkServer UnspawnObject is null"); }
				return;
			}

			TinyNetIdentity objTinyNetIdentity;
			if (!GetTinyNetIdentity(obj, out objTinyNetIdentity)) {
				return;
			}

			UnSpawnObject(objTinyNetIdentity);
		}

		/// <summary>
		/// Unspawn an object.
		/// </summary>
		/// <param name="obj">The <see cref="TinyNetIdentity"/> to unspawn.</param>
		public void UnSpawnObject(TinyNetIdentity tni) {
			DestroyObject(tni, false);
		}

		/// <summary>
		/// Destroys the object.
		/// </summary>
		/// <param name="obj">The object to destroy.</param>
		public void DestroyObject(GameObject obj) {
			if (obj == null) {
				if (TinyNetLogLevel.logDev) { TinyLogger.Log("NetworkServer DestroyObject is null"); }
				return;
			}

			TinyNetIdentity objTinyNetIdentity;
			if (!GetTinyNetIdentity(obj, out objTinyNetIdentity)) {
				return;
			}

			DestroyObject(objTinyNetIdentity, true);
		}

		/// <summary>
		/// Destroys the object.
		/// </summary>
		/// <param name="tni">The <see cref="TinyNetIdentity"/> of the object.</param>
		/// <param name="destroyServerObject">if set to <c>true</c> destroy the object on server too.</param>
		public void DestroyObject(TinyNetIdentity tni, bool destroyServerObject) {
			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("DestroyObject instance:" + tni.TinyInstanceID); }

			/*if (_localIdentityObjects.ContainsKey(tni.TinyInstanceID.NetworkID)) {
				_localIdentityObjects.Remove(tni.TinyInstanceID.NetworkID);
			}*/
			RemoveTinyNetIdentityFromList(tni);

			if (tni.ConnectionToOwnerClient != null) {
				tni.ConnectionToOwnerClient.RemoveOwnedObject(tni);
			}

			TinyNetObjectDestroyMessage msg = new TinyNetObjectDestroyMessage();
			msg.networkID = tni.TinyInstanceID.NetworkID;
			SendMessageByChannelToAllObserversOf(tni, msg, DeliveryMethod.ReliableOrdered);

			for (int i = 0; i < tinyNetConns.Count; i++) {
				tinyNetConns[i].HideObjectToConnection(tni, true);
			}

			/*if (TinyNetGameManager.instance.isListenServer) {
				tni.OnNetworkDestroy();
			}*/

			tni.OnNetworkDestroy();
			// when unspawning, dont destroy the server's object
			if (destroyServerObject) {
				Object.Destroy(tni.gameObject);
			}

			tni.ReceiveNetworkID(new TinyNetworkID(-1));
		}

		/// <summary>
		/// Sends the RPC to the client owner of an object.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <param name="rpcMethodIndex">Index of the RPC method.</param>
		/// <param name="iObj">The object.</param>
		public void SendRPCToClientOwner(NetDataWriter stream, int rpcMethodIndex, ITinyNetComponent iObj) {
			//TODO: Pack rpc messages
			var msg = new TinyNetRPCMessage();

			msg.networkID = iObj.TinyInstanceID.NetworkID;
			msg.componentID = iObj.TinyInstanceID.ComponentID;
			msg.rpcMethodIndex = rpcMethodIndex;
			msg.frameTick = CurrentGameTick;
			msg.parameters = stream.Data;

			TinyNetIdentity tni = GetTinyNetIdentityByNetworkID(iObj.TinyInstanceID.NetworkID);

			SendMessageByChannelToTargetConnection(msg, DeliveryMethod.ReliableOrdered, tni.ConnectionToOwnerClient);
		}

		/// <summary>
		/// Sends the RPC to all clients.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <param name="rpcMethodIndex">Index of the RPC method.</param>
		/// <param name="iObj">The object.</param>
		public void SendRPCToAllClients(NetDataWriter stream, int rpcMethodIndex, ITinyNetComponent iObj) {
			//TODO: Pack rpc messages
			var msg = new TinyNetRPCMessage();

			msg.networkID = iObj.TinyInstanceID.NetworkID;
			msg.componentID = iObj.TinyInstanceID.ComponentID;
			msg.rpcMethodIndex = rpcMethodIndex;
			msg.frameTick = CurrentGameTick;
			msg.parameters = stream.Data;

			TinyNetIdentity tni = GetTinyNetIdentityByNetworkID(iObj.TinyInstanceID.NetworkID);

			SendMessageByChannelToAllObserversOf(tni, msg, DeliveryMethod.ReliableOrdered);
		}

		//============ TinyNetMessages Networking ===========//

		// TODO: Sepparates objects into desired send type
		/// <summary>
		/// Sends the state updates for all observing objects of each connection.
		/// </summary>
		public virtual void SendStateUpdatesToAll() {
			s_recycleWriter.Reset();

			s_recycleWriter.Put(TinyNetMsgType.StateUpdate);
			s_recycleWriter.Put(CurrentGameTick);

			for (int i = 0; i < tinyNetConns.Count; i++) {
				foreach (TinyNetIdentity tNetId in tinyNetConns[i].ObservingNetObjects) {
					s_recycleWriter.Put(tNetId.TinyInstanceID.NetworkID);

					_serializeWriter.Reset();
					tNetId.TinySerialize(_serializeWriter, false);

					s_recycleWriter.Put(_serializeWriter.Length);
					s_recycleWriter.Put(_serializeWriter.Data);
				}

				tinyNetConns[i].Send(s_recycleWriter, DeliveryMethod.ReliableOrdered);
			}
		}

		/*
		public virtual void SendStateUpdateToAllConnections(TinyNetBehaviour netBehaviour, DeliveryMethod sendOptions) {
			recycleWriter.Reset();

			recycleWriter.Put(TinyNetMsgType.StateUpdate);
			recycleWriter.Put(netBehaviour.NetworkID);

			netBehaviour.TinySerialize(recycleWriter, false);

			for (int i = 0; i < tinyNetConns.Count; i++) {
				tinyNetConns[i].Send(recycleWriter, sendOptions);
			}
		}*/

		//============ TinyNetMessages Handlers =============//

		// default ready handler.
		/// <summary>
		/// Called when we receive a client ready message.
		/// </summary>
		/// <param name="netMsg">The net MSG.</param>
		void OnClientReadyMessage(TinyNetMessageReader netMsg) {
			if (TinyNetLogLevel.logDev) { TinyLogger.Log("Default handler for ready message from " + netMsg.tinyNetConn); }

			SetClientReady(netMsg.tinyNetConn);
		}

		//============ Clients Functions ====================//

		/// <summary>
		/// Sets the client as ready.
		/// </summary>
		/// <param name="conn">The connection.</param>
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
			SendMessageByChannelToTargetConnection(msg, DeliveryMethod.ReliableOrdered, conn);

			foreach (TinyNetIdentity tinyNetId in _localIdentityObjects.Values) {

				if (tinyNetId == null) {
					if (TinyNetLogLevel.logWarn) { TinyLogger.LogWarning("Invalid object found in server local object list (null TinyNetIdentity)."); }
					continue;
				}
				if (!tinyNetId.gameObject.activeSelf) {
					continue;
				}

				if (TinyNetLogLevel.logDebug) { TinyLogger.Log("Sending spawn message for current server objects name='" + tinyNetId.gameObject.name + "' netId=" + tinyNetId.TinyInstanceID); }

				conn.ShowObjectToConnection(tinyNetId);
			}

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("Spawning objects for conn " + conn.ConnectId + " finished"); }

			msg.state = 1; //We finished spamming the spawn messages!
			SendMessageByChannelToTargetConnection(msg, DeliveryMethod.ReliableOrdered, conn);
		}

		/// <summary>
		/// Sets all clients as not ready.
		/// </summary>
		public void SetAllClientsNotReady() {
			for (int i = 0; i < tinyNetConns.Count; i++) {
				var conn = tinyNetConns[i];

				if (conn != null) {
					SetClientNotReady(conn);
				}
			}
		}

		/// <summary>
		/// Sets the client as not ready.
		/// </summary>
		/// <param name="conn">The connection.</param>
		void SetClientNotReady(TinyNetConnection conn) {
			if (conn.isReady) {
				if (TinyNetLogLevel.logDebug) { TinyLogger.Log("PlayerNotReady " + conn); }

				conn.isReady = false;

				TinyNetNotReadyMessage msg = new TinyNetNotReadyMessage();
				SendMessageByChannelToTargetConnection(msg, DeliveryMethod.ReliableOrdered, conn);
			}
		}

		//============ Connections Methods ==================//

		/// <summary>
		/// Always call this from a TinyNetConnection ShowObjectToConnection, or you will have sync issues.
		/// </summary>
		/// <param name="tinyNetId">The tiny net identifier.</param>
		/// <param name="conn">The connection.</param>
		public void ShowForConnection(TinyNetIdentity tinyNetId, TinyNetConnection conn) {
			if (conn.isReady) {
				instance.SendSpawnMessage(tinyNetId, conn);
			}
		}

		/// <summary>
		/// Always call this from a TinyNetConnection RemoveFromVisList, or you will have sync issues.
		/// </summary>
		/// <param name="tinyNetId">The tiny net identifier.</param>
		/// <param name="conn">The connection.</param>
		public void HideForConnection(TinyNetIdentity tinyNetId, TinyNetConnection conn) {
			TinyNetObjectHideMessage msg = new TinyNetObjectHideMessage();
			msg.networkID = tinyNetId.TinyInstanceID.NetworkID;

			SendMessageByChannelToTargetConnection(msg, DeliveryMethod.ReliableOrdered, conn);
		}

		//============ Objects Methods ======================//

		/// <summary>
		/// Spawns all TinyNetIdentity objects in the scene.
		/// </summary>
		/// <returns>
		/// This actually always return true?
		/// </returns>
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
						if (TinyNetLogLevel.logError) { TinyLogger.LogError("Log this? Something is wrong if this happens?"); }
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

		/// <summary>
		/// Called when the server scene is changed.
		/// </summary>
		/// <param name="sceneName">Name of the scene.</param>
		public virtual void OnServerSceneChanged(string sceneName) {
		}

		//============ Players Methods ======================//

		/// <summary>
		/// Called when a <see cref="TinyNetInputMessage"/> is received.
		/// </summary>
		/// <param name="netMsg">The net message.</param>
		void OnPlayerInputMessage(TinyNetMessageReader netMsg) {
			netMsg.tinyNetConn.GetPlayerInputMessage(netMsg);
		}

		/// <summary>
		/// Called when a <see cref="TinyNetRequestAddPlayerMessage"/> is received.
		/// </summary>
		/// <param name="netMsg">The net message.</param>
		void OnRequestAddPlayerMessage(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetRequestAddPlayerMessage);

			if (s_TinyNetRequestAddPlayerMessage.amountOfPlayers <= 0) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("OnRequestAddPlayerMessage() called with amountOfPlayers <= 0"); }
				return;
			}

			// Check here if you should create another player controller for that connection.

			int playerId = TinyNetGameManager.instance.NextPlayerID;//netMsg.tinyNetConn.playerControllers.Count;

			AddPlayerControllerToConnection(netMsg.tinyNetConn, playerId);

			// Tell the origin client to add them too!
			s_TinyNetAddPlayerMessage.playerControllerId = (short)playerId;
			SendMessageByChannelToTargetConnection(s_TinyNetAddPlayerMessage, DeliveryMethod.ReliableOrdered, netMsg.tinyNetConn);
		}

		/// <summary>
		/// Called when a <see cref="TinyNetRequestRemovePlayerMessage"/> is received.
		/// </summary>
		/// <param name="netMsg">The net message.</param>
		void OnRequestRemovePlayerMessage(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetRequestRemovePlayerMessage);

			if (s_TinyNetRequestRemovePlayerMessage.playerControllerId <= 0) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("OnRequestRemovePlayerMessage() called with playerControllerId <= 0"); }
				return;
			}

			RemovePlayerControllerFromConnection(netMsg.tinyNetConn, s_TinyNetRequestRemovePlayerMessage.playerControllerId);

			// Tell the origin client to remove them too!
			s_TinyNetRemovePlayerMessage.playerControllerId = s_TinyNetRequestRemovePlayerMessage.playerControllerId;
			SendMessageByChannelToTargetConnection(s_TinyNetRemovePlayerMessage, DeliveryMethod.ReliableOrdered, netMsg.tinyNetConn);
		}

		/// <summary>
		/// Gets the player controller that have the given identifier.
		/// </summary>
		/// <param name="playerControllerId">The player controller identifier.</param>
		/// <returns></returns>
		public TinyNetPlayerController GetPlayerController(int playerControllerId) {
			TinyNetPlayerController tPC = null;

			for (int i = 0; i < tinyNetConns.Count; i++) {
				if (tinyNetConns[i].GetPlayerController((short)playerControllerId, out tPC)) {
					break;
				}
			}

			return tPC;
		}

		/// <summary>
		/// Gets the player controller from a specific connection.
		/// </summary>
		/// <param name="connId">The connection identifier.</param>
		/// <param name="playerControllerId">The player controller identifier.</param>
		/// <returns></returns>
		public TinyNetPlayerController GetPlayerControllerFromConnection(long connId, int playerControllerId) {
			return GetTinyNetConnection(connId).GetPlayerController((short)playerControllerId);
		}
	}
}
