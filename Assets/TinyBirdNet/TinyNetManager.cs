using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;

namespace TinyBirdNet {

	public class TinyNetManager : MonoBehaviour, INetEventListener {

		public int MaxAmountOfPlayers = 4;
		public int Port = 7777;

		private HashSet<NetPeer> _clients;
		private NetManager _netManager;

		void Awake() {
			_clients = new HashSet<NetPeer>();

			AwakeInit();
		}

		protected void AwakeInit() { }

		void Start() {
			StartInit();
		}

		protected void StartInit() { }

		void Update() {

		}

		public void StartServer() {
			_netManager = new NetManager(this, MaxAmountOfPlayers, Application.version);
		}

		public void OnPeerConnected(NetPeer peer) {
			Debug.Log("[SERVER] We have new peer: " + peer.EndPoint);
			_clients.Add(peer);
		}

		public void OnPeerDisconnected(NetPeer peer, DisconnectReason reason, int socketErrorCode) {
			Debug.Log("[SERVER] I don't think this is ever actually called...");
		}

		public void OnNetworkError(NetEndPoint endPoint, int socketErrorCode) {
			Debug.Log("[SERVER] error " + socketErrorCode + " at: " + endPoint);
		}

		public void OnNetworkReceiveUnconnected(NetEndPoint remoteEndPoint, NetDataReader reader, UnconnectedMessageType messageType) {
			if (messageType == UnconnectedMessageType.DiscoveryRequest) {
				Debug.Log("[SERVER] Received discovery request. Send discovery response");
				_netManager.SendDiscoveryResponse(new byte[] { 1 }, remoteEndPoint);
			}
		}

		public void OnNetworkLatencyUpdate(NetPeer peer, int latency) {
			Debug.Log("[SERVER] Latency update for peer: " + peer.EndPoint + " " + latency + "ms");
		}

		public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
			Debug.Log("[SERVER] peer disconnected " + peer.EndPoint + ", info: " + disconnectInfo.Reason);
			_clients.Remove(peer);
		}

		public void OnNetworkReceive(NetPeer peer, NetDataReader reader) {
			Debug.Log("[SERVER] On network receive what? D:");
		}
	}
}
