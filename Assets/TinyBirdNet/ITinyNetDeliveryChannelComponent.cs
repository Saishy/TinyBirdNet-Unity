using UnityEngine;
using System.Collections;
using LiteNetLib;

namespace TinyBirdNet {

	/// <summary>
	/// Implement this interface to choose what DeliveryMethod and Channel you want for your interface.
	/// </summary>
	public interface ITinyNetDeliveryChannelComponent : ITinyNetInstanceID {

		/// <summary>
		/// The delivery channel used for sending the packet
		/// </summary>
		SerializationMethod serializationMethod { get; }
	}
}