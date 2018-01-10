using UnityEngine;
using System.Collections;
using LiteNetLib;
using System.Collections.Generic;
using LiteNetLib.Utils;
using TinyBirdUtils;
using TinyBirdNet.Messaging;
using System;

namespace TinyBirdNet {

	public abstract class TinyNetScene : System.Object, INetEventListener {

		public virtual string TYPE { get { return "Abstract"; } }

		//protected static Dictionary<string, GameObject> guidToPrefab;

		public static Action<TinyNetConnection, int> createPlayerAction;

		/// <summary>
		/// int is the NetworkID of the TinyNetIdentity object.
		/// </summary>
		protected static Dictionary<int, TinyNetIdentity> _localIdentityObjects = new Dictionary<int, TinyNetIdentity>();

		/// <summary>
		/// int is the NetworkID of the ITinyNetObject object.
		/// </summary>
		protected static Dictionary<int, ITinyNetObject> _localNetObjects = new Dictionary<int, ITinyNetObject>();

		/// <summary>
		/// If using this, always Reset before use!
		/// </summary>
		protected static NetDataWriter recycleWriter = new NetDataWriter();

		protected static TinyNetMessageReader recycleMessageReader = new TinyNetMessageReader();

		// static message objects to avoid runtime-allocations
		protected static TinyNetRPCMessage s_TinyNetRPCMessage = new TinyNetRPCMessage();
		protected static TinyNetObjectHideMessage s_TinyNetObjectHideMessage = new TinyNetObjectHideMessage();
		protected static TinyNetObjectDestroyMessage s_TinyNetObjectDestroyMessage = new TinyNetObjectDestroyMessage();
		protected static TinyNetObjectSpawnMessage s_TinyNetObjectSpawnMessage = new TinyNetObjectSpawnMessage();
		protected static TinyNetObjectSpawnSceneMessage s_TinyNetObjectSpawnSceneMessage = new TinyNetObjectSpawnSceneMessage();
		protected static TinyNetObjectSpawnFinishedMessage s_TineNetObjectSpawnFinishedMessage = new TinyNetObjectSpawnFinishedMessage();
		protected static TinyNetAddPlayerMessage s_TinyNetAddPlayerMessage = new TinyNetAddPlayerMessage();
		protected static TinyNetRemovePlayerMessage s_TinyNetRemovePlayerMessage = new TinyNetRemovePlayerMessage();
		protected static TinyNetRequestAddPlayerMessage s_TinyNetRequestAddPlayerMessage = new TinyNetRequestAddPlayerMessage();
		protected static TinyNetRequestRemovePlayerMessage s_TinyNetRequestRemovePlayerMessage = new TinyNetRequestRemovePlayerMessage();
		protected static TinyNetClientAuthorityMessage s_TinyNetClientAuthorityMessage = new TinyNetClientAuthorityMessage();

		protected TinyNetMessageHandlers _tinyMessageHandlers = new TinyNetMessageHandlers();

		protected List<TinyNetConnection> _tinyNetConns;
		public List<TinyNetConnection> tinyNetConns { get { return _tinyNetConns; } }

		public TinyNetConnection connToHost { get; protected set; }

		protected NetManager _netManager;

		protected int currentFixedFrame = 0;

		/// <summary>
		/// Returns true if socket listening and update thread is running.
		/// </summary>
		public bool isRunning { get {
				if (_netManager == null) {
					return false;
				}

				return _netManager.IsRunning;
		} }

		/// <summary>
		/// Returns true if it's connected to at least one peer.
		/// </summary>
		public bool isConnected {
			get {
				if (_netManager == null) {
					return false;
				}

				return _tinyNetConns.Count > 0;
			}
		}

		public TinyNetScene() {
			_tinyNetConns = new List<TinyNetConnection>(TinyNetGameManager.instance.MaxNumberOfPlayers);

			/*if (guidToPrefab == null) {
				guidToPrefab = TinyNetGameManager.instance.GetDictionaryOfAssetGUIDToPrefabs();
			}*/
		}

