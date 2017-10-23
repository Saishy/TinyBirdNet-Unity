using System;
using UnityEngine;

namespace TinyBirdNet {
	// Handles requests to spawn objects on the client
	public delegate GameObject SpawnDelegate(Vector3 position, int assetIndex);

	// Handles requests to unspawn objects on the client
	public delegate void UnSpawnDelegate(GameObject gObj);
}
