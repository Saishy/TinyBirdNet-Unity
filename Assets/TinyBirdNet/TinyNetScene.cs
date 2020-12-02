using UnityEngine;
using LiteNetLib;
using System.Collections.Generic;
using LiteNetLib.Utils;
using TinyBirdUtils;
using TinyBirdNet.Messaging;
using System.Net;
using System.Net.Sockets;

namespace TinyBirdNet {

	/// <summary>
	/// Represents a Scene, which is all data required to reproduce the game state.
	/// </summary>
	/// <seealso cref="LiteNetLib.INetEventListener" />
	public abstract class TinyNetScene : INetEventListener, INetLogger {

		/// <summary>
		/// Sugar for generating debug logs.
		/// </summary>
		public virtual string TYPE { get { return "Abstract"; } }

		public EventBasedNetListener NetListenerEvents { get; protected set; } = new EventBasedNetListener();

		//protected static Dictionary<string, GameObject> guidToPrefab;

		/// <summary>
		/// int is the NetworkID of the TinyNetIdentity object.
		/// </summary>
		private static Dictionary<int, TinyNetIdentity> _localIdentityObjects = new Dictionary<int, TinyNetIdentity>();

		protected Dictionary<int, TinyNetIdentity> LocalIdentityObjects {
			get {
				return _localIdentityObjects;
			}
		}

		/// <summary>
		/// If using this, always Reset before use!
		/// </summary>
		protected static NetDataWriter s_recycleWriter = new NetDataWriter();

		/// <summary>
		/// A message reader used to prevent garbage collection.
		/// </summary>
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

		/// <summary>
		/// The <see cref="ITinyNetMessage"/> handlers.
		/// </summary>
		protected TinyNetMessageHandlers _tinyMessageHandlers = new TinyNetMessageHandlers();

		/// <summary>
		/// All connections to this scene.
		/// </summary>
		protected List<TinyNetConnection> _tinyNetConns;
		/// <summary>
		/// Gets the connections to this scene.
		/// </summary>
		/// <value>
		/// The connection list.
		/// </value>
		public List<TinyNetConnection> tinyNetConns { get { return _tinyNetConns; } }

		/// <summary>
		/// Gets or sets the connection to host.
		/// </summary>
		/// <value>
		/// The connection to host.
		/// </value>
		public TinyNetConnection connToHost { get; protected set; }

		/// <summary>
		/// The <see cref="NetManager"/>.
		/// </summary>
		protected NetManager _netManager;

		/// <summary>
		/// Access to the NetManager.
		/// <para> Please use this with caution. </para>
		/// </summary>
		/// <value>
		/// The NetManager this scene uses.
		/// </value>
		public NetManager NetManager {
			get {
				return _netManager;
			}
		}

		/// <summary>
		/// Gets or sets the current tick.
		/// <para>(This is not syncronized between client and server)</para>
		/// </summary>
		/// <value>
		/// The current tick.
		/// </value>
		public ushort CurrentTick {
			get; protected set;
		}

		/// <summary>
		/// Returns true if socket is listening and update thread is running.
		/// </summary>
		public virtual bool isRunning {
			get {
				if (_netManager == null) {
					return false;
				}

				return _netManager.IsRunning;
			}
		}

