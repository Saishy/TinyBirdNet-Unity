using UnityEngine;
using System.Collections;
using LiteNetLib;
using LiteNetLib.Utils;
using TinyBirdUtils;
using TinyBirdNet.Messaging;
using System.Collections.Generic;

namespace TinyBirdNet {

	public class TinyNetClient : TinyNetScene {

		public static TinyNetClient instance;

		public override string TYPE { get { return "CLIENT"; } }

		public static System.Action OnClientReadyEvent;

		//static TinyNetObjectStateUpdate recycleStateUpdateMessage = new TinyNetObjectStateUpdate();

		bool _isSpawnFinished;
		
		public bool bLoadedScene { get; protected set; }

		Dictionary<int, TinyNetIdentity> _sceneIdentityObjectsToSpawn;

		protected List<TinyNetPlayerController> _localPlayers = new List<TinyNetPlayerController>();
		public List<TinyNetPlayerController> localPlayers { get { return _localPlayers; } }

		public TinyNetClient() : base() {
			instance = this;
		}

		protected override void RegisterMessageHandlers() {
			base.RegisterMessageHandlers();

			TinyNetGameManager.instance.RegisterMessageHandlersClient();

			// A local client is basically the client in a listen server.
			if (TinyNetGameManager.instance.isListenServer) {
				RegisterHandlerSafe(TinyNetMsgType.ObjectDestroy, OnLocalClientObjectDestroy);
				RegisterHandlerSafe(TinyNetMsgType.ObjectSpawnMessage, OnLocalClientObjectSpawn);
				RegisterHandlerSafe(TinyNetMsgType.ObjectSpawnScene, OnLocalClientObjectSpawnScene);
				RegisterHandlerSafe(TinyNetMsgType.ObjectHide, OnLocalClientObjectHide);
				RegisterHandlerSafe(TinyNetMsgType.AddPlayer, OnLocalAddPlayerMessage);
			} else {
				// LocalClient shares the sim/scene with the server, no need for these events
				RegisterHandlerSafe(TinyNetMsgType.ObjectDestroy, OnObjectDestroy);
				RegisterHandlerSafe(TinyNetMsgType.ObjectSpawnMessage, OnObjectSpawn);
				RegisterHandlerSafe(TinyNetMsgType.StateUpdate, OnStateUpdateMessage);
				RegisterHandlerSafe(TinyNetMsgType.ObjectSpawnScene, OnObjectSpawnScene);
				RegisterHandlerSafe(TinyNetMsgType.SpawnFinished, OnObjectSpawnFinished);
				RegisterHandlerSafe(TinyNetMsgType.ObjectHide, OnObjectDestroy);
				//RegisterHandlerSafe(TinyNetMsgType.SyncList, OnSyncListMessage);
				//RegisterHandlerSafe(TinyNetMsgType.Animation, NetworkAnimator.OnAnimationClientMessage);
				//RegisterHandlerSafe(TinyNetMsgType.AnimationParameters, NetworkAnimator.OnAnimationParametersClientMessage);
				RegisterHandlerSafe(TinyNetMsgType.AddPlayer, OnAddPlayerMessage);
				RegisterHandlerSafe(TinyNetMsgType.RemovePlayer, OnRemovePlayerMessage);
			}

			RegisterHandlerSafe(TinyNetMsgType.LocalClientAuthority, OnClientAuthorityMessage);

			RegisterHandler(TinyNetMsgType.Scene, OnClientChangeSceneMessage);
		}

		public virtual bool StartClient() {
			if (_netManager != null) {
				TinyLogger.LogError("StartClient() called multiple times.");
				return false;
			}

			_netManager = new NetManager(this, Application.version);
			_netManager.Start();

			ConfigureNetManager(true);

			TinyLogger.Log("[CLIENT] Started client");

			return true;
		}

		public virtual void ClientConnectTo(string hostAddress, int hostPort) {
			TinyLogger.Log("[CLIENT] Attempt to connect at adress: " + hostAddress + ":" + hostPort);

			_netManager.Connect(hostAddress, hostPort);
		}

		protected override TinyNetConnection CreateTinyNetConnection(NetPeer peer) {
			TinyNetConnection tinyConn = TinyNetGameManager.instance.isListenServer ? new TinyNetLocalConnectionToServer(peer) : new TinyNetConnection(peer);

			tinyNetConns.Add(tinyConn);

			//First connection is to host:
			if (tinyNetConns.Count == 1) {
				connToHost = tinyNetConns[0];
			}

			return tinyConn;
		}

