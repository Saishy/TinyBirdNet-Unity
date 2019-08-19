using UnityEngine;
using System.Collections;
using LiteNetLib;
using LiteNetLib.Utils;
using TinyBirdUtils;
using TinyBirdNet.Messaging;
using System.Collections.Generic;
using TinyBirdNet.Utils;
using System.Net;

namespace TinyBirdNet {

	/// <summary>
	/// Represents the Scene of a Client.
	/// </summary>
	/// <seealso cref="TinyBirdNet.TinyNetScene" />
	public class TinyNetClient : TinyNetScene {

		/// <summary>
		/// The singleton instance.
		/// </summary>
		public static TinyNetClient instance;

		/// <inheritdoc />
		public override string TYPE { get { return "CLIENT"; } }

		/// <summary>
		/// A reader to be used when reading state updates.
		/// </summary>
		protected static TinyNetStateReader _stateUpdateReader = new TinyNetStateReader();

		public ushort LastServerTick {
			get; protected set;
		}

		//static TinyNetObjectStateUpdate recycleStateUpdateMessage = new TinyNetObjectStateUpdate();

		/// <summary>
		/// If true, all spawning procedures have been finished.
		/// </summary>
		bool _isSpawnFinished;

		/// <summary>
		/// Gets or sets a value indicating whether the scene has been loaded.
		/// </summary>
		/// <value>
		///   <c>true</c> if scene has been loaded; otherwise, <c>false</c>.
		/// </value>
		public bool bLoadedScene { get; protected set; }

		/// <summary>
		/// A dictionary of <see cref="TinyNetIdentity"/> objects to spawn that belong to the scene.
		/// </summary>
		Dictionary<int, TinyNetIdentity> _sceneIdentityObjectsToSpawn;

		/// <summary>
		/// The local players
		/// </summary>
		protected List<TinyNetPlayerController> _localPlayers = new List<TinyNetPlayerController>();
		/// <summary>
		/// Gets the local players.
		/// </summary>
		/// <value>
		/// The local players.
		/// </value>
		public List<TinyNetPlayerController> localPlayers { get { return _localPlayers; } }

		/// <summary>
		/// Initializes a new instance of the <see cref="TinyNetClient"/> class.
		/// </summary>
		public TinyNetClient() : base() {
			instance = this;
		}

		/// <inheritdoc />
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

		/// <summary>
		/// Starts the client.
		/// </summary>
		/// <returns></returns>
		public virtual bool StartClient() {
			if (_netManager != null) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("[" + TYPE + "] StartClient() called multiple times."); }
				return false;
			}

			_netManager = new NetManager(this);
			_netManager.Start();

			ConfigureNetManager(true);

			if (TinyNetLogLevel.logDev) { TinyLogger.Log("[" + TYPE + "] Started client"); }