		protected virtual void RegisterMessageHandlers() {
			RegisterHandlerSafe(TinyNetMsgType.Rpc, OnRPCMessage);
			//RegisterHandlerSafe(MsgType.SyncEvent, OnSyncEventMessage);
			//RegisterHandlerSafe(MsgType.AnimationTrigger, NetworkAnimator.OnAnimationTriggerClientMessage);
		}

		public void RegisterHandler(ushort msgType, TinyNetMessageDelegate handler) {
			_tinyMessageHandlers.RegisterHandler(msgType, handler);
		}

		public void RegisterHandlerSafe(ushort msgType, TinyNetMessageDelegate handler) {
			_tinyMessageHandlers.RegisterHandlerSafe(msgType, handler);
		}

		/// <summary>
		/// It is called from TinyNetGameManager Update(), handles PollEvents().
		/// </summary>
		public virtual void InternalUpdate() {
			if (_netManager != null) {
				_netManager.PollEvents();
			}
		}

		public virtual void TinyNetUpdate() {
		}

		public virtual void ClearNetManager() {
			if (_netManager != null) {
				_netManager.Stop();
			}
		}

		protected virtual void ConfigureNetManager(bool bUseFixedTime) {
			if (bUseFixedTime) {
				_netManager.UpdateTime = Mathf.FloorToInt(Time.fixedDeltaTime * 1000);
			} else {
				_netManager.UpdateTime = 15;
			}

			_netManager.PingInterval = TinyNetGameManager.instance.PingInterval;
			_netManager.NatPunchEnabled = TinyNetGameManager.instance.bNatPunchEnabled;

			RegisterMessageHandlers();
		}

		public virtual void ToggleNatPunching(bool bNewState) {
			_netManager.NatPunchEnabled = bNewState;
		}

		public virtual void SetPingInterval(int newPingInterval) {
			if (_netManager != null) {
				_netManager.PingInterval = newPingInterval;
			}
		}

		protected virtual TinyNetConnection CreateTinyNetConnection(NetPeer peer) {
			//No default implemention
			return null;
		}

		protected TinyNetConnection GetTinyNetConnection(long connId) {
			for (int i = 0; i < tinyNetConns.Count; i++) {
				if (tinyNetConns[i].ConnectId == connId) {
					return tinyNetConns[i];
				}
			}

			return null;
		}

		protected TinyNetConnection GetTinyNetConnection(NetPeer peer) {
			for (int i = 0; i < tinyNetConns.Count; i++) {
				if (tinyNetConns[i].netPeer == peer) {
					return tinyNetConns[i];
				}
			}

			return null;
		}

		protected virtual bool RemoveTinyNetConnection(TinyNetConnection nConn) {
			for (int i = 0; i < tinyNetConns.Count; i++) {
				if (tinyNetConns[i] == nConn) {
					tinyNetConns.RemoveAt(i);
					return true;
				}
			}

			return false;
		}

		protected virtual bool RemoveTinyNetConnection(NetPeer peer) {
			for (int i = 0; i < tinyNetConns.Count; i++) {
				if (tinyNetConns[i].netPeer == peer) {
					tinyNetConns.RemoveAt(i);
					return true;
				}
			}

			return false;
		}

		protected virtual bool RemoveTinyNetConnection(long connectId) {
			for (int i = 0; i < tinyNetConns.Count; i++) {
				if (tinyNetConns[i].ConnectId == connectId) {
					tinyNetConns.RemoveAt(i);
					return true;
				}
			}

			return false;
		}

		//============ Object Networking ====================//

		public static void AddTinyNetIdentityToList(TinyNetIdentity netIdentity) {
			_localIdentityObjects.Add(netIdentity.NetworkID, netIdentity);
		}

		public static void AddTinyNetObjectToList(ITinyNetObject netObj) {
			_localNetObjects.Add(netObj.NetworkID, netObj);
		}

		public static void RemoveTinyNetIdentityFromList(TinyNetIdentity netIdentity) {
			_localIdentityObjects.Remove(netIdentity.NetworkID);
		}

		public static void RemoveTinyNetObjectFromList(ITinyNetObject netObj) {
			_localNetObjects.Remove(netObj.NetworkID);
		}

