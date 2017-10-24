using UnityEngine;
using System.Collections;
using LiteNetLib;
using LiteNetLib.Utils;
using TinyBirdUtils;
using TinyBirdNet.Messaging;

namespace TinyBirdNet {

	public class TinyNetClient : TinyNetScene {

		public static TinyNetClient instance;

		public override string TYPE { get { return "CLIENT"; } }

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
				//RegisterHandlerSafe(TinyNetMsgType.ObjectSpawnScene, OnLocalClientObjectSpawnScene);
				//RegisterHandlerSafe(TinyNetMsgType.LocalClientAuthority, OnClientAuthority);
			} else {
				// LocalClient shares the sim/scene with the server, no need for these events
				RegisterHandlerSafe(TinyNetMsgType.ObjectSpawnMessage, OnObjectSpawn);
				//RegisterHandlerSafe(MsgType.ObjectSpawnScene, OnObjectSpawnScene);
				//RegisterHandlerSafe(TinyNetMsgType.SpawnFinished, OnObjectSpawnFinished); //Saishy: Something to do with Scene objects?
				RegisterHandlerSafe(TinyNetMsgType.ObjectDestroy, OnObjectDestroy);
				//RegisterHandlerSafe(TinyNetMsgType.ObjectHide, OnObjectDestroy);
				RegisterHandlerSafe(TinyNetMsgType.StateUpdate, OnStateUpdateMessage);
				RegisterHandlerSafe(TinyNetMsgType.Owner, OnOwnerMessage);
				//RegisterHandlerSafe(TinyNetMsgType.SyncList, OnSyncListMessage);
				//RegisterHandlerSafe(TinyNetMsgType.Animation, NetworkAnimator.OnAnimationClientMessage);
				//RegisterHandlerSafe(TinyNetMsgType.AnimationParameters, NetworkAnimator.OnAnimationParametersClientMessage);
				//RegisterHandlerSafe(TinyNetMsgType.LocalClientAuthority, OnClientAuthority);
			}
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

		//============ Object Networking ====================//

		public new static void TinyNetObjectSpawned(ITinyNetObject netObj) {
			if (!TinyNetGameManager.instance.isServer) {
				TinyNetScene.TinyNetObjectSpawned(netObj);
			}

			netObj.OnStartClient();
		}

		//============ TinyNetMessages Handlers =============//

