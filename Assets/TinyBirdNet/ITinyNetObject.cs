using LiteNetLib.Utils;

namespace TinyBirdNet {

	/// <summary>
	/// Implements basic functionality to allow network syncing.
	/// </summary>
	public interface ITinyNetObject : ITinyNetInstanceID {

		bool isServer { get; }
		bool isClient { get; }

		void TinySerialize(NetDataWriter writer, bool firstStateUpdate);
		void TinyDeserialize(NetDataReader reader, bool firstStateUpdate);

		/*/// <summary>
		/// Called after all Updates but before any LateUpdate.
		/// </summary>
		void TinyNetUpdate();*/

		bool IsTimeToUpdate();

		/// <summary>
		/// Always called, regardless of being a client or server.
		/// </summary>
		void OnNetworkCreate();

		void OnNetworkDestroy();

		void OnStartServer();

		void OnStartClient();

		void OnStartLocalPlayer();

		void OnStartAuthority();

		void OnStopAuthority();

		int GetNetworkChannel();

		float GetNetworkSendInterval();
	}
}