		public static TinyNetIdentity GetTinyNetIdentityByNetworkID(int nId) {
			TinyNetIdentity reference = null;
			_localIdentityObjects.TryGetValue(nId, out reference);
			//return _localIdentityObjects.ContainsKey(nId) ? _localIdentityObjects[nId] : null;
			return reference;
		}

		public static ITinyNetObject GetTinyNetObjectByNetworkID(int nId) {
			ITinyNetObject reference = null;
			//return _localNetObjects.ContainsKey(nId) ? _localNetObjects[nId] : null;
			_localNetObjects.TryGetValue(nId, out reference);
			return reference;
		}

		//============ TinyNetMessages Networking ===========//

		ushort ReadMessageAndCallDelegate(NetDataReader reader, NetPeer peer) {
			ushort msgType = reader.GetUShort();

			if (_tinyMessageHandlers.Contains(msgType)) {
				recycleMessageReader.msgType = msgType;
				recycleMessageReader.reader = reader;
				recycleMessageReader.tinyNetConn = GetTinyNetConnection(peer);
				recycleMessageReader.channelId = SendOptions.ReliableOrdered; //@TODO: I currently don't know if it's possible to get from which channel a message came.

				_tinyMessageHandlers.GetHandler(msgType)(recycleMessageReader);
			}

			return msgType;
		}

		public virtual void SendMessageByChannelToHost(ITinyNetMessage msg, SendOptions sendOptions) {
			recycleWriter.Reset();

			recycleWriter.Put(msg.msgType);
			msg.Serialize(recycleWriter);

			connToHost.Send(recycleWriter, sendOptions);
		}

		public virtual void SendMessageByChannelToTargetConnection(ITinyNetMessage msg, SendOptions sendOptions, TinyNetConnection tinyNetConn) {
			recycleWriter.Reset();

			recycleWriter.Put(msg.msgType);
			msg.Serialize(recycleWriter);

			tinyNetConn.Send(recycleWriter, sendOptions);
		}

		public virtual void SendMessageByChannelToAllConnections(ITinyNetMessage msg, SendOptions sendOptions) {
			recycleWriter.Reset();

			recycleWriter.Put(msg.msgType);
			msg.Serialize(recycleWriter);
			
			for (int i = 0; i < tinyNetConns.Count; i++) {
				tinyNetConns[i].Send(recycleWriter, sendOptions);
			}
		}

		public virtual void SendMessageByChannelToAllReadyConnections(ITinyNetMessage msg, SendOptions sendOptions) {
			recycleWriter.Reset();

			recycleWriter.Put(msg.msgType);
			msg.Serialize(recycleWriter);

			for (int i = 0; i < tinyNetConns.Count; i++) {
				if (!tinyNetConns[i].isReady) {
					return;
				}
				tinyNetConns[i].Send(recycleWriter, sendOptions);
			}
		}

		public virtual void SendMessageByChannelToAllObserversOf(TinyNetIdentity tni, ITinyNetMessage msg, SendOptions sendOptions) {
			recycleWriter.Reset();

			recycleWriter.Put(msg.msgType);
			msg.Serialize(recycleWriter);

			for (int i = 0; i < tinyNetConns.Count; i++) {
				if (!tinyNetConns[i].IsObservingNetIdentity(tni)) {
					return;
				}
				tinyNetConns[i].Send(recycleWriter, sendOptions);
			}
		}

		//============ INetEventListener methods ============//

		public virtual void OnPeerConnected(NetPeer peer) {
			TinyLogger.Log("[" + TYPE + "] We have new peer: " + peer.EndPoint + " connectId: " + peer.ConnectId);

			TinyNetConnection nConn = CreateTinyNetConnection(peer);

			OnConnectionCreated(nConn);
		}

		public virtual void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
			TinyLogger.Log("[" + TYPE + "] disconnected from: " + peer.EndPoint + " because " + disconnectInfo.Reason);

			TinyNetConnection nConn = GetTinyNetConnection(peer);
			OnDisconnect(nConn);

			RemoveTinyNetConnection(nConn);
		}

		public virtual void OnNetworkError(NetEndPoint endPoint, int socketErrorCode) {
			TinyLogger.Log("[" + TYPE + "] error " + socketErrorCode + " at: " + endPoint);
		}

