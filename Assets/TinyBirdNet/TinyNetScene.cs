using UnityEngine;
using System.Collections;
using LiteNetLib;
using System.Collections.Generic;
using LiteNetLib.Utils;
using TinyBirdUtils;
using TinyBirdNet.Messaging;

namespace TinyBirdNet {

	public abstract class TinyNetScene : System.Object, INetEventListener {

		public virtual string TYPE { get { return "Abstract"; } }

		//protected static Dictionary<string, GameObject> guidToPrefab;

		/// <summary>
		/// uint is the NetworkID of the TinyNetIdentity object.
		/// </summary>
		protected static Dictionary<int, TinyNetIdentity> _localIdentityObjects = new Dictionary<int, TinyNetIdentity>();

		/// <summary>
		/// uint is the NetworkID of the ITinyNetObject object.
		/// </summary>
		protected static Dictionary<int, ITinyNetObject> _localNetObjects = new Dictionary<int, ITinyNetObject>();

		/// <summary>
		/// If using this, always Reset before use!
		/// </summary>
		protected static NetDataWriter recycleWriter = new NetDataWriter();

		// static message objects to avoid runtime-allocations
		protected static TinyNetObjectDestroyMessage s_TinyNetObjectDestroyMessage = new TinyNetObjectDestroyMessage();
		protected static TinyNetObjectSpawnMessage s_TinyNetObjectSpawnMessage = new TinyNetObjectSpawnMessage();
		protected static TinyNetOwnerMessage s_TinyNetOwnerMessage = new TinyNetOwnerMessage();
		protected static TinyNetObjectSpawnSceneMessage s_TinyNetObjectSpawnSceneMessage = new TinyNetObjectSpawnSceneMessage();
		protected static TinyNetObjectSpawnFinishedMessage s_TineNetObjectSpawnFinishedMessage = new TinyNetObjectSpawnFinishedMessage();
		//static ClientAuthorityMessage s_ClientAuthorityMessage = new ClientAuthorityMessage();

		protected TinyNetMessageHandlers _tinyMessageHandlers = new TinyNetMessageHandlers();

		protected List<TinyNetConnection> _tinyNetConns;
		public List<TinyNetConnection> tinyNetConns { get { return _tinyNetConns; } }

		protected NetManager _netManager;

		public bool isRunning { get {
				if (_netManager == null) {
					return false;
				}

				return _netManager.IsRunning;
		} }

		public TinyNetScene() {
			_tinyNetConns = new List<TinyNetConnection>(TinyNetGameManager.instance.MaxNumberOfPlayers);

			/*if (guidToPrefab == null) {
				guidToPrefab = TinyNetGameManager.instance.GetDictionaryOfAssetGUIDToPrefabs();
			}*/
		}

		protected virtual void RegisterMessageHandlers() {
            //RegisterHandlerSafe(MsgType.Rpc, OnRPCMessage);
            //RegisterHandlerSafe(MsgType.SyncEvent, OnSyncEventMessage);
            //RegisterHandlerSafe(MsgType.AnimationTrigger, NetworkAnimator.OnAnimationTriggerClientMessage);
        }

		public void RegisterHandlerSafe(ushort msgType, TinyNetMessageDelegate handler) {
			_tinyMessageHandlers.RegisterHandlerSafe(msgType, handler);
		}

		/// <summary>
		/// It is called from TinyNetGameManager Update(), handles PollEvents().
		/// </summary>
		public void InternalUpdate() {
			if (_netManager != null) {
				_netManager.PollEvents();
			}
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

		protected virtual bool RemoveTinyNetConnection(NetPeer peer) {
			foreach (TinyNetConnection tinyNetCon in tinyNetConns) {
				if (tinyNetCon.netPeer == peer) {
					tinyNetConns.Remove(tinyNetCon);
					return true;
				}
			}

			return false;
		}

		protected virtual bool RemoveTinyNetConnection(long connectId) {
			foreach (TinyNetConnection tinyNetCon in tinyNetConns) {
				if (tinyNetCon.ConnectId == connectId) {
					tinyNetConns.Remove(tinyNetCon);
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

		//============ TinyNetMessages Networking ===========//

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

		//============ INetEventListener methods ============//

		public virtual void OnPeerConnected(NetPeer peer) {
			TinyLogger.Log("[" + TYPE + "] We have new peer: " + peer.EndPoint);

			tinyNetConns.Add(new TinyNetConnection(peer));
		}

		public virtual void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
			TinyLogger.Log("[" + TYPE + "] disconnected from: " + peer.EndPoint + "because " + disconnectInfo.Reason);

			RemoveTinyNetConnection(peer);
		}

		public virtual void OnNetworkError(NetEndPoint endPoint, int socketErrorCode) {
			TinyLogger.Log("[" + TYPE + "] error " + socketErrorCode + " at: " + endPoint);
		}

		public virtual void OnNetworkReceiveUnconnected(NetEndPoint remoteEndPoint, NetDataReader reader, UnconnectedMessageType messageType) {
			TinyLogger.Log("[" + TYPE + "] Received Unconnected message from: " + remoteEndPoint);

			if (messageType == UnconnectedMessageType.DiscoveryRequest) {
				OnDiscoveryRequestReceived(remoteEndPoint, reader);
			}
		}

		public virtual void OnNetworkLatencyUpdate(NetPeer peer, int latency) {
			TinyLogger.Log("[" + TYPE + "] Latency update for peer: " + peer.EndPoint + " " + latency + "ms");
		}

		public virtual void OnNetworkReceive(NetPeer peer, NetDataReader reader) {
			TinyLogger.Log("[" + TYPE + "] On network receive from: " + peer.EndPoint);
		}

		//============ Network Events =======================//

		protected virtual void OnDiscoveryRequestReceived(NetEndPoint remoteEndPoint, NetDataReader reader) {
			TinyLogger.Log("[" + TYPE + "] Received discovery request. Send discovery response");
			_netManager.SendDiscoveryResponse(new byte[] { 1 }, remoteEndPoint);
		}
	}
}