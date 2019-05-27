using UnityEngine;
using System.Collections;

namespace TinyBirdNet {

	public class TinyNetworkID {

		protected int _networkID = -1;

		/// <summary>
		/// The network identifier of the TinyNetIdentity
		/// </summary>
		public int NetworkID {
			get {
				return _networkID;
			}

			protected set {
				_networkID = value;
			}
		}
		
		protected byte _componentID;

		/// <summary>
		/// The component identifier.
		/// <para>0 = TinyNetIdentity, 1 for the first component, 2 second and so on.</para>
		/// </summary>
		public byte ComponentID {
			get {
				return _componentID;
			}

			set {
				_componentID = value;
			}
		}

		public TinyNetworkID(int networkID, byte componentID) {
			NetworkID = networkID;
			ComponentID = componentID;
		}

		public TinyNetworkID (int networkID) {
			NetworkID = networkID;
			ComponentID = 0;
		}

		public bool IsNotInitialized() {
			return NetworkID == -1;
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