		public virtual void OnNetworkReceive(NetPeer peer, NetDataReader reader) {
			TinyLogger.Log("[" + TYPE + "] received message " + TinyNetMsgType.MsgTypeToString(ReadMessageAndCallDelegate(reader, peer)) + " from: " + peer.EndPoint);
		}

		/// <summary>
		/// Saishy: I literally have no idea what this is.
		/// </summary>
		/// <param name="remoteEndPoint"></param>
		/// <param name="reader"></param>
		/// <param name="messageType"></param>
		public virtual void OnNetworkReceiveUnconnected(NetEndPoint remoteEndPoint, NetDataReader reader, UnconnectedMessageType messageType) {
			TinyLogger.Log("[" + TYPE + "] Received Unconnected message from: " + remoteEndPoint);

			if (messageType == UnconnectedMessageType.DiscoveryRequest) {
				OnDiscoveryRequestReceived(remoteEndPoint, reader);
			}
		}

		public virtual void OnNetworkLatencyUpdate(NetPeer peer, int latency) {
			TinyLogger.Log("[" + TYPE + "] Latency update for peer: " + peer.EndPoint + " " + latency + "ms");
		}

		/*public virtual void OnNetworkReceive(NetPeer peer, NetDataReader reader) {
			TinyLogger.Log("[" + TYPE + "] On network receive from: " + peer.EndPoint);
		}*/

		protected virtual void OnDiscoveryRequestReceived(NetEndPoint remoteEndPoint, NetDataReader reader) {
			TinyLogger.Log("[" + TYPE + "] Received discovery request. Send discovery response");
			_netManager.SendDiscoveryResponse(new byte[] { 1 }, remoteEndPoint);
		}

		//============ TinyNetEvents ========================//

		/// <summary>
		/// Called after a peer has connected and a TinyNetConnection was created for it.
		/// </summary>
		/// <param name="nConn">The connection created.</param>
		protected virtual void OnConnectionCreated(TinyNetConnection nConn) {
		}

		/// <summary>
		/// Called after a peer has been disconnected but before the TinyNetConnection has been removed from the list.
		/// </summary>
		/// <param name="nConn">The connection that disconnected.</param>
		protected virtual void OnDisconnect(TinyNetConnection nConn) {
		}

		//============ TinyNetMessages Handlers =============//

		protected virtual void OnRPCMessage(TinyNetMessageReader netMsg) {
			netMsg.ReadMessage(s_TinyNetRPCMessage);

			ITinyNetObject iObj = GetTinyNetObjectByNetworkID(s_TinyNetRPCMessage.networkID);

			if (iObj == null) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("TinyNetScene::OnRPCMessage No ITinyNetObject with the HNetworkID: " + s_TinyNetRPCMessage.networkID); }
				return;
			}

			recycleMessageReader.reader.SetSource(s_TinyNetRPCMessage.parameters);
			iObj.InvokeRPC(s_TinyNetRPCMessage.rpcMethodIndex, recycleMessageReader.reader);
		}

		//============ Players Methods ======================//

		protected virtual void AddPlayerControllerToConnection(TinyNetConnection conn, int playerControllerId) {
			if (playerControllerId < 0) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("AddPlayerControllerToConnection() called with playerControllerId < 0"); }
				return;
			}

			if (playerControllerId < conn.playerControllers.Count && conn.playerControllers[playerControllerId].IsValid) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("There is already a player with that playerControllerId for this connection"); }
				return;
			}

			CreatePlayerAndAdd(conn, playerControllerId);
		}

		protected virtual void RemovePlayerControllerFromConnection(TinyNetConnection conn, short playerControllerId) {
			conn.RemovePlayerController(playerControllerId);
		}

		protected virtual void CreatePlayerAndAdd(TinyNetConnection conn, int playerControllerId) {
			if (createPlayerAction != null) {
				createPlayerAction(conn, playerControllerId);
				return;
			}
			// If no action is set, we just use default implementation
			conn.SetPlayerController<TinyNetPlayerController>(new TinyNetPlayerController((short)playerControllerId, conn));
		}
	}
}
