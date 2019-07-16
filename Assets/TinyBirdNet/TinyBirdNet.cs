using LiteNetLib.Utils;
using System;
using TinyBirdNet.Utils;
using UnityEngine;

namespace TinyBirdNet {

	/// <summary>
	/// Handles requests to spawn objects on the client
	/// </summary>
	/// <param name="position">The position.</param>
	/// <param name="assetIndex">Index of the asset.</param>
	/// <returns></returns>
	public delegate GameObject SpawnDelegate(Vector3 position, int assetIndex);

	/// <summary>
	/// Handles requests to unspawn objects on the client
	/// </summary>
	/// <param name="gObj">The GameObject.</param>
	public delegate void UnSpawnDelegate(GameObject gObj);

	/// <summary>
	/// Handles RPC calls
	/// </summary>
	/// <param name="reader">The reader.</param>
	public delegate void RPCDelegate(TinyNetStateReader reader);

	/// <summary>
	/// A data storage class for RPC methods information.
	/// </summary>
	public class RPCMethodInfo {
		public RPCTarget target { private set; get; }
		public RPCCallers caller { private set; get; }
		public string name { private set; get; }

		public RPCMethodInfo(string rpcName, RPCTarget nTarget, RPCCallers nCaller) {
			name = rpcName;
			target = nTarget;
			caller = nCaller;
		}
	}
}
