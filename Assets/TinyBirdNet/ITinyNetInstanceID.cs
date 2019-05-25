using UnityEngine;
using System.Collections;

namespace TinyBirdNet {

	public struct TinyNetworkID {
		/// <summary>
		/// The network identifier of the TinyNetIdentity
		/// </summary>
		public int NetworkID;
		/// <summary>
		/// The component identifier.
		/// <para>0 = TinyNetIdentity, 1 for the first component, 2 second and so on.</para>
		/// </summary>
		public byte ComponentID;

		public TinyNetworkID(int networkID, byte componentID) {
			NetworkID = networkID;
			ComponentID = componentID;
		}

		public TinyNetworkID (int networkID) {
			NetworkID = networkID;
			ComponentID = 0;
		}

		public bool IsNotInitialized() {
			return NetworkID == 0;
		}

		public override string ToString() {
			return string.Format("{0}.{1}", NetworkID, ComponentID);
		}
	}

	/// <summary>
	/// Implement this interface to allow your custom class to receive a NetworkID.
	/// </summary>
	public interface ITinyNetInstanceID {

		/// <summary>
		/// The ID of an instance in the network.
		/// </summary>
		TinyNetworkID TinyInstanceID { get; }

		/// <summary>
		/// Receives the network identifier.
		/// </summary>
		/// <param name="newID">The new identifier.</param>
		void ReceiveNetworkID(TinyNetworkID newID);
	}
}
