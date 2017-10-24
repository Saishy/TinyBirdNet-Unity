using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TinyBirdNet {

	class TinyNetLocalConnectionToClient : TinyNetConnection {

		public TinyNetLocalConnectionToClient(NetPeer newPeer) : base(newPeer) {
			
		}
	}

	class TinyNetLocalConnectionToServer : TinyNetConnection {

		public TinyNetLocalConnectionToServer(NetPeer newPeer) : base(newPeer) {

		}
	}
}
