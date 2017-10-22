using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TinyBirdNet {

	public class TinyNetBehaviour : MonoBehaviour {

		private Dictionary<string, TinyNetPropertyAccessor<byte>> byteAccessor;
		private Dictionary<string, TinyNetPropertyAccessor<sbyte>> sbyteAccessor;
		private Dictionary<string, TinyNetPropertyAccessor<short>> shortAccessor;
		private Dictionary<string, TinyNetPropertyAccessor<ushort>> ushortAccessor;
		private Dictionary<string, TinyNetPropertyAccessor<int>> intAccessor;
		private Dictionary<string, TinyNetPropertyAccessor<uint>> uintAccessor;
		private Dictionary<string, TinyNetPropertyAccessor<long>> longAccessor;
		private Dictionary<string, TinyNetPropertyAccessor<ulong>> ulongAccessor;
		private Dictionary<string, TinyNetPropertyAccessor<float>> floatAccessor;
		private Dictionary<string, TinyNetPropertyAccessor<double>> doubleAccessor;
		private Dictionary<string, TinyNetPropertyAccessor<bool>> boolAccessor;
		private Dictionary<string, TinyNetPropertyAccessor<string>> stringAccessor;
	}
}