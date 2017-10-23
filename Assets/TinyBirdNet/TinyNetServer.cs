using UnityEngine;
using System.Collections;
using LiteNetLib;
using LiteNetLib.Utils;
using TinyBirdUtils;

namespace TinyBirdNet {

	public class TinyNetServer : TinyNetScene {

		public static TinyNetServer instance;

		public override string TYPE { get { return "SERVER"; } }

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
	}
}