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

		//============ Object Networking ====================//

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
	}
}