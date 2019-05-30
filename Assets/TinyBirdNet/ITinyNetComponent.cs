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
		void TinyDeserialize(TinyNetStateReader reader, bool firstStateUpdate);

		/// <summary>
		/// Called on Server after all FixedUpdates and physics but before any Update.
		/// </summary>
		void TinyNetUpdate();
	}
}
