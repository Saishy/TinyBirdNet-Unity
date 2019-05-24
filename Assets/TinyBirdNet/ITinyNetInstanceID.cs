using UnityEngine;
using System.Collections;

namespace TinyBirdNet {

	/// <summary>
	/// Implement this interface to allow your custom class to receive a NetworkID.
	/// </summary>
	public interface ITinyNetInstanceID {

		/// <summary>
		/// The ID of an instance in the network.
		/// </summary>
		int NetworkID { get; }

		/// <summary>
		/// Receives the network identifier.
		/// </summary>
		/// <param name="newID">The new identifier.</param>
		void ReceiveNetworkID(int newID);
	}
}
