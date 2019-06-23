using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using TinyBirdUtils;
using System.Text.RegularExpressions;

namespace TinyBirdNet {

	/// <summary>
	/// This class stores all SyncVar allowed properties and is used to sync the game state.
	/// </summary>
	public abstract class TinyNetStateSyncer {

		protected static Dictionary<Type, List<PropertyInfo>> syncVarProps = new Dictionary<Type, List<PropertyInfo>>();
		protected static Dictionary<Type, List<RPCMethodInfo>> rpcMethods = new Dictionary<Type, List<RPCMethodInfo>>(); //Maybe we have to change this

		protected static int[] tempIntArray = new int[1];

		public static void InitializePropertyInfoListOfType(int size, Type type) {
			syncVarProps.Add(type, new List<PropertyInfo>(size));
		}

		public static void InitializeRPCMethodsOfType(int size, Type type) {
			rpcMethods.Add(type, new List<RPCMethodInfo>(size));
		}

		public static void AddPropertyToType(PropertyInfo prop, Type type) {
			MethodInfo getMethod = prop.GetGetMethod(true);
			MethodInfo setMethod = prop.GetSetMethod(true);

			if (getMethod != null && setMethod != null) {
				syncVarProps[type].Add(prop);
			} else {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("TinyNetSyncVar used on property without get and/or set: " + prop.Name); }
				return;
			}
		}

		public static void AddRPCMethodNameToType(string rpcName, RPCTarget nTarget, RPCCallers nCaller, Type type) {
			rpcMethods[type].Add(new RPCMethodInfo(rpcName, nTarget, nCaller));
		}

		public static PropertyInfo GetPropertyInfoFromType(Type type, string propName) {
			for (int i = 0; i < syncVarProps[type].Count; i++) {
				if (syncVarProps[type][i].Name == propName) {
					return syncVarProps[type][i];
				}
			}

			return null;
		}

		public static void OutPropertyNamesFromType(Type type, out string[] propNames) {
			propNames = new string[syncVarProps[type].Count];

			for (int i = 0; i < propNames.Length; i++) {
				propNames[i] = syncVarProps[type][i].Name;
			}
		}

		public static void OutRPCMethodNamesFromType(Type type, out string[] rpcNames) {
			rpcNames = new string[rpcMethods[type].Count];

			for (int i = 0; i < rpcNames.Length; i++) {
				rpcNames[i] = rpcMethods[type][i].name;
			}
		}

		public static void OutPropertyTypesFromType(Type type, out Type[] propTypes) {
			propTypes = new Type[syncVarProps[type].Count];

			for (int i = 0; i < propTypes.Length; i++) {
				propTypes[i] = syncVarProps[type][i].PropertyType;
			}
		}

		public static int GetRPCMethodIndexFromType(Type type, string rpcName) {
			for (int i = 0; i < rpcMethods[type].Count; i++) {
				if (rpcMethods[type][i].name == rpcName) {
					return i;
				}
			}

			throw new Exception("GetRPCMethodIndexFromType called but " + type + " does not contain a method called " + rpcName);

			//return -1;
		}

		public static int GetRPCMethodInfoFromType(Type type, string rpcName, ref RPCMethodInfo rpcMethodInfo) {
			for (int i = 0; i < rpcMethods[type].Count; i++) {
				if (rpcMethods[type][i].name == rpcName) {
					rpcMethodInfo = rpcMethods[type][i];
					return i;
				}
			}

			return -1;
		}

		public static void GetRPCMethodInfoFromType(Type type, int rpcMethodIndex, ref RPCMethodInfo rpcMethodInfo) {
			rpcMethodInfo = rpcMethods[type][rpcMethodIndex];
		}

		public static bool UpdateDirtyFlagOf(TinyNetBehaviour instance, BitArray bitArray) {
			Type type = instance.GetType();
			bool localReturn = false;

			for (int i = 0; i < syncVarProps[type].Count; i++) {
				if (instance.CheckIfPropertyUpdated(syncVarProps[type][i].Name, syncVarProps[type][i].PropertyType)) {
					bitArray[i] = true;
					localReturn = true;
				} else {
					bitArray[i] = false;
				}
			}

			return localReturn;
		}

		/*public static int DirtyFlagToInt(BitArray bitArray) {
			if (bitArray.Length > 32) {
				throw new ArgumentException("Argument length shall be at most 32 bits.");
			}

			int output = 0;

			for (int i = 0; i < 32; i++) {
				if (bitArray[i]) { //check the first bit 
					output |= 1 << i;
				} else {
					output |= 0 << i;
				}
			}

			//bitArray.CopyTo(tempIntArray, 0);
			//return tempIntArray[0];

			return output;
		}

		public static void IntToDirtyFlag(int input, BitArray bitArray) {
			//bitArray = new BitArray(new[] { input });
			for (int i = 0; i < 32; i++) {
				if ((input & 1) == 1) { //check the first bit 
					bitArray[i] = true;
				} else {
					bitArray[i] = false;
				}
				input >>= 1; // move all bits right 1 so the next first bit from 110 becomes 11
			}
		}*/

		/*public static int DirtyFlagToInt(BitArray bitArray) {
			int value = 0;

			for (var i = 0; i < bitArray.Count; i++) {
				value <<= 1;
				if (bitArray[i]) {
					value |= 1;
				}
			}

			//if (TinyNetLogLevel.logDev) { TinyLogger.Log("binary dirtyflag: " + Convert.ToString(value,2)); }

			return value;
		}

		public static void IntToDirtyFlag(int input, BitArray bitArray) {
			for (var i = 0; i < bitArray.Count; i++) {
				if ((input & 1) == 1) { //current right most bit is a 1 [true]
					bitArray[i] = true;
				} else {
					bitArray[i] = false;
				}

				input >>= 1;
			}

			//if (TinyNetLogLevel.logDev) { TinyLogger.Log("bitArray: " + bitArray); }
		}*/

		public static int GetNumberOfSyncedProperties(Type type) {
			return syncVarProps[type].Count;
		}

		public static int GetNumberOfRPCMethods(Type type) {
			return rpcMethods[type].Count;
		}
	}
}