			return true;
		}

		/// <summary>
		/// Attempts to connect the client to the given server.
		/// </summary>
		/// <param name="hostAddress">The host address.</param>
		/// <param name="hostPort">The host port.</param>
		public virtual void ClientConnectTo(string hostAddress, int hostPort) {
			if (TinyNetLogLevel.logDev) { TinyLogger.Log("[" + TYPE + "] Attempt to connect at adress: " + hostAddress + ":" + hostPort); }

			WriteConnectionRequest(s_recycleWriter);

			_netManager.Connect(hostAddress, hostPort, s_recycleWriter);
		}

		protected NetDataWriter WriteConnectionRequest(NetDataWriter netDataWriter) {
			netDataWriter.Reset();
			netDataWriter.Put(TinyNetGameManager.PROTOCOL_VERSION + TinyNetGameManager.instance.multiplayerConnectKey);
			netDataWriter.Put(TinyNetGameManager.ApplicationGUIDString);

			return netDataWriter;
		}

		/// <summary>
		/// Creates a <see cref="TinyNetConnection"/>
		/// </summary>
		/// <param name="peer">The <see cref="NetPeer"/>.</param>
		/// <returns></returns>
		protected override TinyNetConnection CreateTinyNetConnection(NetPeer peer) {
			TinyNetConnection tinyConn = TinyNetGameManager.instance.isListenServer ? new TinyNetLocalConnectionToServer(peer) : new TinyNetConnection(peer);

			/*TinyNetConnection tinyConn;

			if (TinyNetGameManager.instance.isServer && peer.OriginAppGUID.Equals(NetManager.ApplicationGUID)) {
				tinyConn = new TinyNetLocalConnectionToServer(peer);
			} else {
				tinyConn = new TinyNetConnection(peer);
			}*/

			peer.Tag = tinyConn;

			tinyNetConns.Add(tinyConn);

			//First connection is to host:
			if (tinyNetConns.Count == 1) {
				connToHost = tinyNetConns[0];
			}

			return tinyConn;
		}

		//============ TinyNetEvents ========================//

		/// <inheritdoc />
		protected override void OnConnectionCreated(TinyNetConnection nConn) {
			base.OnConnectionCreated(nConn);

			TinyNetEmptyMessage msg = new TinyNetEmptyMessage();
			msg.msgType = TinyNetMsgType.Connect;
			nConn.Send(msg, DeliveryMethod.ReliableOrdered);
		}

		//============ Static Methods =======================//



		//============ Object Networking ====================//

		/// <summary>
		/// Sends the RPC to server.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <param name="rpcMethodIndex">Index of the RPC method.</param>
		/// <param name="iObj">The <see cref="ITinyNetComponent"/> instance.</param>
		public void SendRPCToServer(NetDataWriter stream, int rpcMethodIndex, ITinyNetComponent iObj) {
			//TODO: Pack rpc messages
			var msg = new TinyNetRPCMessage();

			msg.networkID = iObj.TinyInstanceID.NetworkID;
			msg.componentID = iObj.TinyInstanceID.ComponentID;
			msg.rpcMethodIndex = rpcMethodIndex;
			msg.frameTick = LastServerTick;
			msg.parameters = stream.Data;

			SendMessageByChannelToTargetConnection(msg, DeliveryMethod.ReliableOrdered, connToHost);
		}

		//============ TinyNetMessages Handlers =============//

		/// <summary>
		/// Called when an object is destroyed and we are a Listen Server.
		/// </summary>
		/// <param name="netMsg">A wrapper for a <see cref="TinyNetObjectDestroyMessage"/>.</param>
		void OnLocalClientObjectDestroy(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetObjectDestroyMessage);

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("TinyNetClient::OnLocalObjectObjDestroy netId:" + s_TinyNetObjectDestroyMessage.networkID); }

			// Removing from the tinynetidentitylist is already done at OnNetworkDestroy() at the TinyNetIdentity.

			/*TinyNetIdentity localObject = _localIdentityObjects[s_TinyNetObjectSpawnMessage.networkID];
			if (localObject != null) {
				RemoveTinyNetIdentityFromList(localObject);
			} else {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("You tried to call OnLocalClientObjectDestroy on a non localIdentityObjects, how?"); }
			}*/
		}

		/// <summary>
		/// Called when an object is hidden and we are a Listen Server.
		/// </summary>
		/// <param name="netMsg">A wrapper for a <see cref="TinyNetObjectHideMessage"/>.</param>
		void OnLocalClientObjectHide(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetObjectHideMessage);

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("TinyNetClient::OnLocalObjectObjHide netId:" + s_TinyNetObjectHideMessage.networkID); }

			TinyNetIdentity localObject = GetTinyNetIdentityByNetworkID(s_TinyNetObjectHideMessage.networkID);
			if (localObject != null) {
				localObject.OnRemoveLocalVisibility();
			}
		}

		/// <summary>
		/// Called when an object is spawned and we are a Listen Server.
		/// </summary>
		/// <param name="netMsg">A wrapper for a <see cref="TinyNetObjectSpawnMessage"/>.</param>
		void OnLocalClientObjectSpawn(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetObjectSpawnMessage);

			TinyNetIdentity localObject = GetTinyNetIdentityByNetworkID(s_TinyNetObjectSpawnMessage.networkID);
			if (localObject != null) {
				localObject.OnStartClient();
				localObject.OnGiveLocalVisibility();
			} else {
				if (TinyNetLogLevel.logDebug) { TinyLogger.Log("TinyNetClient::OnLocalClientObjectSpawn called but object has never been spawned to client netId:" + s_TinyNetObjectSpawnMessage.networkID); }
			}
		}

		/// <summary>
		/// Called when a scene object is spawned and we are a Listen Server.
		/// </summary>
		/// <param name="netMsg">A wrapper for a <see cref="TinyNetObjectSpawnSceneMessage"/>.</param>
		void OnLocalClientObjectSpawnScene(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetObjectSpawnSceneMessage);

			TinyNetIdentity localObject = GetTinyNetIdentityByNetworkID(s_TinyNetObjectSpawnSceneMessage.networkID);
			if (localObject != null) {
				localObject.OnGiveLocalVisibility();
			}
		}

		/// <summary>
		/// Called when an object is destroyed.
		/// </summary>
		/// <param name="netMsg">A wrapper for a <see cref="TinyNetObjectDestroyMessage"/>.</param>
		void OnObjectDestroy(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetObjectDestroyMessage);

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("TinyNetClient::OnObjDestroy networkID:" + s_TinyNetObjectDestroyMessage.networkID); }

			TinyNetIdentity localObject = GetTinyNetIdentityByNetworkID(s_TinyNetObjectDestroyMessage.networkID);
			if (localObject != null) {
				RemoveTinyNetIdentityFromList(localObject);
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

		/// <summary>
		/// Called when an object is spawned.
		/// </summary>
		/// <param name="netMsg">A wrapper for a <see cref="TinyNetObjectSpawnMessage"/>.</param>
		void OnObjectSpawn(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetObjectSpawnMessage);

			if (s_TinyNetObjectSpawnMessage.assetIndex < 0 || s_TinyNetObjectSpawnMessage.assetIndex > int.MaxValue || s_TinyNetObjectSpawnMessage.assetIndex > TinyNetGameManager.instance.GetAmountOfRegisteredAssets()) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("OnObjSpawn networkID: " + s_TinyNetObjectSpawnMessage.networkID + " has invalid asset Id"); }
				return;
			}
			if (TinyNetLogLevel.logDev) { TinyLogger.Log("Client spawn handler instantiating [networkID:" + s_TinyNetObjectSpawnMessage.networkID + " asset ID:" + s_TinyNetObjectSpawnMessage.assetIndex + " pos:" + s_TinyNetObjectSpawnMessage.position + "]"); }

			TinyNetIdentity localTinyNetIdentity = GetTinyNetIdentityByNetworkID(s_TinyNetObjectSpawnMessage.networkID);
			// Check if this object was already registered in the scene:
			if (localTinyNetIdentity != null) {
				// this object already exists (was in the scene), just apply the update to existing object.
				ApplyInitialState(localTinyNetIdentity, s_TinyNetObjectSpawnMessage.position, s_TinyNetObjectSpawnMessage.initialState, s_TinyNetObjectSpawnMessage.networkID, null, s_TinyNetObjectSpawnMessage.frameTick);
				return;
			}

			GameObject prefab;
			SpawnDelegate handler;

			prefab = TinyNetGameManager.instance.GetPrefabFromAssetId(s_TinyNetObjectSpawnMessage.assetIndex);
			// Check if the prefab is registered in the list of spawnable prefabs.
			if (prefab != null) {
				var obj = (GameObject)Object.Instantiate(prefab, s_TinyNetObjectSpawnMessage.position, Quaternion.identity);

				localTinyNetIdentity = obj.GetComponent<TinyNetIdentity>();

				if (localTinyNetIdentity == null) {
					if (TinyNetLogLevel.logError) { TinyLogger.LogError("Client object spawned for " + s_TinyNetObjectSpawnMessage.assetIndex + " does not have a TinyNetidentity"); }
					return;
				}

				ApplyInitialState(localTinyNetIdentity, s_TinyNetObjectSpawnMessage.position, s_TinyNetObjectSpawnMessage.initialState, s_TinyNetObjectSpawnMessage.networkID, obj, s_TinyNetObjectSpawnMessage.frameTick);
				// If not, check if the prefab have a spawn handler registered.
			} else if (TinyNetGameManager.instance.GetSpawnHandler(s_TinyNetObjectSpawnMessage.assetIndex, out handler)) {
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
				ApplyInitialState(localTinyNetIdentity, s_TinyNetObjectSpawnMessage.position, s_TinyNetObjectSpawnMessage.initialState, s_TinyNetObjectSpawnMessage.networkID, obj, s_TinyNetObjectSpawnMessage.frameTick);
				// If also not, we literally cannot spawn this object and you should feel bad.
			} else {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("Failed to spawn server object, assetId=" + s_TinyNetObjectSpawnMessage.assetIndex + " networkID=" + s_TinyNetObjectSpawnMessage.networkID); }
			}
		}

		/// <summary>
		/// Called when a scene object is spawned.
		/// </summary>
		/// <param name="netMsg">A wrapper for a <see cref="TinyNetObjectSpawnSceneMessage"/>.</param>
		void OnObjectSpawnScene(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetObjectSpawnSceneMessage);

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("Client spawn scene handler instantiating [networkID: " + s_TinyNetObjectSpawnSceneMessage.networkID + " sceneId: " + s_TinyNetObjectSpawnSceneMessage.sceneId + " pos: " + s_TinyNetObjectSpawnSceneMessage.position); }

			TinyNetIdentity localTinyNetIdentity = GetTinyNetIdentityByNetworkID(s_TinyNetObjectSpawnSceneMessage.networkID);
			if (localTinyNetIdentity != null) {
				// this object already exists (was in the scene)
				ApplyInitialState(localTinyNetIdentity, s_TinyNetObjectSpawnSceneMessage.position, s_TinyNetObjectSpawnSceneMessage.initialState, s_TinyNetObjectSpawnSceneMessage.networkID, localTinyNetIdentity.gameObject, s_TinyNetObjectSpawnSceneMessage.frameTick);
				return;
			}

			TinyNetIdentity spawnedId = SpawnSceneObject(s_TinyNetObjectSpawnSceneMessage.sceneId);
			if (spawnedId == null) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("Spawn scene object not found for " + s_TinyNetObjectSpawnSceneMessage.sceneId); }
				return;
			}

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("Client spawn for [networkID :" + s_TinyNetObjectSpawnSceneMessage.networkID + "] [sceneId: " + s_TinyNetObjectSpawnSceneMessage.sceneId + "] obj: " + spawnedId.gameObject.name); }

			ApplyInitialState(spawnedId, s_TinyNetObjectSpawnSceneMessage.position, s_TinyNetObjectSpawnSceneMessage.initialState, s_TinyNetObjectSpawnSceneMessage.networkID, spawnedId.gameObject, s_TinyNetObjectSpawnSceneMessage.frameTick);
		}

		/// <summary>
		/// Called when the initial spawning of objects have been started and when it finishes.
		/// </summary>
		/// <param name="netMsg">A wrapper for a <see cref="TinyNetObjectSpawnFinishedMessage"/>.</param>
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

			foreach (TinyNetIdentity tinyNetId in LocalIdentityObjects.Values) {
				if (tinyNetId.isClient) {
					tinyNetId.OnStartClient();
				}
			}

			_isSpawnFinished = true;
		}

		/// <summary>
		/// Called when we receive an Authorithy message from the server.
		/// </summary>
		/// <param name="netMsg">A wrapper for a <see cref="TinyNetClientAuthorityMessage"/>.</param>
		void OnClientAuthorityMessage(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetClientAuthorityMessage);

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("TinyNetClient::OnClientAuthority for  connectionId=" + netMsg.tinyNetConn.ConnectId + " netId: " + s_TinyNetClientAuthorityMessage.networkID); }

			TinyNetIdentity tni = GetTinyNetIdentityByNetworkID(s_TinyNetClientAuthorityMessage.networkID);

			if (tni != null) {
				tni.HandleClientAuthority(s_TinyNetClientAuthorityMessage.authority);
			}
		}

		/// <summary>
		/// Calls TinyDeserialize in all Components that we received updates for.
		/// </summary>
		/// <param name="netMsg">A wrapper for a <see cref="TinyNetMsgType.StateUpdate"/> message.</param>
		void OnStateUpdateMessage(TinyNetMessageReader netMsg) {
			LastServerTick = netMsg.reader.GetUShort();

			if (TinyNetLogLevel.logDev) { TinyLogger.Log("TinyNetClient::OnStateUpdateMessage frame: " + LastServerTick + " channel: " + netMsg.channelId); }

			while (netMsg.reader.AvailableBytes > 0) {
				int networkID = netMsg.reader.GetInt();

				int rSize = netMsg.reader.GetInt();

				_stateUpdateReader.Clear();
				//Debug.Log("OnStateUpdate: RawDataSize: " + netMsg.reader.RawDataSize + ", Position: " + netMsg.reader.Position + ", rSize: " + rSize);
				_stateUpdateReader.SetSource(netMsg.reader.RawData, netMsg.reader.Position, rSize + netMsg.reader.Position);
				_stateUpdateReader.SetFrameTick(LastServerTick);
				//Debug.Log("OnStateUpdate: _stateUpdateReader " + _stateUpdateReader.RawDataSize + ", Position: " + _stateUpdateReader.Position);

				TinyNetIdentity localObject = GetTinyNetIdentityByNetworkID(networkID);
				if (localObject != null) {
					localObject.TinyDeserialize(_stateUpdateReader, false);
				} else {
					if (TinyNetLogLevel.logWarn) { TinyLogger.LogWarning("Did not find target for sync message for " + networkID); }
				}

				// We jump the reader position to the amount of data we read.
				//netMsg.reader.SetSource(netMsg.reader.RawData, netMsg.reader.Position + rSize, netMsg.reader.RawDataSize);
				netMsg.reader.SkipBytes(rSize);
			}

			foreach (TinyNetIdentity tinyNetId in LocalIdentityObjects.Values) {
				tinyNetId.OnStateUpdate();
			}
		}

		//============ TinyNetIdentity Functions ============//

		/// <summary>
		/// Applies the initial state of an object (it's <see cref="TinyNetSyncVar"/>).
		/// </summary>
		/// <param name="tinyNetId">The <see cref="TinyNetIdentity"/>r.</param>
		/// <param name="position">The position.</param>
		/// <param name="initialState">The initial state.</param>
		/// <param name="networkID">The network identifier.</param>
		/// <param name="newGameObject">The new <see cref="GameObject"/>.</param>
		void ApplyInitialState(TinyNetIdentity tinyNetId, Vector3 position, byte[] initialState, int networkID, GameObject newGameObject, ushort frameTick) {
			if (!tinyNetId.gameObject.activeSelf) {
				tinyNetId.gameObject.SetActive(true);
			}

			tinyNetId.transform.position = position;

			tinyNetId.OnNetworkCreate();

			if (newGameObject != null) {
				tinyNetId.ReceiveNetworkID(new TinyNetworkID(networkID));
			}

			if (initialState != null && initialState.Length > 0) {
				_stateUpdateReader.Clear();
				_stateUpdateReader.SetSource(initialState);
				_stateUpdateReader.SetFrameTick(frameTick);
				tinyNetId.TinyDeserialize(_stateUpdateReader, true);
			}

			// If this is null then this object already existed (already have a networkID and was registered in TinyNetScene), just apply the update to existing object
			if (newGameObject == null) {
				return;
			}

			newGameObject.SetActive(true);
			tinyNetId.ReceiveNetworkID(new TinyNetworkID(networkID));
			AddTinyNetIdentityToList(tinyNetId);

			// If the object was spawned as part of the initial replication (s_TineNetObjectSpawnFinishedMessage.state == 0) it will have it's OnStartClient called by OnObjectSpawnFinished.
			if (_isSpawnFinished) {
				tinyNetId.OnStartClient();
			}
		}

		/// <summary>
		/// Prepares to spawn scene objects.
		/// </summary>
		void PrepareToSpawnSceneObjects() {
			//NOTE: what if is there are already objects in this dict?! should we merge with them?
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

				if (TinyNetLogLevel.logDebug) { TinyLogger.Log("TinyNetClient::PrepareSpawnObjects sceneId: " + tinyNetId.sceneID); }
			}
		}

		/// <summary>
		/// Spawns the scene object.
		/// </summary>
		/// <param name="sceneId">The scene identifier.</param>
		/// <returns></returns>
		TinyNetIdentity SpawnSceneObject(int sceneId) {
			if (_sceneIdentityObjectsToSpawn.ContainsKey(sceneId)) {
				TinyNetIdentity foundId = _sceneIdentityObjectsToSpawn[sceneId];
				_sceneIdentityObjectsToSpawn.Remove(sceneId);

				return foundId;
			}

			return null;
		}

		//===

		/// <summary>
		/// Readies this instance.
		/// </summary>
		/// <returns></returns>
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

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("TinyNetClient::Ready() called with connection [" + connToHost + "]"); }

			var msg = new TinyNetReadyMessage();
			SendMessageByChannelToTargetConnection(msg, DeliveryMethod.ReliableOrdered, connToHost);

			connToHost.isReady = true;

			TinyNetGameManager.instance.OnClientReady();

			return true;
		}

		/// <summary>
		/// Called when a scene change finishes.
		/// </summary>
		public virtual void OnClientSceneChanged() {
			if (TinyNetLogLevel.logDev) { TinyLogger.Log("TinyNetClient::OnClientSceneChanged() called"); }

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
		/// <param name="netMsg">A wrapper for a <see cref="TinyNetStringMessage"/> containing the scene name.</param>
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

		/// <inheritdoc />
		protected override void CreatePlayerAndAdd(TinyNetConnection conn, int playerControllerId) {
			if (TinyNetGameManager.instance.isListenServer) {
				// In a listen server we only have one player controller, the one for the server.
				conn.SetPlayerController<TinyNetPlayerController>(TinyNetServer.instance.GetPlayerControllerFromConnection(connToHost.ConnectId, (short)playerControllerId));
				return;
			}

			base.CreatePlayerAndAdd(conn, playerControllerId);
		}

		/// <summary>
		/// Called when an AddPlayerMessage is received and we are a Listen Server.
		/// </summary>
		/// <param name="netMsg">A wrapper for a <see cref="TinyNetAddPlayerMessage"/>.</param>
		protected virtual void OnLocalAddPlayerMessage(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetAddPlayerMessage);

			CreatePlayerAndAdd(netMsg.tinyNetConn, s_TinyNetAddPlayerMessage.playerControllerId);
		}

		/// <summary>
		/// Called when an AddPlayerMessage is received.
		/// </summary>
		/// <param name="netMsg">A wrapper for a <see cref="TinyNetAddPlayerMessage"/>.</param>
		protected virtual void OnAddPlayerMessage(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetAddPlayerMessage);

			AddPlayerControllerToConnection(netMsg.tinyNetConn, s_TinyNetAddPlayerMessage.playerControllerId);
		}

		/// <summary>
		/// Called when a <see cref="TinyNetRemovePlayerMessage"/> is received.
		/// </summary>
		/// <param name="netMsg">A wrapper for a <see cref="TinyNetRemovePlayerMessage"/>.</param>
		protected virtual void OnRemovePlayerMessage(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetRemovePlayerMessage);

			//netMsg.tinyNetConn.RemovePlayerController(s_TinyNetRemovePlayerMessage.playerControllerId);
			RemovePlayerControllerFromConnection(netMsg.tinyNetConn, s_TinyNetRemovePlayerMessage.playerControllerId);
		}

		/// <summary>
		/// Requests a new <see cref="TinyNetPlayerController"/> to the server.
		/// </summary>
		/// <param name="amountPlayers">The amount of players to create.</param>
		public void RequestAddPlayerControllerToServer(int amountPlayers = 1) {
			if (amountPlayers <= 0) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("RequestAddPlayerControllerToServer() called with amountPlayers <= 0"); }
				return;
			}

			s_TinyNetRequestAddPlayerMessage.amountOfPlayers = (ushort)amountPlayers;
			SendMessageByChannelToTargetConnection(s_TinyNetRequestAddPlayerMessage, DeliveryMethod.ReliableOrdered, connToHost);
		}

		//============ INetEventListener methods ============//

		/// <inheritdoc />
		public override void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) {
			base.OnNetworkReceiveUnconnected(remoteEndPoint, reader, messageType);

			if (messageType == UnconnectedMessageType.DiscoveryResponse && _netManager.PeersCount == 0) {
				if (TinyNetLogLevel.logDev) { TinyLogger.Log("[" + TYPE + "] Received discovery response. Connecting to: " + remoteEndPoint); }

				WriteConnectionRequest(s_recycleWriter);

				_netManager.Connect(remoteEndPoint, s_recycleWriter);
			}
		}
	}
}
