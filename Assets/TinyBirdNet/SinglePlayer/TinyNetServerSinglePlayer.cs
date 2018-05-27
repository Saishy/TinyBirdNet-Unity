using LiteNetLib;
using System.Collections;
using System.Collections.Generic;
using TinyBirdUtils;
using UnityEngine;

namespace TinyBirdNet {

	public class TinyNetServerSinglePlayer : TinyNetServer {

		/// <inheritdoc />
		public override string TYPE { get { return "SERVER: SinglePlayer"; } }

		/// <inheritdoc />
		public override bool isRunning {
			get {
				return bStarted;
			}
		}

		/// <inheritdoc />
		public override bool isConnected {
			get {
				return _tinyNetConns.Count > 0;
			}
		}

		private bool bStarted = false;

		/// <inheritdoc />
		public override void InternalUpdate() {
		}

		private TinyNetClientSinglePlayer _clientSinglePlayer;

		public TinyNetClientSinglePlayer ClientSinglePlayerManager {
			get {
				if (_clientSinglePlayer == null) {
					_clientSinglePlayer = TinyNetClient.instance as TinyNetClientSinglePlayer;
				}

				return _clientSinglePlayer;
			}
		}

		/// <inheritdoc />
		public override bool StartServer(int port, int maxNumberOfPlayers) {
			if (bStarted) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("[" + TYPE + "] StartServer() called multiple times."); }
				return false;
			}

			bStarted = true;

			ConfigureNetManager(true);

			if (TinyNetLogLevel.logDev) { TinyLogger.Log("[" + TYPE + "] Started server with maxNumberOfPlayers: " + maxNumberOfPlayers); }

			return true;
		}

		protected override void ConfigureNetManager(bool bUseFixedTime) {
			RegisterMessageHandlers();
		}

		public virtual void OnSinglePlayerConnection() {
			if (TinyNetLogLevel.logDev) { TinyLogger.Log("[" + TYPE + "] Connected"); }

			//NetPeer netPeer = new NetPeer(null, new NetEndPoint("localhost", 8130), connectionAttempt.ConnectId);
			TinyNetConnection nConn = CreateTinyNetConnection(null);

			ClientSinglePlayerManager.OnSinglePlayerConnection();
		}

		/// <inheritdoc />
		protected override TinyNetConnection CreateTinyNetConnection(NetPeer peer) {
			TinyNetConnection tinyConn = new TinyNetLocalConnectionToClient(peer);

			tinyNetConns.Add(tinyConn);

			return tinyConn;
		}
	}
}