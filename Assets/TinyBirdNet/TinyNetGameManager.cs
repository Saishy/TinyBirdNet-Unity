using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;

namespace TinyBirdNet {

	public class TinyNetGameManager : MonoBehaviour {

		public static TinyNetGameManager instance;

		protected int maxNumberOfPlayers = 4;
		protected int port = 7777;
		public bool bNatPunchEnabled { get; protected set; }

		public int MaxNumberOfPlayers { get { return maxNumberOfPlayers; } }
		public int Port { get { return port; } }

		protected TinyNetServerManager serverManager;
		protected TinyNetClientManager clientManager;

		void Awake() {
			instance = this;

			serverManager = new TinyNetServerManager();
			clientManager = new TinyNetClientManager();

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
			if (serverManager != null) {
				serverManager.InternalUpdate();
			}
			if (clientManager != null) {
				clientManager.InternalUpdate();
			}

			UpdateVirtual();
		}

		/** <summary>Please override this function to use the Update call.</summary> */
		protected virtual void UpdateVirtual() { }

		void OnDestroy() {
			ClearNetManager();
		}

		protected virtual void ClearNetManager() {
			if (serverManager != null) {
				serverManager.ClearNetManager();
			}

			if (clientManager != null) {
				clientManager.ClearNetManager();
			}
		}

		/** <summary>Changes the current max amount of players, this only has an effect before starting a Server.</summary> */
		public virtual void SetMaxNumberOfPlayers(int newNumber) {
			if (serverManager != null) {
				return;
			}
			maxNumberOfPlayers = newNumber;
		}

		/** <summary>Changes the port that will be used for hosting, this only has an effect before starting a Server.</summary> */
		public virtual void SetPort(int newPort) {
			if (serverManager != null) {
				return;
			}
			port = newPort;
		}

		public virtual void ToggleNatPunching(bool bNewState) {
			bNatPunchEnabled = bNewState;
		}

		/// <summary>
		/// Prepares this game to work as a server.
		/// </summary>
		public virtual void StartServer() {
			serverManager.StartServer(port, maxNumberOfPlayers);
		}

		/// <summary>
		/// Prepares this game to work as a client.
		/// </summary>
		public virtual void StartClient() {
			clientManager.StartClient();
		}

		/// <summary>
		/// Attempts to connect to the target server, StartClient() must have been called before.
		/// </summary>
		/// <param name="hostAddress">An IPv4 or IPv6 string containing the address of the server.</param>
		/// <param name="hostPort">An int representing the port to use for the connection.</param>
		public virtual void ClientConnectTo(string hostAddress, int hostPort) {
			clientManager.ClientConnectTo(hostAddress, hostPort);
		}
	}
}
