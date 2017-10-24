using UnityEngine;

namespace TinyBirdNet {

	// This class represents the player entity in a network game, there can be multiple players per client.
	// when there are multiple people playing on one machine.
	// The server has one NetPeer per player.
	public class TinyPlayerController {
		internal const short kMaxLocalPlayers = 8;

		public short playerControllerId = -1;
		public TinyNetIdentity tinyNetId;
		public GameObject gameObject;

		public const int MaxPlayersPerClient = 32;

		public TinyPlayerController() {
		}

		public bool IsValid { get { return playerControllerId != -1; } }

		public TinyPlayerController(GameObject go, short playerControllerId) {
			gameObject = go;
			tinyNetId = go.GetComponent<TinyNetIdentity>();
			this.playerControllerId = playerControllerId;
		}

		public override string ToString() {
			return string.Format("ID={0} NetworkIdentity NetID={1} Player={2}", new object[] { playerControllerId, (tinyNetId != null ? tinyNetId.NetworkID.ToString() : "null"), (gameObject != null ? gameObject.name : "null") });
		}
	}
}
