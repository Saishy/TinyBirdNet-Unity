using UnityEngine;
using System.Collections;

namespace TinyBirdNet {

	/// <summary>
	/// Implement this interface to allow your custom class to receive a NetworkID.
	/// </summary>
	public interface ITinyNetInstanceID {

		/// <summary>
		/// The ID of an instance in the network, given by the server on spawn.
		/// </summary>
		int NetworkID { get; }

		void ReceiveNetworkID(int newID);
	}
}