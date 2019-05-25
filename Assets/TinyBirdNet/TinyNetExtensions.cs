using LiteNetLib.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TinyBirdNet {

	public static class TinyNetExtensions {

		public static void Put (this NetDataWriter writer, TinyNetworkID networkID) {
			writer.Put(networkID.NetworkID);
			writer.Put(networkID.ComponentID);
		}
	}
}