using UnityEngine;
using System.Collections;
using LiteNetLib;
using LiteNetLib.Utils;

namespace TinyBirdNet {

	public class TinyNetServerManager : TinyNetManager {

		public static TinyNetServerManager instance;

		public override string TYPE { get { return "SERVER"; } }

		public virtual bool StartServer(int port, int maxNumberOfPlayers) {
			if (_netManager != null) {
				Debug.LogError("StartServer() called multiple times.");
				return false;
			}

			_netManager = new NetManager(this, maxNumberOfPlayers, Application.version);
			_netManager.Start(port);

			ConfigureNetManager(true);

			Debug.Log("[SERVER] Started server at port: " + port + " with maxNumberOfPlayers: " + maxNumberOfPlayers);

			return true;
		}
	}
}