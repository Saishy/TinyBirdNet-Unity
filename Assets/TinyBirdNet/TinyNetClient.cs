using UnityEngine;
using System.Collections;
using LiteNetLib;
using LiteNetLib.Utils;
using TinyBirdUtils;
using TinyBirdNet.Messaging;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace TinyBirdNet {

	public class TinyNetClient : TinyNetScene {

		public static TinyNetClient instance;

		public override string TYPE { get { return "CLIENT"; } }

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

			// A local client is basically the client in a listen server.
			if (TinyNetGameManager.instance.isListenServer) {
				RegisterHandlerSafe(TinyNetMsgType.ObjectDestroy, OnLocalClientObjectDestroy);
				//RegisterHandlerSafe(TinyNetMsgType.ObjectHide, OnLocalClientObjectHide);
				RegisterHandlerSafe(TinyNetMsgType.ObjectSpawnMessage, OnLocalClientObjectSpawn);
				RegisterHandlerSafe(TinyNetMsgType.ObjectSpawnScene, OnLocalClientObjectSpawnScene);
				//RegisterHandlerSafe(TinyNetMsgType.LocalClientAuthority, OnClientAuthority);
			} else {
				// LocalClient shares the sim/scene with the server, no need for these events
				RegisterHandlerSafe(TinyNetMsgType.ObjectDestroy, OnObjectDestroy);
				RegisterHandlerSafe(TinyNetMsgType.ObjectSpawnMessage, OnObjectSpawn);
				RegisterHandlerSafe(TinyNetMsgType.Owner, OnOwnerMessage);
				//RegisterHandlerSafe(TinyNetMsgType.ObjectHide, OnObjectDestroy);
				RegisterHandlerSafe(TinyNetMsgType.StateUpdate, OnStateUpdateMessage);
				RegisterHandlerSafe(TinyNetMsgType.ObjectSpawnScene, OnObjectSpawnScene);
				RegisterHandlerSafe(TinyNetMsgType.SpawnFinished, OnObjectSpawnFinished);
				//RegisterHandlerSafe(TinyNetMsgType.SyncList, OnSyncListMessage);
				//RegisterHandlerSafe(TinyNetMsgType.Animation, NetworkAnimator.OnAnimationClientMessage);
				//RegisterHandlerSafe(TinyNetMsgType.AnimationParameters, NetworkAnimator.OnAnimationParametersClientMessage);
				//RegisterHandlerSafe(TinyNetMsgType.LocalClientAuthority, OnClientAuthority);
				RegisterHandlerSafe(TinyNetMsgType.AddPlayer, OnAddPlayerMessage);
				RegisterHandlerSafe(TinyNetMsgType.RemovePlayer, OnRemovePlayerMessage);
			}

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

		//============ Static Methods =======================//

		

		//============ Object Networking ====================//



		//============ TinyNetMessages Handlers =============//

		void OnLocalClientObjectDestroy(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetObjectDestroyMessage);

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("ClientScene::OnLocalObjectObjDestroy netId:" + s_TinyNetObjectDestroyMessage.networkID); }

			TinyNetIdentity localObject = _localIdentityObjects[s_TinyNetObjectSpawnMessage.networkID];
			if (localObject != null) {
				RemoveTinyNetIdentityFromList(localObject);
			} else {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("You tried to call OnLocalClientObjectDestroy on a non localIdentityObjects, how?"); }
			}
		}

		/*void OnLocalClientObjectHide(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetObjectDestroyMessage);

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("ClientScene::OnLocalObjectObjHide netId:" + s_TinyNetObjectDestroyMessage.networkID); }

			TinyNetIdentity localObject = _localIdentityObjects[s_TinyNetObjectDestroyMessage.networkID];
			if (localObject != null) {
				localObject.OnSetLocalVisibility(false);
			}
		}*/

		void OnLocalClientObjectSpawn(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetObjectSpawnMessage);

			TinyNetIdentity localObject = _localIdentityObjects[s_TinyNetObjectSpawnMessage.networkID];
			if (localObject != null) {
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
				RemoveTinyNetIdentityFromList(localObject);

				/*if (!InvokeUnSpawnHandler(localObject.assetId, localObject.gameObject)) {
					// default handling
					if (localObject.sceneId.IsEmpty()) {
						Object.Destroy(localObject.gameObject);
					} else {
						// scene object.. disable it in scene instead of destroying
						localObject.gameObject.SetActive(false);
						s_SpawnableObjects[localObject.sceneId] = localObject;
					}
				}*/
			} else {
				if (TinyNetLogLevel.logDebug) { TinyLogger.LogWarning("Did not find target for destroy message for " + s_TinyNetObjectDestroyMessage.networkID); }
			}
		}

		void OnObjectSpawn(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetObjectSpawnMessage);

			if (s_TinyNetObjectSpawnMessage.assetId < 0 || s_TinyNetObjectSpawnMessage.assetId > int.MaxValue || s_TinyNetObjectSpawnMessage.assetId > TinyNetGameManager.instance.GetAmountOfRegisteredAssets()) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("OnObjSpawn networkID: " + s_TinyNetObjectSpawnMessage.networkID + " has invalid asset Id"); }
				return;
			}
			if (TinyNetLogLevel.logDev) { TinyLogger.Log("Client spawn handler instantiating [networkID:" + s_TinyNetObjectSpawnMessage.networkID + " asset ID:" + s_TinyNetObjectSpawnMessage.assetId + " pos:" + s_TinyNetObjectSpawnMessage.position + "]"); }

			TinyNetIdentity localTinyNetIdentity = _localIdentityObjects[s_TinyNetObjectDestroyMessage.networkID];
			if (localTinyNetIdentity != null) {
				// this object already exists (was in the scene), just apply the update to existing object
				ApplyInitialState(localTinyNetIdentity, s_TinyNetObjectSpawnMessage.position, s_TinyNetObjectSpawnMessage.initialState, s_TinyNetObjectSpawnMessage.networkID, null);
				return;
			}

			GameObject prefab;
			//SpawnDelegate handler;
			if (prefab = TinyNetGameManager.instance.GetPrefabFromAssetId(s_TinyNetObjectSpawnMessage.assetId)) {
				var obj = (GameObject)Object.Instantiate(prefab, s_TinyNetObjectSpawnMessage.position, Quaternion.identity);

				localTinyNetIdentity = obj.GetComponent<TinyNetIdentity>();

				if (localTinyNetIdentity == null) {
					if (TinyNetLogLevel.logError) { TinyLogger.LogError("Client object spawned for " + s_TinyNetObjectSpawnMessage.assetId + " does not have a TinyNetidentity"); }
					return;
				}

				ApplyInitialState(localTinyNetIdentity, s_TinyNetObjectSpawnMessage.position, s_TinyNetObjectSpawnMessage.initialState, s_TinyNetObjectSpawnMessage.networkID, obj);
			}
			/*// lookup registered factory for type:
			else if (NetworkScene.GetSpawnHandler(s_TinyNetObjectSpawnMessage.assetId, out handler)) {
				GameObject obj = handler(s_TinyNetObjectSpawnMessage.position, s_TinyNetObjectSpawnMessage.assetId);
				if (obj == null) {
					if (TinyNetLogLevel.logWarn) { TinyLogger.LogWarning("Client spawn handler for " + s_TinyNetObjectSpawnMessage.assetId + " returned null"); }
					return;
				}

				localTinyNetIdentity = obj.GetComponent<TinyNetIdentity>();
				if (localTinyNetIdentity == null) {
					if (TinyNetLogLevel.logError) { TinyLogger.LogError("Client object spawned for " + s_TinyNetObjectSpawnMessage.assetId + " does not have a network identity"); }
					return;
				}

				localTinyNetIdentity.SetDynamicAssetId(s_TinyNetObjectSpawnMessage.assetId);
				ApplyInitialState(localTinyNetIdentity, s_TinyNetObjectSpawnMessage.position, s_TinyNetObjectSpawnMessage.initialState, s_TinyNetObjectSpawnMessage.networkID, obj);
			}*/ else {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("Failed to spawn server object, assetId=" + s_TinyNetObjectSpawnMessage.assetId + " networkID=" + s_TinyNetObjectSpawnMessage.networkID); }
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

			if (s_TineNetObjectSpawnFinishedMessage.state == 0) {
				PrepareToSpawnSceneObjects();
				_isSpawnFinished = false;

				return;
			}

			foreach (TinyNetIdentity tinyNetId in _localIdentityObjects.Values) {
				if (tinyNetId.isClient) {
					tinyNetId.OnStartClient();
					CheckForOwner(tinyNetId);
				}
			}

			_isSpawnFinished = true;
		}

		// OnClientAddedPlayer?
		// Something something to do with changing an owner of an object?
		void OnOwnerMessage(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetOwnerMessage);

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("ClientScene::OnOwnerMessage - connectId=" + netMsg.tinyNetConn.ConnectId + " networkID: " + s_TinyNetOwnerMessage.networkID); }

			// is there already an owner that is a different object??
			/*TinyPlayerController oldOwner;
			if (netMsg.conn.GetPlayerController(s_OwnerMessage.playerControllerId, out oldOwner)) {
				oldOwner.unetView.SetNotLocalPlayer();
			}

			TinyNetIdentity localTinyNetIdentity;
			if (s_NetworkScene.GetTinyNetIdentity(s_OwnerMessage.networkID, out localTinyNetIdentity)) {
				// this object already exists
				localTinyNetIdentity.SetConnectionToServer(netMsg.conn);
				localTinyNetIdentity.SetLocalPlayer(s_OwnerMessage.playerControllerId);
				InternalAddPlayer(localTinyNetIdentity, s_OwnerMessage.playerControllerId);
			} else {
				var pendingOwner = new PendingOwner { networkID = s_OwnerMessage.networkID, playerControllerId = s_OwnerMessage.playerControllerId };
				s_PendingOwnerIds.Add(pendingOwner);
			}*/
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

			// objects spawned as part of initial state are started on a second pass.
			// Saishy: Wat?
			if (_isSpawnFinished) {
				tinyNetId.OnStartClient();
				CheckForOwner(tinyNetId);
			}
		}

		// Have no idea what this is.
		void CheckForOwner(TinyNetIdentity tinyNetId) {
			/*for (int i = 0; i < s_PendingOwnerIds.Count; i++) {
				var pendingOwner = s_PendingOwnerIds[i];

				if (pendingOwner.networkID == tinyNetId.networkID) {
					// found owner, turn into a local player

					// Set isLocalPlayer to true on this TinyNetIdentity and trigger OnStartLocalPlayer in all scripts on the same GO
					tinyNetId.SetConnectionToServer(s_ReadyConnection);
					tinyNetId.SetLocalPlayer(pendingOwner.playerControllerId);

					if (TinyNetLogLevel.logDev) { TinyLogger.Log("ClientScene::OnOwnerMessage - player=" + tinyNetId.gameObject.name); }
					if (s_ReadyConnection.connectionId < 0) {
						if (TinyNetLogLevel.logError) { TinyLogger.LogError("Owner message received on a local client."); }
						return;
					}
					InternalAddPlayer(tinyNetId, pendingOwner.playerControllerId);

					s_PendingOwnerIds.RemoveAt(i);
					break;
				}
			}*/
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

			return true;
		}

		public virtual void OnClientSceneChanged() {
			// always become ready.
			Ready();

			// Saishy: I don't think the client should be the one managing the spawn of player controllers?

			RequestAddPlayerControllerToServer(1);

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