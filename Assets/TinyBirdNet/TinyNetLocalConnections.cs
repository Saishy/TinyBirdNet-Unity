using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TinyBirdNet {

	/// <summary>
	/// Represents a connection to a client in the same instance as the server (Listen Server).
	/// </summary>
	/// <seealso cref="TinyBirdNet.TinyNetConnection" />
	class TinyNetLocalConnectionToClient : TinyNetConnection {

		public TinyNetLocalConnectionToClient(NetPeer newPeer) : base(newPeer) {
			
		}
	}

	/// <summary>
	/// Represents a connection to a aserver in the same instance as the client (Listen Server).
	/// </summary>
	/// <seealso cref="TinyBirdNet.TinyNetConnection" />
	class TinyNetLocalConnectionToServer : TinyNetConnection {

		public TinyNetLocalConnectionToServer(NetPeer newPeer) : base(newPeer) {

		}
	}
}
