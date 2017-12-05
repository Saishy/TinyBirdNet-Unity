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
		protected static NetDataWriter recycleWriter;

		protected NetPeer _peer;

		public NetPeer netPeer { get { return _peer; } }

		List<TinyNetPlayerController> _playerControllers = new List<TinyNetPlayerController>();

		public List<TinyNetPlayerController> playerControllers { get { return _playerControllers; } }

		/// <summary>
		/// This is a list of objects the connection is able to observe, aka, are spawned and synced.
		/// <para>At a client this list would just be a copy of the one in TinyNetScene so it is always empty.</para>
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
				if (TinyNetLogLevel.logWarn) { TinyLogger.LogWarning("RemoveFromVisList() called but object with networkdID: " + tni.NetworkID + " is not shown"); }
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
			while (player.playerControllerId >= _playerControllers.Count) {
				_playerControllers.Add(new T());
			}

			_playerControllers[player.playerControllerId] = player;
			_playerControllers[player.playerControllerId].Conn = this;
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

		// Get player controller from connection's list
		public TinyNetPlayerController GetPlayerController(short playerControllerId) {
			if (playerControllers.Count > 0) {
				for (int i = 0; i < playerControllers.Count; i++) {
					if (playerControllers[i].IsValid && playerControllers[i].playerControllerId == playerControllerId) {
						return playerControllers[i];
					}
				}

				return null;
			}

			return null;
		}

		public void GetPlayerInputMessage(TinyNetMessageReader netMsg) {
			_playerControllers[TinyNetInputMessage.PeekAtPlayerControllerId(netMsg)].GetInputMessage(netMsg);
		}
	}
}
