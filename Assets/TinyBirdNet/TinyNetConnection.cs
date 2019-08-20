using TinyBirdUtils;
using TinyBirdNet.Messaging;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Generic;

namespace TinyBirdNet {

	/// <summary>
	/// A container for a connection to a <see cref="NetPeer"/>.
	/// </summary>
	public class TinyNetConnection {

		/// <summary>
		/// If using this, always Reset before use!
		/// </summary>
		protected static NetDataWriter recycleWriter = new NetDataWriter();

		/// <summary>
		/// The <see cref="NetPeer"/> of this connection.
		/// </summary>
		protected NetPeer _peer;

		/// <summary>
		/// Gets the <see cref="NetPeer"/>.
		/// </summary>
		/// <value>
		/// The <see cref="NetPeer"/>.
		/// </value>
		public NetPeer netPeer { get { return _peer; } }

		public readonly string ApplicationGUIDString;

		/// <summary>
		/// A list of <see cref="TinyNetPlayerController"/> of this connection.
		/// </summary>
		List<TinyNetPlayerController> _playerControllers = new List<TinyNetPlayerController>();

		/// <summary>
		/// Gets the <see cref="TinyNetPlayerController"/>.
		/// </summary>
		/// <value>
		/// The <see cref="TinyNetPlayerController"/>.
		/// </value>
		public List<TinyNetPlayerController> playerControllers { get { return _playerControllers; } }

		/// <summary>
		/// This is a list of objects the connection is able to observe, aka, are spawned and synced.
		/// </summary>
		protected HashSet<TinyNetIdentity> _observingNetObjects = new HashSet<TinyNetIdentity>();
		///<summary>A hash containing the NetworkIDs of objects owned by this connection.</summary>
		protected HashSet<int> _ownedObjectsId;

		public HashSet<TinyNetIdentity> ObservingNetObjects {
			get {
				return _observingNetObjects;
			}
		}

		/// <summary>
		/// If this instance is ready
		/// </summary>
		public bool isReady;

		/// <summary>
		/// Initializes a new instance of the <see cref="TinyNetConnection"/> class.
		/// </summary>
		/// <param name="newPeer">The <see cref="NetPeer"/>.</param>
		public TinyNetConnection(NetPeer newPeer) {
			_peer = newPeer;

			if (_peer.Tag != null) {
				ApplicationGUIDString = (string)_peer.Tag;
			}
		}

		/// <summary>
		/// Gets the connect identifier.
		/// </summary>
		/// <value>
		/// The connect identifier.
		/// </value>
		public virtual long ConnectId {	get { return _peer.Id; }
		}

		/// <summary>
		/// Returns a <see cref="System.String" /> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String" /> that represents this instance.
		/// </returns>
		public override string ToString() {
			return string.Format("{0} EndPoint: {1} ConnectId: {2} isReady: {3}", GetType(), netPeer.EndPoint, ConnectId, isReady);
		}

		//============ Network Data =========================//

		/// <summary>
		/// Sends the specified data.
		/// </summary>
		/// <param name="data">The data.</param>
		/// <param name="options">The options.</param>
		public virtual void Send(byte[] data, DeliveryMethod options) {
			_peer.Send(data, options);
		}

		/// <summary>
		/// Sends the specified data.
		/// </summary>
		/// <param name="dataWriter">The data writer.</param>
		/// <param name="options">The options.</param>
		public virtual void Send(NetDataWriter dataWriter, DeliveryMethod options) {
			_peer.Send(dataWriter, options);
		}

		/// <summary>
		/// Sends the specified <see cref="ITinyNetMessage"/>.
		/// </summary>
		/// <param name="msg">The message.</param>
		/// <param name="options">The options.</param>
		public virtual void Send(ITinyNetMessage msg, DeliveryMethod options) {
			recycleWriter.Reset();

			recycleWriter.Put(msg.msgType);
			msg.Serialize(recycleWriter);

			Send(recycleWriter, options);
		}

		//============ Network Identity =====================//

		/// <summary>
		/// Determines whether this instance is observing the specified <see cref="TinyNetIdentity"/>.
		/// </summary>
		/// <param name="tni">The <see cref="TinyNetIdentity"/>.</param>
		/// <returns>
		///   <c>true</c> if is observing the specified <see cref="TinyNetIdentity"/>; otherwise, <c>false</c>.
		/// </returns>
		public bool IsObservingNetIdentity(TinyNetIdentity tni) {
			return _observingNetObjects.Contains(tni);
		}

		/// <summary>
		/// Always call this to spawn an object to a client, or you will have sync issues.
		/// </summary>
		/// <param name="tni">The <see cref="TinyNetIdentity"/> of the object to spawn.</param>
		public void ShowObjectToConnection(TinyNetIdentity tni) {
			if (_observingNetObjects.Contains(tni)) {
				if (TinyNetLogLevel.logDev) { TinyLogger.Log("ShowObjectToConnection() called but object with networkdID: " + tni.TinyInstanceID + " is already shown"); }
				return;
			}

			_observingNetObjects.Add(tni);

			// spawn tiny for this conn
			TinyNetServer.instance.ShowForConnection(tni, this);
		}

