using LiteNetLib.Utils;

namespace TinyBirdNet {

	/// <summary>
	/// Implements basic functionality to allow network syncing.
	/// </summary>
	public interface ITinyNetObject : ITinyNetInstanceID {

		bool isServer { get; }
		bool isClient { get; }

		// Saishy: You don't need to use a netidentity if you don't want and have implemmented your own way of spawning netobjects
		TinyNetIdentity NetIdentity { get; }

		void TinySerialize(NetDataWriter writer, bool firstStateUpdate);
		void TinyDeserialize(NetDataReader reader, bool firstStateUpdate);

		void SendRPC(NetDataWriter stream, string rpcName);
		bool InvokeRPC(int rpcMethodIndex, NetDataReader reader);

		/// <summary>
		/// Called after all FixedUpdates and physics but before any Update.
		/// <para>It is used by TinyNetServer to check if it is time to send the current state to clients.</para>
		/// </summary>
		void TinyNetUpdate();

		/// <summary>
		/// Used to check if 
		/// </summary>
		/// <returns></returns>
		//bool IsTimeToUpdate();

		/// <summary>
		/// Always called, regardless of being a client or server. (order 0) Called before variables are synced.
		/// </summary>
		void OnNetworkCreate();

		/// <summary>
		/// Called when the object receives an order to be destroyed from the network, in a listen server the object could just be unspawned witout being destroyed.
		/// </summary>
		void OnNetworkDestroy();

		/// <summary>
		/// Called on the server when Spawn is called for this object. (order 1)
		/// </summary>
		void OnStartServer();

		/// <summary>
		/// Called on the client when the object is spawned. (order: 2) Called after variables are synced.
		/// </summary>
		void OnStartClient();

		/// <summary>
		/// Not implemented
		/// </summary>
		void OnStartLocalPlayer();

		/// <summary>
		/// Not implemented
		/// </summary>
		void OnStartAuthority();

		/// <summary>
		/// Not implemented
		/// </summary>
		void OnStopAuthority();

		/// <summary>
		/// This is only called on a listen server, for spawn and hide messages. Objects being destroyed will trigger OnNetworkDestroy as normal.
		/// </summary>
		/// <param name="vis"></param>
		void OnSetLocalVisibility(bool vis);

		LiteNetLib.SendOptions GetNetworkChannel();

		/// <summary>
		/// Sets how frequently a state update is checked and sent. (1f = One time per second)
		/// </summary>
		/// <returns></returns>
		//float GetNetworkSendInterval();
	}
}
