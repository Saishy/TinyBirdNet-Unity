using UnityEngine;
using System.Collections;
using LiteNetLib;
using LiteNetLib.Utils;
using TinyBirdUtils;

namespace TinyBirdNet {

	public class TinyNetClient : TinyNetConnection {

		public static TinyNetClient instance;

		public override string TYPE { get { return "CLIENT"; } }

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
	}
}