		/// <summary>
		/// Always call this to hide an object from a client, or you will have sync issues.
		/// </summary>
		/// <param name="tni">The <see cref="TinyNetIdentity"/> of the object to hide.</param>
		public void HideObjectToConnection(TinyNetIdentity tni, bool isDestroyed) {
			if (!_observingNetObjects.Contains(tni)) {
				if (TinyNetLogLevel.logDev) { TinyLogger.LogWarning("RemoveFromVisList() called but object with networkdID: " + tni.TinyInstanceID + " is not shown"); }
				return;
			}

			_observingNetObjects.Remove(tni);

			if (!isDestroyed) {
				// hide tni for this conn
				TinyNetServer.instance.HideForConnection(tni, this);
			}
		}

		/// <summary>
		/// Adds an object to the list of owned objects.
		/// </summary>
		/// <param name="obj">The <see cref="TinyNetIdentity"/> of the object to own.</param>
		public void AddOwnedObject(TinyNetIdentity obj) {
			if (_ownedObjectsId == null) {
				_ownedObjectsId = new HashSet<int>();
			}

			_ownedObjectsId.Add(obj.TinyInstanceID.NetworkID);
		}

		/// <summary>
		/// Removes the owned object from the list.
		/// </summary>
		/// <param name="obj">The <see cref="TinyNetIdentity"/> of the object to remove.</param>
		public void RemoveOwnedObject(TinyNetIdentity obj) {
			if (_ownedObjectsId == null) {
				return;
			}

			_ownedObjectsId.Remove(obj.TinyInstanceID.NetworkID);
		}

		//============ Player Controllers ===================//

		/// <summary>
		/// Called when a disconnect event happens.
		/// </summary>
		public void OnDisconnect() {
			for (int i = 0; i < _playerControllers.Count; i++) {
				_playerControllers[i].OnDisconnect();
			}
		}

		/// <summary>
		/// Calls Update on all controllers.
		/// <para>This is called every frame, like an Unity Update call.</para>
		/// </summary>
		public void CallUpdateOnControllers() {
			for (int i = 0; i < _playerControllers.Count; i++) {
				_playerControllers[i].Update();
			}
		}

		/// <summary>
		/// Adds a <see cref="TinyNetPlayerController"/> to the list of player controllers of this connection.
		/// </summary>
		/// <typeparam name="T">A type derived from <see cref="TinyNetPlayerController"/>.</typeparam>
		/// <param name="player">The player controller to add.</param>
		public void SetPlayerController<T>(TinyNetPlayerController player) where T : TinyNetPlayerController, new() {
			/*while (player.playerControllerId >= _playerControllers.Count) {
				_playerControllers.Add(new T());
			}

			_playerControllers[player.playerControllerId] = player;*/
			_playerControllers.Add(player);
		}

		/// <summary>
		/// Removes the player controller from this connection.
		/// </summary>
		/// <param name="playerControllerId">The player controller identifier.</param>
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

		/// <summary>
		/// Returns a <see cref="TinyNetPlayerController"/>, given an identifier.
		/// </summary>
		/// <param name="playerControllerId">The player controller identifier.</param>
		/// <returns></returns>
		public TinyNetPlayerController GetPlayerController(short playerControllerId) {
			for (int i = 0; i < _playerControllers.Count; i++) {
				if (_playerControllers[i].IsValid && _playerControllers[i].playerControllerId == playerControllerId) {
					return _playerControllers[i];
				}
			}

			return null;
		}

		/// <summary>
		/// Outs a player controller, given an identifier. Returns true if one was found.
		/// </summary>
		/// <param name="playerControllerId">The player controller identifier.</param>
		/// <param name="playerController">The player controller found.</param>
		/// <returns>
		///	  <c>true</c> if a player controller was found; otherwise, <c>false</c>.
		/// </returns>
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
		/// <summary>
		/// Returns a player controller cast to the type given.
		/// </summary>
		/// <typeparam name="T">A type derived from <see cref="TinyNetPlayerController"/>.</typeparam>
		/// <param name="playerControllerId">The player controller identifier.</param>
		/// <returns>A player controller cast to T.</returns>
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

		/// <summary>
		/// Gets the first player controller.
		/// <para>Useful if your game only have one player per connection.</para>
		/// </summary>
		/// <returns></returns>
		public TinyNetPlayerController GetFirstPlayerController() {
			return _playerControllers[0];
		}

		/// <summary>
		/// Redirects an <see cref="TinyNetInputMessage"/> to the correct player controller.
		/// </summary>
		/// <param name="netMsg">The <see cref="TinyNetInputMessage"/>.</param>
		public void GetPlayerInputMessage(TinyNetMessageReader netMsg) {
			TinyNetPlayerController netPlayerController = GetPlayerController(TinyNetInputMessage.PeekAtPlayerControllerId(netMsg));
			if (netPlayerController != null) {
				netPlayerController.GetInputMessage(netMsg);
			}
		}
	}
}
