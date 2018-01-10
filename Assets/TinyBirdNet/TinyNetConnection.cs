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

		/// <summary>
		/// If using this, always Reset before use!
		/// </summary>
		protected static NetDataWriter recycleWriter = new NetDataWriter();

		protected NetPeer _peer;

		public NetPeer netPeer { get { return _peer; } }

		List<TinyNetPlayerController> _playerControllers = new List<TinyNetPlayerController>();

		public List<TinyNetPlayerController> playerControllers { get { return _playerControllers; } }

		/// <summary>
		/// This is a list of objects the connection is able to observe, aka, are spawned and synced.
		/// </summary>
		protected HashSet<TinyNetIdentity> _observingNetObjects = new HashSet<TinyNetIdentity>();
		/**<summary>A hash containing the networkIds of objects owned by this connection.</summary>*/
		protected HashSet<int> _ownedObjectsId;

		public bool isReady;

		public TinyNetConnection(NetPeer newPeer) {
			_peer = newPeer;
		}

		public long ConnectId {	get { return _peer.ConnectId; }
		}

		public override string ToString() {
			return string.Format("EndPoint: {0} ConnectId: {1} isReady: {2}", netPeer.EndPoint, ConnectId, isReady);
		}

		//============ Network Data =========================//

		public void Send(byte[] data, SendOptions options) {
			_peer.Send(data, options);
		}

		public void Send(NetDataWriter dataWriter, SendOptions options) {
			_peer.Send(dataWriter, options);
		}

		public void Send(ITinyNetMessage msg, SendOptions options) {
			recycleWriter.Reset();

			recycleWriter.Put(msg.msgType);
			msg.Serialize(recycleWriter);

			Send(recycleWriter, options);
		}

		//============ Network Identity =====================//

		public bool IsObservingNetIdentity(TinyNetIdentity tni) {
			return _observingNetObjects.Contains(tni);
		}

		/// <summary>
		/// Always call this to spawn an object to a client, or you will have sync issues.
		/// </summary>
		/// <param name="tni"></param>
		public void ShowObjectToConnection(TinyNetIdentity tni) {
			if (_observingNetObjects.Contains(tni)) {
				if (TinyNetLogLevel.logDev) { TinyLogger.Log("ShowObjectToConnection() called but object with networkdID: " + tni.NetworkID + " is already shown"); }
				return;
			}

			_observingNetObjects.Add(tni);

			// spawn tiny for this conn
			TinyNetServer.instance.ShowForConnection(tni, this);
		}

		/// <summary>
		/// Always call this to hide an object from a client, or you will have sync issues.
		/// </summary>
		/// <param name="tni"></param>
		public void HideObjectToConnection(TinyNetIdentity tni, bool isDestroyed) {
			if (!_observingNetObjects.Contains(tni)) {
				if (TinyNetLogLevel.logDev) { TinyLogger.LogWarning("RemoveFromVisList() called but object with networkdID: " + tni.NetworkID + " is not shown"); }
				return;
			}

			_observingNetObjects.Remove(tni);

			if (!isDestroyed) {
				// hide tni for this conn
				TinyNetServer.instance.HideForConnection(tni, this);
			}
		}

		public void AddOwnedObject(TinyNetIdentity obj) {
			if (_ownedObjectsId == null) {
				_ownedObjectsId = new HashSet<int>();
			}

			_ownedObjectsId.Add(obj.NetworkID);
		}

		public void RemoveOwnedObject(TinyNetIdentity obj) {
			if (_ownedObjectsId == null) {
				return;
			}

			_ownedObjectsId.Remove(obj.NetworkID);
		}

		//============ Player Controllers ===================//

		public void SetPlayerController<T>(TinyNetPlayerController player) where T : TinyNetPlayerController, new() {
			/*while (player.playerControllerId >= _playerControllers.Count) {
				_playerControllers.Add(new T());
			}

			_playerControllers[player.playerControllerId] = player;*/
			_playerControllers.Add(player);
		}

		public void RemovePlayerController(short playerControllerId) {
			/*int count = _playerControllers.Count;

			while (count >= 0) {
				if (playerControllerId == count && playerControllerId == _playerControllers[count].playerControllerId) {
					_playerControllers[count] = new TinyNetPlayerController();
					return;
				}
				count -= 1;
			}*/
			TinyNetPlayerController tPC;
			if (GetPlayerController(playerControllerId, out tPC)) {
				_playerControllers.Remove(tPC);
				return;
			}

			if (TinyNetLogLevel.logError) { TinyLogger.LogError("RemovePlayerController for playerControllerId " + playerControllerId + " not found"); }
		}

		public TinyNetPlayerController GetPlayerController(short playerControllerId) {
			for (int i = 0; i < _playerControllers.Count; i++) {
				if (_playerControllers[i].IsValid && _playerControllers[i].playerControllerId == playerControllerId) {
					return _playerControllers[i];
				}
			}

			return null;
		}

		// Get player controller from connection's list
		public bool GetPlayerController(short playerControllerId, out TinyNetPlayerController playerController) {
			playerController = null;

			/*if (_playerControllers.Count > playerControllerId) {
				for (int i = 0; i < playerControllers.Count; i++) {
					if (playerControllers[i].IsValid && playerControllers[i].playerControllerId == playerControllerId) {
						playerController = playerControllers[i];

						return true;
					}
				}				

				return false;
			}*/

			for (int i = 0; i < _playerControllers.Count; i++) {
				if (_playerControllers[i].IsValid && _playerControllers[i].playerControllerId == playerControllerId) {
					playerController = _playerControllers[i];

					return true;
				}
			}

			return false;
		}

		// Get player controller from connection's list
		public T GetPlayerController<T>(short playerControllerId) where T : TinyNetPlayerController {
			for (int i = 0; i < _playerControllers.Count; i++) {
				if (_playerControllers[i].IsValid && _playerControllers[i].playerControllerId == playerControllerId) {
					return (T)_playerControllers[i];
				}
			}

			/*if (_playerControllers.Count > playerControllerId) {
				if (_playerControllers[playerControllerId] != null && _playerControllers[playerControllerId].IsValid) {
					return (T)_playerControllers[playerControllerId];
				}

				return null;
			}*/

			return null;
		}

		public TinyNetPlayerController GetFirstPlayerController() {
			return _playerControllers[0];
		}

		public void GetPlayerInputMessage(TinyNetMessageReader netMsg) {
			GetPlayerController(TinyNetInputMessage.PeekAtPlayerControllerId(netMsg)).GetInputMessage(netMsg);
		}
	}
}
