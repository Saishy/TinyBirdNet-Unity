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
	}
}
