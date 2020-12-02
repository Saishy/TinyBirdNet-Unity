using LiteNetLib;
using LiteNetLib.Utils;
using TinyBirdNet.Utils;

namespace TinyBirdNet {

	/// <summary>
	/// Implements basic functionality to allow network syncing.
	/// </summary>
	public interface ITinyNetComponent : ITinyNetInstanceID {

		/// <summary>
		/// Gets a value indicating whether this instance is dirty.
		/// <para> It is used by TinyNetIdentity to check if we should call TinySerialize. </para>
		/// </summary>
		/// <value>
		///   <c>true</c> if instance is dirty; otherwise, <c>false</c>.
		/// </value>
		bool IsDirty { get; }

		/// <summary>
		/// This is added to the PriorityAccumulator every frame, the N objects with the highest priorities are sent through the network, then it reset to default.
		/// </summary>
		float ImmediatePriority { get; }

		/// <summary>
		/// Serializates the data to send.
		/// </summary>
		/// <param name="writer">The writer.</param>
		/// <param name="firstTimeUpdate">if set to <c>true</c> this is the first time this object has been serialized to a connection.</param>
		void TinySerialize(NetDataWriter writer, bool firstTimeUpdate);
		
		/// <summary>
		/// Deserializates the data received.
		/// </summary>
		/// <param name="reader">The reader.</param>
		/// <param name="fullDataUpdate">if set to <c>true</c> should contain every data possible.</param>
		void TinyDeserialize(TinyNetStateReader reader, bool fullDataUpdate);

		/// <summary>
		/// [Server Only] Called every server network tick that a state update is gonna be sent, after all FixedUpdates and before sending state updates.
		/// </summary>
		void TinyNetUpdate();
	}
}