		//============ TinyNetEvents ========================//

		protected override void OnConnectionCreated(TinyNetConnection nConn) {
			base.OnConnectionCreated(nConn);

			TinyNetEmptyMessage msg = new TinyNetEmptyMessage();
			msg.msgType = TinyNetMsgType.Connect;
			nConn.Send(msg, SendOptions.ReliableOrdered);
		}

		//============ Static Methods =======================//



		//============ Object Networking ====================//

		public void SendRPCToServer(NetDataWriter stream, int rpcMethodIndex, ITinyNetObject iObj) {
			var msg = new TinyNetRPCMessage();

			msg.networkID = iObj.NetworkID;
			msg.rpcMethodIndex = rpcMethodIndex;
			msg.parameters = stream.Data;

			SendMessageByChannelToTargetConnection(msg, SendOptions.ReliableOrdered, connToHost);
		}

		//============ TinyNetMessages Handlers =============//

		void OnLocalClientObjectDestroy(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetObjectDestroyMessage);

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("ClientScene::OnLocalObjectObjDestroy netId:" + s_TinyNetObjectDestroyMessage.networkID); }

			// Removing from the tinynetidentitylist is already done at OnNetworkDestroy() at the TinyNetIdentity.

			/*TinyNetIdentity localObject = _localIdentityObjects[s_TinyNetObjectSpawnMessage.networkID];
			if (localObject != null) {
				RemoveTinyNetIdentityFromList(localObject);
			} else {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("You tried to call OnLocalClientObjectDestroy on a non localIdentityObjects, how?"); }
			}*/
		}

		void OnLocalClientObjectHide(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetObjectHideMessage);

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("ClientScene::OnLocalObjectObjHide netId:" + s_TinyNetObjectHideMessage.networkID); }

			TinyNetIdentity localObject = _localIdentityObjects[s_TinyNetObjectHideMessage.networkID];
			if (localObject != null) {
				localObject.OnSetLocalVisibility(false);
			}
		}

		void OnLocalClientObjectSpawn(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetObjectSpawnMessage);

			TinyNetIdentity localObject = _localIdentityObjects[s_TinyNetObjectSpawnMessage.networkID];
			if (localObject != null) {
				localObject.OnStartClient();
				localObject.OnSetLocalVisibility(true);
			}
		}

		void OnLocalClientObjectSpawnScene(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetObjectSpawnSceneMessage);

			TinyNetIdentity localObject = _localIdentityObjects[s_TinyNetObjectSpawnSceneMessage.networkID];
			if (localObject != null) {
				localObject.OnSetLocalVisibility(true);
			}
		}

		void OnObjectDestroy(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetObjectDestroyMessage);

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("ClientScene::OnObjDestroy networkID:" + s_TinyNetObjectDestroyMessage.networkID); }

			TinyNetIdentity localObject = _localIdentityObjects[s_TinyNetObjectDestroyMessage.networkID];
			if (localObject != null) {
				localObject.OnNetworkDestroy();

				if (!TinyNetGameManager.instance.InvokeUnSpawnHandler(localObject.assetGUID, localObject.gameObject)) {
					// default handling
					if (localObject.sceneID == 0) {
						Object.Destroy(localObject.gameObject);
					} else {
						// scene object.. disable it in scene instead of destroying
						localObject.gameObject.SetActive(false);
						_sceneIdentityObjectsToSpawn[localObject.sceneID] = localObject;
					}
				}
			} else {
				if (TinyNetLogLevel.logDebug) { TinyLogger.LogWarning("Did not find target for destroy message for " + s_TinyNetObjectDestroyMessage.networkID); }
			}
		}

		void OnObjectSpawn(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetObjectSpawnMessage);

			if (s_TinyNetObjectSpawnMessage.assetIndex < 0 || s_TinyNetObjectSpawnMessage.assetIndex > int.MaxValue || s_TinyNetObjectSpawnMessage.assetIndex > TinyNetGameManager.instance.GetAmountOfRegisteredAssets()) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("OnObjSpawn networkID: " + s_TinyNetObjectSpawnMessage.networkID + " has invalid asset Id"); }
				return;
			}
			if (TinyNetLogLevel.logDev) { TinyLogger.Log("Client spawn handler instantiating [networkID:" + s_TinyNetObjectSpawnMessage.networkID + " asset ID:" + s_TinyNetObjectSpawnMessage.assetIndex + " pos:" + s_TinyNetObjectSpawnMessage.position + "]"); }

			TinyNetIdentity localTinyNetIdentity = _localIdentityObjects[s_TinyNetObjectDestroyMessage.networkID];
			if (localTinyNetIdentity != null) {
				// this object already exists (was in the scene), just apply the update to existing object
				ApplyInitialState(localTinyNetIdentity, s_TinyNetObjectSpawnMessage.position, s_TinyNetObjectSpawnMessage.initialState, s_TinyNetObjectSpawnMessage.networkID, null);
				return;
			}

			GameObject prefab;
			SpawnDelegate handler;
			if (prefab = TinyNetGameManager.instance.GetPrefabFromAssetId(s_TinyNetObjectSpawnMessage.assetIndex)) {
				var obj = (GameObject)Object.Instantiate(prefab, s_TinyNetObjectSpawnMessage.position, Quaternion.identity);

				localTinyNetIdentity = obj.GetComponent<TinyNetIdentity>();

				if (localTinyNetIdentity == null) {
					if (TinyNetLogLevel.logError) { TinyLogger.LogError("Client object spawned for " + s_TinyNetObjectSpawnMessage.assetIndex + " does not have a TinyNetidentity"); }
					return;
				}

				ApplyInitialState(localTinyNetIdentity, s_TinyNetObjectSpawnMessage.position, s_TinyNetObjectSpawnMessage.initialState, s_TinyNetObjectSpawnMessage.networkID, obj);
			} else if (TinyNetGameManager.instance.GetSpawnHandler(s_TinyNetObjectSpawnMessage.assetIndex, out handler)) {
				// lookup registered factory for type:
				GameObject obj = handler(s_TinyNetObjectSpawnMessage.position, s_TinyNetObjectSpawnMessage.assetIndex);
				if (obj == null) {
					if (TinyNetLogLevel.logWarn) { TinyLogger.LogWarning("Client spawn handler for " + s_TinyNetObjectSpawnMessage.assetIndex + " returned null"); }
					return;
				}

				localTinyNetIdentity = obj.GetComponent<TinyNetIdentity>();
				if (localTinyNetIdentity == null) {
					if (TinyNetLogLevel.logError) { TinyLogger.LogError("Client object spawned for " + s_TinyNetObjectSpawnMessage.assetIndex + " does not have a network identity"); }
					return;
				}

				localTinyNetIdentity.SetDynamicAssetGUID(TinyNetGameManager.instance.GetAssetGUIDFromAssetId(s_TinyNetObjectSpawnMessage.assetIndex));
				ApplyInitialState(localTinyNetIdentity, s_TinyNetObjectSpawnMessage.position, s_TinyNetObjectSpawnMessage.initialState, s_TinyNetObjectSpawnMessage.networkID, obj);
			} else {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("Failed to spawn server object, assetId=" + s_TinyNetObjectSpawnMessage.assetIndex + " networkID=" + s_TinyNetObjectSpawnMessage.networkID); }
			}
		}

		void OnObjectSpawnScene(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetObjectSpawnSceneMessage);

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("Client spawn scene handler instantiating [networkID: " + s_TinyNetObjectSpawnSceneMessage.networkID + " sceneId: " + s_TinyNetObjectSpawnSceneMessage.sceneId + " pos: " + s_TinyNetObjectSpawnSceneMessage.position); }

			TinyNetIdentity localTinyNetIdentity = _localIdentityObjects[s_TinyNetObjectSpawnSceneMessage.networkID];
			if (localTinyNetIdentity != null) {
				// this object already exists (was in the scene)
				ApplyInitialState(localTinyNetIdentity, s_TinyNetObjectSpawnSceneMessage.position, s_TinyNetObjectSpawnSceneMessage.initialState, s_TinyNetObjectSpawnSceneMessage.networkID, localTinyNetIdentity.gameObject);
				return;
			}

			TinyNetIdentity spawnedId = SpawnSceneObject(s_TinyNetObjectSpawnSceneMessage.sceneId);
			if (spawnedId == null) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("Spawn scene object not found for " + s_TinyNetObjectSpawnSceneMessage.sceneId); }
				return;
			}

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("Client spawn for [networkID :" + s_TinyNetObjectSpawnSceneMessage.networkID + "] [sceneId: " + s_TinyNetObjectSpawnSceneMessage.sceneId + "] obj: " + spawnedId.gameObject.name); }

			ApplyInitialState(spawnedId, s_TinyNetObjectSpawnSceneMessage.position, s_TinyNetObjectSpawnSceneMessage.initialState, s_TinyNetObjectSpawnSceneMessage.networkID, spawnedId.gameObject);
		}

		void OnObjectSpawnFinished(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TineNetObjectSpawnFinishedMessage);

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("SpawnFinished: " + s_TineNetObjectSpawnFinishedMessage.state); }

			// when 0, means we already started receiving the spawn messages but we have yet to receive them all.
			if (s_TineNetObjectSpawnFinishedMessage.state == 0) {
				PrepareToSpawnSceneObjects();
				_isSpawnFinished = false;

				return;
			}

			// when 1, means we have received every single spawn message!
			foreach (TinyNetIdentity tinyNetId in _localIdentityObjects.Values) {
				tinyNetId.OnNetworkCreate();

				if (tinyNetId.isClient) {
					tinyNetId.OnStartClient();
				}
			}

			_isSpawnFinished = true;
		}

		void OnClientAuthorityMessage(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetClientAuthorityMessage);

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("ClientScene::OnClientAuthority for  connectionId=" + netMsg.tinyNetConn.ConnectId + " netId: " + s_TinyNetClientAuthorityMessage.networkID); }

			TinyNetIdentity tni = _localIdentityObjects[s_TinyNetClientAuthorityMessage.networkID];

			if (tni != null) {
				tni.HandleClientAuthority(s_TinyNetClientAuthorityMessage.authority);
			}
		}

		/// <summary>
		/// By default it will deserialize the TinyNetSyncVar properties.
		/// </summary>
		/// <param name="netMsg"></param>
		void OnStateUpdateMessage(TinyNetMessageReader netMsg) {
			int networkID = netMsg.reader.GetInt();

			if (TinyNetLogLevel.logDev) { TinyLogger.Log("ClientScene::OnUpdateVarsMessage " + networkID + " channel:" + netMsg.channelId); }

			ITinyNetObject localObject = _localNetObjects[networkID];
			if (localObject != null) {
				localObject.TinyDeserialize(netMsg.reader, false);
			} else {
				if (TinyNetLogLevel.logWarn) { TinyLogger.LogWarning("Did not find target for sync message for " + networkID); }
			}
		}

		//============ TinyNetIdentity Functions ============//

		void ApplyInitialState(TinyNetIdentity tinyNetId, Vector3 position, byte[] initialState, int networkID, GameObject newGameObject) {
			if (!tinyNetId.gameObject.activeSelf) {
				tinyNetId.gameObject.SetActive(true);
			}

			tinyNetId.transform.position = position;

			if (initialState != null && initialState.Length > 0) {
				var initialStateReader = new NetDataReader(initialState);
				tinyNetId.DeserializeAllTinyNetObjects(initialStateReader, true);
			}

			if (newGameObject == null) {
				return;
			}

			newGameObject.SetActive(true);
			tinyNetId.ReceiveNetworkID(networkID);
			AddTinyNetIdentityToList(tinyNetId);

			// If the object was spawned as part of the initial replication (s_TineNetObjectSpawnFinishedMessage.state == 0) it will have it's OnStartClient called by OnObjectSpawnFinished.
			if (_isSpawnFinished) {
				tinyNetId.OnNetworkCreate();
				tinyNetId.OnStartClient();
			}
		}

		void PrepareToSpawnSceneObjects() {
			//NOTE: what is there are already objects in this dict?! should we merge with them?
			_sceneIdentityObjectsToSpawn = new Dictionary<int, TinyNetIdentity>();

			foreach (TinyNetIdentity tinyNetId in Resources.FindObjectsOfTypeAll<TinyNetIdentity>()) {
				if (tinyNetId.gameObject.activeSelf) {
					// already active, cannot spawn it
					continue;
				}

				if (tinyNetId.gameObject.hideFlags == HideFlags.NotEditable || tinyNetId.gameObject.hideFlags == HideFlags.HideAndDontSave) {
					continue;
				}

				if (tinyNetId.sceneID == 0) {
					continue;
				}

				_sceneIdentityObjectsToSpawn[tinyNetId.sceneID] = tinyNetId;

				if (TinyNetLogLevel.logDebug) { TinyLogger.Log("ClientScene::PrepareSpawnObjects sceneId: " + tinyNetId.sceneID); }
			}
		}

		TinyNetIdentity SpawnSceneObject(int sceneId) {
			if (_sceneIdentityObjectsToSpawn.ContainsKey(sceneId)) {
				TinyNetIdentity foundId = _sceneIdentityObjectsToSpawn[sceneId];
				_sceneIdentityObjectsToSpawn.Remove(sceneId);

				return foundId;
			}

			return null;
		}

		//===

		public virtual bool Ready() {
			if (!isConnected) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("Ready() called but we are not connected to anything."); }
				return false;
			}

			// The first connection should always be to the host.
			//TinyNetConnection conn = _tinyNetConns[0];

			if (connToHost.isReady) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("A connection has already been set as ready. There can only be one."); }
				return false;
			}			

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("ClientScene::Ready() called with connection [" + connToHost + "]"); }

			var msg = new TinyNetReadyMessage();
			SendMessageByChannelToTargetConnection(msg, SendOptions.ReliableOrdered, connToHost);

			connToHost.isReady = true;

			if (OnClientReadyEvent != null) {
				OnClientReadyEvent();
			}

			return true;
		}

		public virtual void OnClientSceneChanged() {
			// always become ready.
			Ready();

			// Saishy: I don't think the client should be the one managing the spawn of player controllers?

			/*if (!m_AutoCreatePlayer) {
				return;
			}

			bool addPlayer = (ClientScene.localPlayers.Count == 0);
			bool foundPlayer = false;
			foreach (var playerController in ClientScene.localPlayers) {
				if (playerController.gameObject != null) {
					foundPlayer = true;
					break;
				}
			}
			if (!foundPlayer) {
				// there are players, but their game objects have all been deleted
				addPlayer = true;
			}
			if (addPlayer) {
				ClientScene.AddPlayer(0);
			}*/
		}

		//============ Scenes Methods =======================//

		/// <summary>
		/// Handler for a scene change message.
		/// </summary>
		/// <param name="netMsg"></param>
		protected virtual void OnClientChangeSceneMessage(TinyNetMessageReader netMsg) {
			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("TinyNetClient:OnClientChangeSceneMessage"); }

			string newSceneName = netMsg.reader.GetString();

			if (isConnected && !TinyNetGameManager.instance.isServer) {
				TinyNetGameManager.instance.ClientChangeScene(newSceneName, true);
			}
		}

		/// <summary>
		/// Called from the TinyNetGameManager when a scene finishes loading.
		/// </summary>
		public virtual void ClientFinishLoadScene() {
			bLoadedScene = true;
		}

		//============ Players Methods ======================//

		protected override void CreatePlayerAndAdd(TinyNetConnection conn, int playerControllerId) {
			if (TinyNetGameManager.instance.isListenServer) {
				conn.SetPlayerController<TinyNetPlayerController>(TinyNetServer.instance.GetPlayerControllerFromConnection(connToHost.ConnectId, (short)playerControllerId));
				return;
			}

			base.CreatePlayerAndAdd(conn, playerControllerId);
		}

		protected virtual void OnLocalAddPlayerMessage(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetAddPlayerMessage);

			CreatePlayerAndAdd(netMsg.tinyNetConn, s_TinyNetAddPlayerMessage.playerControllerId);
		}

		protected virtual void OnAddPlayerMessage(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetAddPlayerMessage);

			AddPlayerControllerToConnection(netMsg.tinyNetConn, s_TinyNetAddPlayerMessage.playerControllerId);
		}

		protected virtual void OnRemovePlayerMessage(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetRemovePlayerMessage);

			//netMsg.tinyNetConn.RemovePlayerController(s_TinyNetRemovePlayerMessage.playerControllerId);
			RemovePlayerControllerFromConnection(netMsg.tinyNetConn, s_TinyNetRemovePlayerMessage.playerControllerId);
		}

		public void RequestAddPlayerControllerToServer(int amountPlayers = 1) {
			if (amountPlayers <= 0) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("RequestAddPlayerControllerToServer() called with amountPlayers <= 0"); }
				return;
			}

			s_TinyNetRequestAddPlayerMessage.amountOfPlayers = (ushort)amountPlayers;
			SendMessageByChannelToTargetConnection(s_TinyNetRequestAddPlayerMessage, SendOptions.ReliableOrdered, connToHost);
		}
	}
}