		static void OnLocalClientObjectDestroy(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetObjectDestroyMessage);

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("ClientScene::OnLocalObjectObjDestroy netId:" + s_TinyNetObjectDestroyMessage.networkID); }

			TinyNetIdentity localObject = _localIdentityObjects[s_TinyNetObjectSpawnMessage.networkID];
			if (localObject != null) {
				TinyNetIdentityDestroyed(localObject, true);
			} else {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("You tried to call OnLocalClientObjectDestroy on a non localIdentityObjects, how?"); }
			}
		}

		/*static void OnLocalClientObjectHide(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetObjectDestroyMessage);

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("ClientScene::OnLocalObjectObjHide netId:" + s_TinyNetObjectDestroyMessage.networkID); }

			TinyNetIdentity localObject = _localIdentityObjects[s_TinyNetObjectDestroyMessage.networkID];
			if (localObject != null) {
				localObject.OnSetLocalVisibility(false);
			}
		}*/

		static void OnLocalClientObjectSpawn(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetObjectSpawnMessage);

			TinyNetIdentity localObject = _localIdentityObjects[s_TinyNetObjectSpawnMessage.networkID];
			if (localObject != null) {
				localObject.OnSetLocalVisibility(true);
			}
		}

		/*static void OnLocalClientObjectSpawnScene(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_ObjectSpawnSceneMessage);
			NetworkIdentity localObject;
			if (s_NetworkScene.GetNetworkIdentity(s_ObjectSpawnSceneMessage.networkID, out localObject)) {
				localObject.OnSetLocalVisibility(true);
			}
		}*/

		static void OnObjectDestroy(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetObjectDestroyMessage);

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("ClientScene::OnObjDestroy networkID:" + s_TinyNetObjectDestroyMessage.networkID); }

			TinyNetIdentity localObject = _localIdentityObjects[s_TinyNetObjectDestroyMessage.networkID];
			if (localObject != null) {
				TinyNetIdentityDestroyed(localObject);

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

		static void OnObjectSpawn(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetObjectSpawnMessage);

			if (s_TinyNetObjectSpawnMessage.assetId < 0 || s_TinyNetObjectSpawnMessage.assetId > int.MaxValue || s_TinyNetObjectSpawnMessage.assetId > guidToPrefab.Count) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("OnObjSpawn networkID: " + s_TinyNetObjectSpawnMessage.networkID + " has invalid asset Id"); }
				return;
			}
			if (TinyNetLogLevel.logDev) { TinyLogger.Log("Client spawn handler instantiating [networkID:" + s_TinyNetObjectSpawnMessage.networkID + " asset ID:" + s_TinyNetObjectSpawnMessage.assetId + " pos:" + s_TinyNetObjectSpawnMessage.position + "]"); }

			TinyNetIdentity localNetworkIdentity;
			/*if (s_NetworkScene.GetNetworkIdentity(s_TinyNetObjectSpawnMessage.networkID, out localNetworkIdentity)) {
				// this object already exists (was in the scene), just apply the update to existing object
				ApplySpawnPayload(localNetworkIdentity, s_TinyNetObjectSpawnMessage.position, s_TinyNetObjectSpawnMessage.initialState, s_TinyNetObjectSpawnMessage.networkID, null);
				return;
			}*/

			GameObject prefab;
			//SpawnDelegate handler;
			if (prefab = TinyNetGameManager.instance.GetPrefabFromAssetId(s_TinyNetObjectSpawnMessage.assetId)) {
				var obj = (GameObject)Object.Instantiate(prefab, s_TinyNetObjectSpawnMessage.position, Quaternion.identity);

				localNetworkIdentity = obj.GetComponent<TinyNetIdentity>();

				if (localNetworkIdentity == null) {
					if (TinyNetLogLevel.logError) { TinyLogger.LogError("Client object spawned for " + s_TinyNetObjectSpawnMessage.assetId + " does not have a TinyNetidentity"); }
					return;
				}

				ApplyInitialState(localNetworkIdentity, s_TinyNetObjectSpawnMessage.position, s_TinyNetObjectSpawnMessage.initialState, s_TinyNetObjectSpawnMessage.networkID, obj);
			}
			/*// lookup registered factory for type:
			else if (NetworkScene.GetSpawnHandler(s_TinyNetObjectSpawnMessage.assetId, out handler)) {
				GameObject obj = handler(s_TinyNetObjectSpawnMessage.position, s_TinyNetObjectSpawnMessage.assetId);
				if (obj == null) {
					if (TinyNetLogLevel.logWarn) { TinyLogger.LogWarning("Client spawn handler for " + s_TinyNetObjectSpawnMessage.assetId + " returned null"); }
					return;
				}

				localNetworkIdentity = obj.GetComponent<TinyNetIdentity>();
				if (localNetworkIdentity == null) {
					if (TinyNetLogLevel.logError) { TinyLogger.LogError("Client object spawned for " + s_TinyNetObjectSpawnMessage.assetId + " does not have a network identity"); }
					return;
				}

				localNetworkIdentity.SetDynamicAssetId(s_TinyNetObjectSpawnMessage.assetId);
				ApplyInitialState(localNetworkIdentity, s_TinyNetObjectSpawnMessage.position, s_TinyNetObjectSpawnMessage.initialState, s_TinyNetObjectSpawnMessage.networkID, obj);
			}*/ else {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("Failed to spawn server object, assetId=" + s_TinyNetObjectSpawnMessage.assetId + " networkID=" + s_TinyNetObjectSpawnMessage.networkID); }
			}
		}

		static void ApplyInitialState(TinyNetIdentity tinyNetId, Vector3 position, byte[] initialState, int networkID, GameObject newGameObject) {
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
			TinyNetIdentitySpawned(tinyNetId);

			// objects spawned as part of initial state are started on a second pass Saishy: Wat?
			//if (s_IsSpawnFinished) {
			tinyNetId.OnStartClient();
			CheckForOwner(tinyNetId);
			//}
		}

		static void OnStateUpdateMessage(TinyNetMessageReader netMsg) {
			int networkID = netMsg.reader.GetInt();

			if (TinyNetLogLevel.logDev) { TinyLogger.Log("ClientScene::OnUpdateVarsMessage " + networkID + " channel:" + netMsg.channelId); }

			ITinyNetObject localObject = _localNetObjects[networkID];
			if (localObject != null) {
				localObject.TinyDeserialize(netMsg.reader, false);
			} else {
				if (TinyNetLogLevel.logWarn) { TinyLogger.LogWarning("Did not find target for sync message for " + networkID); }
			}
		}

		// OnClientAddedPlayer?
		// Something something to do with changing an owner of an object?
		static void OnOwnerMessage(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetOwnerMessage);

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("ClientScene::OnOwnerMessage - connectId=" + netMsg.tinyNetConn.ConnectId + " networkID: " + s_TinyNetOwnerMessage.networkID); }

			// is there already an owner that is a different object??
			/*TinyPlayerController oldOwner;
			if (netMsg.conn.GetPlayerController(s_OwnerMessage.playerControllerId, out oldOwner)) {
				oldOwner.unetView.SetNotLocalPlayer();
			}

			TinyNetIdentity localNetworkIdentity;
			if (s_NetworkScene.GetNetworkIdentity(s_OwnerMessage.networkID, out localNetworkIdentity)) {
				// this object already exists
				localNetworkIdentity.SetConnectionToServer(netMsg.conn);
				localNetworkIdentity.SetLocalPlayer(s_OwnerMessage.playerControllerId);
				InternalAddPlayer(localNetworkIdentity, s_OwnerMessage.playerControllerId);
			} else {
				var pendingOwner = new PendingOwner { networkID = s_OwnerMessage.networkID, playerControllerId = s_OwnerMessage.playerControllerId };
				s_PendingOwnerIds.Add(pendingOwner);
			}*/
		}

		// Have no idea what this is.
		static void CheckForOwner(TinyNetIdentity tinyNetId) {
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
	}
}