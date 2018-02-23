using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using TinyBirdNet.Messaging;
using UnityEngine;

namespace TinyBirdNet {

	// This class represents the player entity in a network game, there can be multiple players per client.
	// when there are multiple people playing on one machine.
	// The server has one TinyNetConnection per peer.
	/// <summary>
	/// This class represents the player entity in a network game, there can be multiple players per client,
	/// when there are multiple people playing on one machine.
	/// <para>The server has one <see cref="TinyNetConnection"/> per <see cref="LiteNetLib.NetPeer"/>.</para>
	/// </summary>
	public class TinyNetPlayerController {

		/// <summary>
		/// A stream used for input.
		/// </summary>
		protected static NetDataWriter inputWriter = new NetDataWriter();

		/// <summary>
		/// The player controller identifier
		/// </summary>
		public short playerControllerId = -1;

		/// <summary>
		/// Holds a reference to the client connection on the server, and to the server connection on the client.
		/// <para>In a Listen Server this will only hold a reference to the client connection.</para>
		///</summary>
		protected TinyNetConnection conn;

		/// <summary>
		/// Gets or sets the connection.
		/// </summary>
		/// <value>
		/// The connection.
		/// </value>
		public virtual TinyNetConnection Conn { get { return conn; } set { conn = value; } }

		/// <summary>
		/// Initializes a new instance of the <see cref="TinyNetPlayerController"/> class.
		/// </summary>
		public TinyNetPlayerController() {
		}

		/// <summary>
		/// Returns true if this instance is valid.
		/// </summary>
		/// <value>
		///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
		/// </value>
		public bool IsValid { get { return playerControllerId != -1; } }

		/*public TinyNetPlayerController(GameObject go, short playerControllerId) {
			gameObject = go;
			tinyNetId = go.GetComponent<TinyNetIdentity>();

			this.playerControllerId = playerControllerId;
		}*/

		/// <summary>
		/// Initializes a new instance of the <see cref="TinyNetPlayerController"/> class.
		/// </summary>
		/// <param name="playerControllerId">The player controller identifier.</param>
		/// <param name="nConn">The <see cref="TinyNetConnection"/>.</param>
		public TinyNetPlayerController(short playerControllerId, TinyNetConnection nConn) : this() {
			this.playerControllerId = playerControllerId;
			this.conn = nConn;
		}

		/// <summary>
		/// Receives an input message
		/// </summary>
		/// <param name="netMsg">The message reader.</param>
		public virtual void GetInputMessage(TinyNetMessageReader netMsg) {
		}

		/// <summary>
		/// Returns a <see cref="System.String" /> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String" /> that represents this instance.
		/// </returns>
		public override string ToString() {
			//return string.Format("ID={0} NetworkIdentity NetID={1} Player={2}", new object[] { playerControllerId, (tinyNetId != null ? tinyNetId.NetworkID.ToString() : "null"), (gameObject != null ? gameObject.name : "null") });
			return string.Format("PlayerID={" + playerControllerId + "}");
		}
	}
}
