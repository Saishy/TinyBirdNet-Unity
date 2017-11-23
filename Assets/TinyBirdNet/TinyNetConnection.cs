using TinyBirdUtils;
using TinyBirdNet.Messaging;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Generic;

namespace TinyBirdNet {

	/// <summary>
	/// A container for a connection to a NetPeer.
	/// </summary>
	public class TinyNetConnection {

		protected NetPeer _peer;

		public NetPeer netPeer { get { return _peer; } }

		List<TinyNetPlayerController> _playerControllers = new List<TinyNetPlayerController>();

		public List<TinyNetPlayerController> playerControllers { get { return _playerControllers; } }

		public bool isReady;

		public TinyNetConnection(NetPeer newPeer) {
			_peer = newPeer;
		}

		public long ConnectId {	get { return _peer.ConnectId; }
		}

		public void Send(byte[] data, SendOptions options) {
			_peer.Send(data, options);
		}

		public void Send(NetDataWriter dataWriter, SendOptions options) {
			_peer.Send(dataWriter, options);
		}

		public override string ToString() {
			return string.Format("EndPoint: {0} ConnectId: {1} isReady: {2}", netPeer.EndPoint, ConnectId, isReady);
		}

		//============ Player Controllers ===================//

		public void SetPlayerController<T>(TinyNetPlayerController player) where T : TinyNetPlayerController, new() {
			while (player.playerControllerId >= _playerControllers.Count) {
				_playerControllers.Add(new T());
			}

			_playerControllers[player.playerControllerId] = player;
		}

		public void RemovePlayerController(short playerControllerId) {
			int count = _playerControllers.Count;

			while (count >= 0) {
				if (playerControllerId == count && playerControllerId == _playerControllers[count].playerControllerId) {
					_playerControllers[count] = new TinyNetPlayerController();
					return;
				}
				count -= 1;
			}

			if (TinyNetLogLevel.logError) { TinyLogger.LogError("RemovePlayerController for playerControllerId " + playerControllerId + " not found"); }
		}

		// Get player controller from connection's list
		public bool GetPlayerController(short playerControllerId, out TinyNetPlayerController playerController) {
			playerController = null;

			if (playerControllers.Count > 0) {
				for (int i = 0; i < playerControllers.Count; i++) {
					if (playerControllers[i].IsValid && playerControllers[i].playerControllerId == playerControllerId) {
						playerController = playerControllers[i];

						return true;
					}
				}

				return false;
			}

			return false;
		}
	}
}
