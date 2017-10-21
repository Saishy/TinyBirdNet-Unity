using UnityEngine;
using System.Collections;
using LiteNetLib;
using LiteNetLib.Utils;

namespace TinyBirdNet {

	public class TinyNetClientManager : TinyNetManager {

		public static TinyNetClientManager instance;

		public override string TYPE { get { return "CLIENT"; } }

		public virtual bool StartClient() {
			if (_netManager != null) {
				Debug.LogError("StartClient() called multiple times.");
				return false;
			}

			_netManager = new NetManager(this, Application.version);
			_netManager.Start();

			ConfigureNetManager(true);

			Debug.Log("[CLIENT] Started client");

			return true;
		}

		public virtual void ClientConnectTo(string hostAddress, int hostPort) {
			Debug.Log("[CLIENT] Attempt to connect at adress: " + hostAddress + ":" + hostPort);

			_netManager.Connect(hostAddress, hostPort);
		}
	}
}