		/// <summary>
		/// Returns true if it's connected to at least one peer.
		/// </summary>
		public virtual bool isConnected {
			get {
				if (_netManager == null) {
					return false;
				}

				return _tinyNetConns.Count > 0;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TinyNetScene"/> class.
		/// </summary>
		public TinyNetScene() {
			_tinyNetConns = new List<TinyNetConnection>(TinyNetGameManager.Instance.MaxNumberOfPlayers);

			/*if (guidToPrefab == null) {
				guidToPrefab = TinyNetGameManager.instance.GetDictionaryOfAssetGUIDToPrefabs();
			}*/
		}

		/// <summary>
		/// Registers the message handlers.
		/// </summary>
		protected virtual void RegisterMessageHandlers() {
			RegisterHandlerSafe(TinyNetMsgType.Rpc, OnRPCMessage);
			//RegisterHandlerSafe(MsgType.SyncEvent, OnSyncEventMessage);
			//RegisterHandlerSafe(MsgType.AnimationTrigger, NetworkAnimator.OnAnimationTriggerClientMessage);
		}

		public virtual void TinyNetUpdate() {
			CurrentTick = (ushort)((CurrentTick + 1) % TinyNetGameManager.Instance.MaxFrameSequence);
		}

		/// <summary>
		/// Registers a message handler.
		/// </summary>
		/// <param name="msgType">Type of the message.</param>
		/// <param name="handler">The handler.</param>
		public void RegisterHandler(ushort msgType, TinyNetMessageDelegate handler) {
			_tinyMessageHandlers.RegisterHandler(msgType, handler);
		}

		/// <summary>
		/// Registers a message handler safely.
		/// </summary>
		/// <param name="msgType">Type of the message.</param>
		/// <param name="handler">The handler.</param>
		public void RegisterHandlerSafe(ushort msgType, TinyNetMessageDelegate handler) {
			_tinyMessageHandlers.RegisterHandlerSafe(msgType, handler);
		}

		public void UnRegisterHandler(ushort msgType) {
			_tinyMessageHandlers.UnregisterHandler(msgType);
		}

		public void ClearHandlers() {
			_tinyMessageHandlers.ClearMessageHandlers();
		}

		/// <summary>
		/// It is called from TinyNetGameManager, handles PollEvents().
		/// </summary>
		public virtual void PollEvents() {
			if (_netManager != null) {
				_netManager.PollEvents();
			}
		}

		public virtual void CallUpdateOnControllers() {
			for (int i = 0; i < tinyNetConns.Count; i++) {
				tinyNetConns[i].CallUpdateOnControllers();
			}
		}

		/// <summary>
		/// Clears the net manager.
		/// </summary>
		public virtual void ClearNetManager() {
			if (_netManager != null) {
				_netManager.Stop();
				_tinyNetConns.Clear();
			}
		}

		/// <summary>
		/// Configures the net manager.
		/// </summary>
		/// <param name="bUseFixedTime">if set to <c>true</c> use fixed update time.</param>
		protected virtual void ConfigureNetManager(bool bUseFixedTime) {
			if (bUseFixedTime) {
				_netManager.UpdateTime = Mathf.FloorToInt(Time.fixedDeltaTime * 1000);
			} else {
				_netManager.UpdateTime = 15;
			}

			_netManager.AutoRecycle = true;

			_netManager.PingInterval = TinyNetGameManager.Instance.PingInterval;
			_netManager.NatPunchEnabled = TinyNetGameManager.Instance.bNatPunchEnabled;

			RegisterMessageHandlers();
		}

		/// <summary>
		/// Toggles the nat punching.
		/// </summary>
		/// <param name="bNewState">The new nat punching state.</param>
		public virtual void ToggleNatPunching(bool bNewState) {
			_netManager.NatPunchEnabled = bNewState;
		}

		/// <summary>
		/// Sets the ping interval.
		/// </summary>
		/// <param name="newPingInterval">The new ping interval.</param>
		public virtual void SetPingInterval(int newPingInterval) {
			if (_netManager != null) {
				_netManager.PingInterval = newPingInterval;
			}
		}

		/// <summary>
		/// Creates a <see cref="TinyNetConnection"/> for the given <see cref="NetPeer"/>.
		/// </summary>
		/// <param name="peer">The peer.</param>
		/// <returns>A new <see cref="TinyNetConnection"/>.</returns>
		protected abstract TinyNetConnection CreateTinyNetConnection(NetPeer peer);

		/// <summary>
		/// Returns the <see cref="TinyNetConnection"/> with the given connection identifier.
		/// </summary>
		/// <param name="connId">The connection identifier.</param>
		/// <returns></returns>
		public TinyNetConnection GetTinyNetConnection(long connId) {
			for (int i = 0; i < tinyNetConns.Count; i++) {
				if (tinyNetConns[i].ConnectId == connId) {
					return tinyNetConns[i];
				}
			}

			return null;
		}

		/// <summary>
		/// Returns the <see cref="TinyNetConnection"/> with the given <see cref="NetPeer"/>.
		/// </summary>
		/// <param name="peer">The peer.</param>
		/// <returns></returns>
		public TinyNetConnection GetTinyNetConnection(NetPeer peer) {
			for (int i = 0; i < tinyNetConns.Count; i++) {
				if (tinyNetConns[i].netPeer == peer) {
					return tinyNetConns[i];
				}
			}

			return null;
		}

		/// <summary>
		/// Removes the connection.
		/// </summary>
		/// <param name="nConn">The connection.</param>
		/// <returns></returns>
		protected virtual bool RemoveTinyNetConnection(TinyNetConnection nConn) {
			for (int i = 0; i < tinyNetConns.Count; i++) {
				if (tinyNetConns[i] == nConn) {
					tinyNetConns.RemoveAt(i);
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Removes the connection.
		/// </summary>
		/// <param name="peer">The peer.</param>
		/// <returns></returns>
		protected virtual bool RemoveTinyNetConnection(NetPeer peer) {
			for (int i = 0; i < tinyNetConns.Count; i++) {
				if (tinyNetConns[i].netPeer == peer) {
					tinyNetConns.RemoveAt(i);
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Removes the connection.
		/// </summary>
		/// <param name="connectId">The connection identifier.</param>
		/// <returns></returns>
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

		/// <summary>
		/// Adds the <see cref="TinyNetIdentity"/> to list.
		/// </summary>
		/// <param name="netIdentity">The net identity.</param>
		public static void AddTinyNetIdentityToList(TinyNetIdentity netIdentity) {
			_localIdentityObjects.Add(netIdentity.TinyInstanceID.NetworkID, netIdentity);
		}

		/// <summary>
		/// Removes the <see cref="TinyNetIdentity"/> from the list.
		/// </summary>
		/// <param name="netIdentity">The net identity.</param>
		public static bool RemoveTinyNetIdentityFromList(TinyNetIdentity netIdentity) {
			return _localIdentityObjects.Remove(netIdentity.TinyInstanceID.NetworkID);
		}

		/// <summary>
		/// Gets a <see cref="TinyNetIdentity"/> by it's network identifier.
		/// </summary>
		/// <param name="nId">The NetworkID.</param>
		/// <returns></returns>
		public static TinyNetIdentity GetTinyNetIdentityByNetworkID(int nId) {
			TinyNetIdentity reference = null;
			if (nId >= 0) {
				_localIdentityObjects.TryGetValue(nId, out reference);
			}
			//return _localIdentityObjects.ContainsKey(nId) ? _localIdentityObjects[nId] : null;
			return reference;
		}

		/// <summary>Gets a <see cref="ITinyNetComponent"/> by it's network identifier.</summary>
		/// <param name="networkId">The network identifier.</param>
		/// <param name="localId">The local identifier on the TinyNetIdentity.</param>
		/// <returns></returns>
		public static ITinyNetComponent GetTinyNetComponentByNetworkID(int networkId, byte localId) {
			ITinyNetComponent reference = null;
			//return _localNetObjects.ContainsKey(nId) ? _localNetObjects[nId] : null;
			if (networkId >= 0) {
				TinyNetIdentity tinyNetRef = GetTinyNetIdentityByNetworkID(networkId);
				reference = tinyNetRef.GetComponentById(localId);
			}
			return reference;
		}

		//============ TinyNetMessages Networking ===========//

		/// <summary>
		/// Reads the message and calls the correct handler.
		/// </summary>
		/// <param name="reader">The reader.</param>
		/// <param name="peer">The peer.</param>
		/// <returns></returns>
		ushort ReadMessageAndCallDelegate(NetDataReader reader, NetPeer peer, DeliveryMethod deliveryMethod) {
			ushort msgType = reader.GetUShort();

			if (_tinyMessageHandlers.Contains(msgType)) {
				recycleMessageReader.msgType = msgType;
				recycleMessageReader.reader = reader;
				recycleMessageReader.tinyNetConn = GetTinyNetConnection(peer);
				recycleMessageReader.channelId = deliveryMethod;

				_tinyMessageHandlers.GetHandler(msgType)(recycleMessageReader);
			}

			return msgType;
		}

		/// <summary>
		/// Sends the message by a specific channel to host.
		/// </summary>
		/// <param name="msg">The message.</param>
		/// <param name="sendOptions">The send options.</param>
		public virtual void SendMessageByChannelToHost(ITinyNetMessage msg, DeliveryMethod sendOptions, byte channel = 0) {
			s_recycleWriter.Reset();

			s_recycleWriter.Put(msg.msgType);
			msg.Serialize(s_recycleWriter);

			connToHost.Send(s_recycleWriter, sendOptions, channel);
		}

		/// <summary>
		/// Sends the message by a specific channel to target connection.
		/// </summary>
		/// <param name="msg">The message.</param>
		/// <param name="sendOptions">The send options.</param>
		/// <param name="tinyNetConn">The connection.</param>
		public virtual void SendMessageByChannelToTargetConnection(ITinyNetMessage msg, DeliveryMethod sendOptions, TinyNetConnection tinyNetConn, byte channel = 0) {
			s_recycleWriter.Reset();

			s_recycleWriter.Put(msg.msgType);
			msg.Serialize(s_recycleWriter);

			tinyNetConn.Send(s_recycleWriter, sendOptions, channel);
		}

		/// <summary>
		/// Sends the message by a specific channel to all connections.
		/// </summary>
		/// <param name="msg">The message.</param>
		/// <param name="sendOptions">The send options.</param>
		public virtual void SendMessageByChannelToAllConnections(ITinyNetMessage msg, DeliveryMethod sendOptions, byte channel = 0) {
			s_recycleWriter.Reset();

			s_recycleWriter.Put(msg.msgType);
			msg.Serialize(s_recycleWriter);
			
			for (int i = 0; i < tinyNetConns.Count; i++) {
				tinyNetConns[i].Send(s_recycleWriter, sendOptions, channel);
			}
		}

		/// <summary>
		/// Sends the message by a specific channel to all ready connections.
		/// </summary>
		/// <param name="msg">The message.</param>
		/// <param name="sendOptions">The send options.</param>
		public virtual void SendMessageByChannelToAllReadyConnections(ITinyNetMessage msg, DeliveryMethod sendOptions, byte channel = 0) {
			s_recycleWriter.Reset();

			s_recycleWriter.Put(msg.msgType);
			msg.Serialize(s_recycleWriter);

			for (int i = 0; i < tinyNetConns.Count; i++) {
				if (!tinyNetConns[i].bReady) {
					return;
				}
				tinyNetConns[i].Send(s_recycleWriter, sendOptions, channel);
			}
		}

		/// <summary>
		/// Sends the message by a specific channel to all observers of a <see cref="TinyNetIdentity"/>.
		/// </summary>
		/// <param name="tni">The <see cref="TinyNetIdentity"/>.</param>
		/// <param name="msg">The message.</param>
		/// <param name="sendOptions">The send options.</param>
		public virtual void SendMessageByChannelToAllObserversOf(TinyNetIdentity tni, ITinyNetMessage msg, DeliveryMethod sendOptions, byte channel = 0) {
			s_recycleWriter.Reset();

			s_recycleWriter.Put(msg.msgType);
			msg.Serialize(s_recycleWriter);

			for (int i = 0; i < tinyNetConns.Count; i++) {
				if (!tinyNetConns[i].IsObservingNetIdentity(tni)) {
					return;
				}
				tinyNetConns[i].Send(s_recycleWriter, sendOptions, channel);
			}
		}

		//============ INetEventListener methods ============//

		/// <summary>
		/// On peer connection requested
		/// </summary>
		/// <param name="request">Request information (EndPoint, internal id, additional data)</param>
		public virtual void OnConnectionRequest(ConnectionRequest request) {
			if (TinyNetLogLevel.logDev) { TinyLogger.Log("[" + TYPE + "] Connection requested from: " + request.RemoteEndPoint); }

			if (NetListenerEvents != null) {
				((INetEventListener)NetListenerEvents).OnConnectionRequest(request);
			}
		}

		/// <summary>
		/// New remote peer connected to host, or client connected to remote host
		/// </summary>
		/// <param name="peer">Connected peer object</param>
		public virtual void OnPeerConnected(NetPeer peer) {
			if (TinyNetLogLevel.logDev) { TinyLogger.Log("[" + TYPE + "] We have new peer: " + peer.EndPoint + " connectId: " + peer.Id); }

			if (NetListenerEvents != null) {
				((INetEventListener)NetListenerEvents).OnPeerConnected(peer);
			}

			TinyNetConnection nConn = CreateTinyNetConnection(peer);

			OnConnectionCreated(nConn);
		}

		/// <summary>
		/// Peer disconnected
		/// </summary>
		/// <param name="peer">disconnected peer</param>
		/// <param name="disconnectInfo">additional info about reason, errorCode or data received with disconnect message</param>
		public virtual void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
			if (TinyNetLogLevel.logDev) { TinyLogger.Log("[" + TYPE + "] disconnected from: " + peer.EndPoint + " because " + disconnectInfo.Reason); }

			if (NetListenerEvents != null) {
				((INetEventListener)NetListenerEvents).OnPeerDisconnected(peer, disconnectInfo);
			}

			TinyNetConnection nConn = GetTinyNetConnection(peer);
			OnDisconnect(nConn, disconnectInfo);

			RemoveTinyNetConnection(nConn);
		}

		/// <summary>
		/// Network error (on send or receive)
		/// </summary>
		/// <param name="endPoint">From endPoint (can be null)</param>
		/// <param name="socketErrorCode">Socket error code</param>
		public virtual void OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode) {
			if (TinyNetLogLevel.logError) { TinyLogger.LogError("[" + TYPE + "] error " + socketErrorCode + " at: " + endPoint); }

			if (NetListenerEvents != null) {
				((INetEventListener)NetListenerEvents).OnNetworkError(endPoint, socketErrorCode);
			}
		}

		/// <summary>
		/// Received some data
		/// </summary>
		/// <param name="peer">From peer</param>
		/// <param name="reader">DataReader containing all received data</param>
		/// <param name="deliveryMethod">Type of received packet</param>
		public virtual void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod) {
			string msgType = TinyNetMsgType.MsgTypeToString(reader.PeekUShort());
			if (TinyNetLogLevel.logDev) { TinyLogger.Log("[" + TYPE + "] received message " + msgType + " from: " + peer.EndPoint + " channel: " + deliveryMethod.ToString()); }
			if (NetListenerEvents != null) {
				((INetEventListener)NetListenerEvents).OnNetworkReceive(peer, reader, deliveryMethod);
			}
			ReadMessageAndCallDelegate(reader, peer, deliveryMethod);
		}

		/// <summary>
		/// Received unconnected message
		/// </summary>
		/// <param name="remoteEndPoint">From address (IP and Port)</param>
		/// <param name="reader">Message data</param>
		/// <param name="messageType">Message type (simple, discovery request or responce)</param>
		public virtual void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) {
			if (TinyNetLogLevel.logDev) { TinyLogger.Log("[" + TYPE + "] Received Unconnected message from: " + remoteEndPoint); }
			if (NetListenerEvents != null) {
				((INetEventListener)NetListenerEvents).OnNetworkReceiveUnconnected(remoteEndPoint, reader, messageType);
			}
		}

		/// <summary>
		/// Latency information updated
		/// </summary>
		/// <param name="peer">Peer with updated latency</param>
		/// <param name="latency">latency value in milliseconds</param>
		public virtual void OnNetworkLatencyUpdate(NetPeer peer, int latency) {
			if (TinyNetLogLevel.logDev) { TinyLogger.Log("[" + TYPE + "] Latency update for peer: " + peer.EndPoint + " " + latency + "ms"); }
			if (NetListenerEvents != null) {
				((INetEventListener)NetListenerEvents).OnNetworkLatencyUpdate(peer, latency);
			}
		}

		/*public virtual void OnNetworkReceive(NetPeer peer, NetDataReader reader) {
			TinyLogger.Log("[" + TYPE + "] On network receive from: " + peer.EndPoint);
		}*/

		//============ INetLogger ========================//

		public void WriteNet(NetLogLevel level, string str, params object[] args) {
			switch (level) {
				case NetLogLevel.Warning:
					if (TinyNetLogLevel.logWarn) { TinyLogger.LogFormat(str, args); }
					break;
				case NetLogLevel.Error:
					if (TinyNetLogLevel.logError) { TinyLogger.LogFormat(str, args); }
					break;
				case NetLogLevel.Trace:
					if (TinyNetLogLevel.logDev) { TinyLogger.LogFormat(str, args); }
					break;
				case NetLogLevel.Info:
					if (TinyNetLogLevel.logInfo) { TinyLogger.LogFormat(str, args); }
					break;
			}
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
		protected virtual void OnDisconnect(TinyNetConnection nConn, DisconnectInfo disconnectInfo) {
			// If it's null, the connection was never accepted in the first place.
			if (nConn != null) {
				nConn.OnDisconnect();
			}
		}

		//============ TinyNetMessages Handlers =============//

		/// <summary>
		/// Called when an RPC message is received.
		/// TODO FIX THIS!
		/// </summary>
		/// <param name="netMsg">The net message.</param>
		protected virtual void OnRPCMessage(TinyNetMessageReader netMsg) {
			/*netMsg.ReadMessage(s_TinyNetRPCMessage);

			ITinyNetComponent iObj = GetTinyNetObjectByNetworkID(s_TinyNetRPCMessage.networkID);

			if (iObj == null) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("TinyNetScene::OnRPCMessage No ITinyNetObject with the HNetworkID: " + s_TinyNetRPCMessage.networkID); }
				return;
			}

			recycleMessageReader.reader.SetSource(s_TinyNetRPCMessage.parameters);
			iObj.InvokeRPC(s_TinyNetRPCMessage.rpcMethodIndex, recycleMessageReader.reader);*/
		}

		//============ Players Methods ======================//

		/// <summary>
		/// Attempts to add a player controller to the connection.
		/// </summary>
		/// <param name="conn">The connection.</param>
		/// <param name="playerControllerId">The player controller identifier.</param>
		protected virtual void AddPlayerControllerToConnection(TinyNetConnection conn, byte playerControllerId, byte[] msgData) {
			if (playerControllerId < 0) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError($"[{TYPE}] AddPlayerControllerToConnection() called with playerControllerId < 0"); }
				return;
			}

			if (playerControllerId < conn.playerControllers.Count && conn.playerControllers[playerControllerId].IsValid) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError($"[{TYPE}] There is already a player with id={playerControllerId} for this connection"); }
				return;
			}

			CreatePlayerAndAdd(conn, playerControllerId, msgData);
		}

		/// <summary>
		/// Removes a player controller from connection.
		/// </summary>
		/// <param name="conn">The connection.</param>
		/// <param name="playerControllerId">The player controller identifier.</param>
		protected virtual void RemovePlayerControllerFromConnection(TinyNetConnection conn, byte playerControllerId) {
			TinyNetPlayerController playerController = conn.GetPlayerController(playerControllerId);
			
			TinyNetGameManager.Instance.OnPlayerControllerRemoved(playerController);
			conn.RemovePlayerController(playerController);
		}

		/// <summary>
		/// Creates a player controller and adds it to the connection.
		/// </summary>
		/// <param name="conn">The connection.</param>
		/// <param name="playerControllerId">The player controller identifier.</param>
		protected virtual void CreatePlayerAndAdd(TinyNetConnection conn, byte playerControllerId, byte[] msgData) {
			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("[" + TYPE + "] CreatePlayerAndAdd Conn: " + conn + ", playerId: " + playerControllerId); }
			TinyNetPlayerController playerController = TinyNetGameManager.Instance.CreatePlayerController(conn, playerControllerId, msgData);
			
			conn.SetPlayerController<TinyNetPlayerController>(playerController);
			TinyNetGameManager.Instance.OnPlayerControllerAdded(playerController);
		}
	}
}
