using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using TinyBirdNet.Messaging;
using UnityEngine;

namespace TinyBirdNet {

	// This class represents the player entity in a network game, there can be multiple players per client.
	// when there are multiple people playing on one machine.
	// The server has one TinyNetConnection per peer.
	public class TinyNetPlayerController {

		protected static NetDataWriter inputWriter = new NetDataWriter();

		public short playerControllerId = -1;

		///<summary>
		/// Holds a reference to the client connection on the server, and to the server connection on the client.
		///<para>
		/// In a Listen Server this will only hold a reference to the client connection.
		///</para>
		///</summary>
		protected TinyNetConnection conn;

		public virtual TinyNetConnection Conn { get { return conn; } set { conn = value; } }

		public TinyNetPlayerController() {
		}

		public bool IsValid { get { return playerControllerId != -1; } }

		/*public TinyNetPlayerController(GameObject go, short playerControllerId) {
			gameObject = go;
			tinyNetId = go.GetComponent<TinyNetIdentity>();

			this.playerControllerId = playerControllerId;
		}*/

		public TinyNetPlayerController(short playerControllerId, TinyNetConnection nConn) : this() {
			this.playerControllerId = playerControllerId;
			this.conn = nConn;
		}

		public virtual void GetInputMessage(TinyNetMessageReader netMsg) {
		}

		public override string ToString() {
			//return string.Format("ID={0} NetworkIdentity NetID={1} Player={2}", new object[] { playerControllerId, (tinyNetId != null ? tinyNetId.NetworkID.ToString() : "null"), (gameObject != null ? gameObject.name : "null") });
			return string.Format("PlayerID={" + playerControllerId + "}");
		}
	}
}
