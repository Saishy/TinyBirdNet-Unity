using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TinyBirdNet.Messaging;

namespace TinyBirdNet {

	/// <summary>
	/// Represents a connection to the same computer it originates from.
	/// </summary>
	/// <seealso cref="TinyBirdNet.TinyNetLocalConnection" />
	public class TinyNetLocalConnection : TinyNetConnection {

		protected static NetDataReader s_recycleReader = new NetDataReader();

		private long _connectID;

		public override long ConnectId {
			get { return _connectID; }
		}

		/// <inheritdoc />
		public TinyNetLocalConnection(NetPeer newPeer) : base(newPeer) {
			_connectID = DateTime.UtcNow.Ticks;
		}

		/// <inheritdoc />
		public override void Send(byte[] data, DeliveryMethod options) {
		}

		/// <inheritdoc />
		public override void Send(NetDataWriter dataWriter, DeliveryMethod options) {
		}

		/// <inheritdoc />
		public override void Send(ITinyNetMessage msg, DeliveryMethod options) {
		}

		/// <inheritdoc />
		public override string ToString() {
			return string.Format("{0} EndPoint: {0} ConnectId: {1} isReady: {2}", GetType(), "localhost", ConnectId, isReady);
		}
	}

	/// <summary>
	/// Represents a connection to a client in the same instance as the server (Listen Server).
	/// </summary>
	/// <seealso cref="TinyBirdNet.TinyNetLocalConnection" />
	public class TinyNetLocalConnectionToClient : TinyNetLocalConnection {

		/// <inheritdoc />
		public TinyNetLocalConnectionToClient(NetPeer newPeer) : base(newPeer) {
			
		}

		/// <inheritdoc />
		public override void Send(byte[] data, DeliveryMethod options) {
			s_recycleReader.Clear();
			s_recycleReader.SetSource(data);

			TinyNetClient.instance.ReadMessageLocalConnection(s_recycleReader);
		}

		/// <inheritdoc />
		public override void Send(NetDataWriter dataWriter, DeliveryMethod options) {
			s_recycleReader.Clear();
			s_recycleReader.SetSource(dataWriter);

			TinyNetClient.instance.ReadMessageLocalConnection(s_recycleReader);
		}

		/// <inheritdoc />
		public override void Send(ITinyNetMessage msg, DeliveryMethod options) {
			recycleWriter.Reset();

			recycleWriter.Put(msg.msgType);
			msg.Serialize(recycleWriter);

			s_recycleReader.Clear();
			s_recycleReader.SetSource(recycleWriter);

			TinyNetClient.instance.ReadMessageLocalConnection(s_recycleReader);
		}
	}

	/// <summary>
	/// Represents a connection to a a server in the same instance as the client (Listen Server).
	/// </summary>
	/// <seealso cref="TinyBirdNet.TinyNetLocalConnection" />
	public class TinyNetLocalConnectionToServer : TinyNetLocalConnection {

		/// <inheritdoc />
		public TinyNetLocalConnectionToServer(NetPeer newPeer) : base(newPeer) {

		}

		/// <inheritdoc />
		public override void Send(byte[] data, DeliveryMethod options) {
			s_recycleReader.Clear();
			s_recycleReader.SetSource(data);

			TinyNetServer.instance.ReadMessageLocalConnection(s_recycleReader);
		}

		/// <inheritdoc />
		public override void Send(NetDataWriter dataWriter, DeliveryMethod options) {
			s_recycleReader.Clear();
			s_recycleReader.SetSource(dataWriter);

			TinyNetServer.instance.ReadMessageLocalConnection(s_recycleReader);
		}

		/// <inheritdoc />
		public override void Send(ITinyNetMessage msg, DeliveryMethod options) {
			recycleWriter.Reset();

			recycleWriter.Put(msg.msgType);
			msg.Serialize(recycleWriter);

			s_recycleReader.Clear();
			s_recycleReader.SetSource(recycleWriter);

			TinyNetServer.instance.ReadMessageLocalConnection(s_recycleReader);
		}
	}
}
