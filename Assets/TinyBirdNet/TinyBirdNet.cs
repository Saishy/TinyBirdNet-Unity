using LiteNetLib.Utils;
using System;
using UnityEngine;

namespace TinyBirdNet {
	// Handles requests to spawn objects on the client
	public delegate GameObject SpawnDelegate(Vector3 position, int assetIndex);

	// Handles requests to unspawn objects on the client
	public delegate void UnSpawnDelegate(GameObject gObj);

	// Handles RPC calls
	public delegate void RPCDelegate(NetDataReader reader);

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
