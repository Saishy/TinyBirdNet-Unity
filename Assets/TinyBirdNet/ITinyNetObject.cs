using LiteNetLib.Utils;

namespace TinyBirdNet {

	/// <summary>
	/// Implements basic functionality to allow network syncing.
	/// </summary>
	public interface ITinyNetObject : ITinyNetInstanceID {

		/// <summary>
		/// Gets a value indicating whether this instance is server.
		/// </summary>
		/// <value>
		///   <c>true</c> if this instance is server; otherwise, <c>false</c>.
		/// </value>
		bool isServer { get; }
		/// <summary>
		/// Gets a value indicating whether this instance is client.
		/// </summary>
		/// <value>
		///   <c>true</c> if this instance is client; otherwise, <c>false</c>.
		/// </value>
		bool isClient { get; }

		// Saishy: You don't need to use a netidentity if you don't want and have implemmented your own way of spawning netobjects
		/// <summary>
		/// Gets the net identity.
		/// </summary>
		/// <value>
		/// The net identity.
		/// </value>
		TinyNetIdentity NetIdentity { get; }

		/// <summary>
		/// Serializates the data.
		/// </summary>
		/// <param name="writer">The writer.</param>
		/// <param name="firstStateUpdate">if set to <c>true</c> it's the first state update.</param>
		void TinySerialize(NetDataWriter writer, bool firstStateUpdate);
		/// <summary>
		/// Deserializations the data received.
		/// </summary>
		/// <param name="reader">The reader.</param>
		/// <param name="firstStateUpdate">if set to <c>true</c> it's the first state update.</param>
		void TinyDeserialize(NetDataReader reader, bool firstStateUpdate);

		/// <summary>
		/// Sends the RPC.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <param name="rpcName">Name of the RPC.</param>
		void SendRPC(NetDataWriter stream, string rpcName);
		/// <summary>
		/// Invokes the RPC.
		/// </summary>
		/// <param name="rpcMethodIndex">Index of the RPC method.</param>
		/// <param name="reader">The reader.</param>
		/// <returns></returns>
		bool InvokeRPC(int rpcMethodIndex, NetDataReader reader);

		/// <summary>
		/// Called after all FixedUpdates and physics but before any Update.
		/// <para>It is used by TinyNetServer to check if it is time to send the current state to clients.</para>
		/// </summary>
		void TinyNetUpdate();

		// <summary>
		// Used to check if 
		// </summary>
		// <returns></returns>
		//bool IsTimeToUpdate();

		/// <summary>
		/// Always called, regardless of being a client or server. Called before variables are synced. (Order: 0)
		/// </summary>
		void OnNetworkCreate();

		/// <summary>
		/// Called when the object receives an order to be destroyed from the network,
		/// in a listen server the object could just be unspawned without being actually destroyed.
		/// </summary>
		void OnNetworkDestroy();

		/// <summary>
		/// Called on the server when Spawn is called for this object. (Order: 1)
		/// </summary>
		void OnStartServer();

		/// <summary>
		/// Called on the client when the object is spawned. Called after variables are synced. (Order: 2)
		/// </summary>
		void OnStartClient();

		/// <summary>
		/// Not implemented yet.
		/// </summary>
		void OnStartLocalPlayer();

		/// <summary>
		/// Called on the client that receives authority of this object.
		/// </summary>
		void OnStartAuthority();

		/// <summary>
		/// Called on the client that loses authorithy of this object.
		/// </summary>
		void OnStopAuthority();

		/// <summary>
		/// Called on the server when giving authority of this object to a client.
		/// </summary>
		void OnGiveAuthority();

		/// <summary>
		/// Called on the server when removing authority of a client to this object.
		/// </summary>
		void OnRemoveAuthority();

		/// <summary>
		/// This is only called on a listen server, for spawn and hide messages. Objects being destroyed will trigger OnNetworkDestroy as normal.
		/// </summary>
		/// <param name="vis"></param>
		void OnSetLocalVisibility(bool vis);

		LiteNetLib.DeliveryMethod GetNetworkChannel();

		// <summary>
		// Sets how frequently a state update is checked and sent. (1f = One time per second)
		// </summary>
		// <returns></returns>
		//float GetNetworkSendInterval();
	}
}
