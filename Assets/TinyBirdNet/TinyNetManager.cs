using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;

namespace TinyBirdNet {

	public class TinyNetManager : MonoBehaviour, INetEventListener {

		public static TinyNetManager instance;

		protected int maxNumberOfPlayers = 4;
		protected int port = 7777;

		public int MaxNumberOfPlayers { get { return maxNumberOfPlayers; } }
		public int Port { get { return port; } }

		public HashSet<NetPeer> _clients { get; private set; }
		protected NetManager _netManager;

		void Awake() {
			instance = this;

			_clients = new HashSet<NetPeer>();

			AwakeVirtual();
		}

		/** <summary>Please override this function to use the Awake call.</summary> */
		protected virtual void AwakeVirtual() { }

		void Start() {
			StartVirtual();
		}

		/** <summary>Please override this function to use the Start call.</summary> */
		protected virtual void StartVirtual() { }

		void Update() {
			if (_netManager != null) {
				_netManager.PollEvents();
			}

			UpdateVirtual();
		}

		/** <summary>Please override this function to use the Update call.</summary> */
		protected virtual void UpdateVirtual() { }

		void OnDestroy() {
			CleanNetManager();
		}

		protected virtual void CleanNetManager() {
			if (_netManager != null) {
				_netManager.Stop();
			}
		}

		/** <summary>Changes the current max amount of players, this only has an effect before starting a Server.</summary> */
		public virtual void SetMaxNumberOfPlayers(int newNumber) {
			if (_netManager != null) {
				return;
			}
			maxNumberOfPlayers = newNumber;
		}

		/** <summary>Changes the port that will be used for hosting, this only has an effect before starting a Server.</summary> */
		public virtual void SetPort(int newPort) {
			if (_netManager != null) {
				return;
			}
			port = newPort;
		}

		/// <summary>
		/// Prepares this game to work as a server.
		/// </summary>
		public virtual void StartServer() {
			if (_netManager != null) {
				Debug.LogError("StartServer() called multiple times.");
				return;
			}

			_netManager = new NetManager(this, maxNumberOfPlayers, Application.version);
			_netManager.Start(port);

			ConfigureNetManager(true);
		}

		/// <summary>
		/// Prepares this game to work as a client.
		/// </summary>
		public virtual void StartClient() {
			if (_netManager != null) {
				Debug.LogError("StartClient() called multiple times.");
				return;
			}

			_netManager = new NetManager(this, Application.version);
			_netManager.Start();

			ConfigureNetManager(true);
		}

		protected virtual void ConfigureNetManager(bool bUseFixedTime) {
			if (bUseFixedTime) {
				_netManager.UpdateTime = Mathf.FloorToInt(Time.fixedDeltaTime);
			} else {
				_netManager.UpdateTime = 15;
			}
		}

		/// <summary>
		/// Attempts to connect to the target server, StartClient() must have been called before.
		/// </summary>
		/// <param name="hostAddress">An IPv4 or IPv6 string containing the address of the server.</param>
		/// <param name="hostPort">An int representing the port to use for the connection.</param>
		public virtual void ClientConnectTo(string hostAddress, int hostPort) {
			_netManager.Connect(hostAddress, hostPort);
		}

		//============ INetEventListener methods ============//

		public virtual void OnPeerConnected(NetPeer peer) {
			Debug.Log("[SERVER] We have new peer: " + peer.EndPoint);
			_clients.Add(peer);
		}

		public virtual void OnPeerDisconnected(NetPeer peer, DisconnectReason reason, int socketErrorCode) {
			Debug.Log("[SERVER] I don't think this is ever actually called...");
		}

		public virtual void OnNetworkError(NetEndPoint endPoint, int socketErrorCode) {
			Debug.Log("[SERVER] error " + socketErrorCode + " at: " + endPoint);
		}

		public virtual void OnNetworkReceiveUnconnected(NetEndPoint remoteEndPoint, NetDataReader reader, UnconnectedMessageType messageType) {
			if (messageType == UnconnectedMessageType.DiscoveryRequest) {
				Debug.Log("[SERVER] Received discovery request. Send discovery response");
				_netManager.SendDiscoveryResponse(new byte[] { 1 }, remoteEndPoint);
			}
		}

		public virtual void OnNetworkLatencyUpdate(NetPeer peer, int latency) {
			Debug.Log("[SERVER] Latency update for peer: " + peer.EndPoint + " " + latency + "ms");
		}

		public virtual void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
			Debug.Log("[SERVER] peer disconnected " + peer.EndPoint + ", info: " + disconnectInfo.Reason);
			_clients.Remove(peer);
		}

		public virtual void OnNetworkReceive(NetPeer peer, NetDataReader reader) {
			Debug.Log("[SERVER] On network receive what? D:");
		}
	}
}
