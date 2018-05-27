using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TinyBirdUtils;

namespace TinyBirdNet {

	public class TinyNetClientSinglePlayer : TinyNetClient {

		/// <inheritdoc />
		public override string TYPE { get { return "CLIENT: SinglePlayer"; } }

		public override bool isRunning {
			get {
				return bStarted;
			}
		}

		public override bool isConnected {
			get {
				return _tinyNetConns.Count > 0;
			}
		}

		private bool bStarted = false;

		private TinyNetServerSinglePlayer _serverSinglePlayer;

		public TinyNetServerSinglePlayer ServerSinglePlayerManager {
			get {
				if (_serverSinglePlayer == null) {
					_serverSinglePlayer = TinyNetServer.instance as TinyNetServerSinglePlayer;
				}

				return _serverSinglePlayer;
			}
		}

		/// <inheritdoc />
		public override bool StartClient() {
			if (bStarted) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("[" + TYPE + "] StartClient() called multiple times."); }
				return false;
			}

			bStarted = true;

			ConfigureNetManager(true);

			if (TinyNetLogLevel.logDev) { TinyLogger.Log("[" + TYPE + "] Started client"); }

			return true;
		}

		protected override void ConfigureNetManager(bool bUseFixedTime) {
			RegisterMessageHandlers();
		}

		/// <inheritdoc />
		public override void ClientConnectTo(string hostAddress, int hostPort) {
			if (TinyNetLogLevel.logDev) { TinyLogger.Log("[" + TYPE + "] Connecting"); }

			recycleWriter.Reset();
			recycleWriter.Put(TinyNetGameManager.instance.multiplayerConnectKey);
			recycleWriter.Put(TinyNetGameManager.ApplicationGUIDString);

			SinglePlayerConnect();
		}

		protected virtual void SinglePlayerConnect() {
			ServerSinglePlayerManager.OnSinglePlayerConnection();
		}

		public virtual void OnSinglePlayerConnection() {
			if (TinyNetLogLevel.logDev) { TinyLogger.Log("[" + TYPE + "] Connected"); }

			TinyNetConnection nConn = CreateTinyNetConnection(null);
		}
	}
}
