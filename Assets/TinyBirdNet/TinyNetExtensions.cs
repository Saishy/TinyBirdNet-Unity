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

        public static void Put(this NetDataWriter writer, Vector2 vector) {
            writer.Put(vector.x);
            writer.Put(vector.y);
        }

        public static Vector2 GetVector2(this NetDataReader reader) {
            return new Vector2(reader.GetFloat(), reader.GetFloat());
        }

    }
}