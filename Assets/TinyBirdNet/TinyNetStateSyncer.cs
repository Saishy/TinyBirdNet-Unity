using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using TinyBirdUtils;

namespace TinyBirdNet {

	/// <summary>
	/// This class stores all SyncVar allowed properties and is used to sync the game state.
	/// </summary>
	public abstract class TinyNetStateSyncer {

		protected static Dictionary<Type, List<PropertyInfo>> syncVarProps = new Dictionary<Type, List<PropertyInfo>>();

		public static void InitializePropertyInfoListOfType(int size, Type type) {
			syncVarProps.Add(type, new List<PropertyInfo>(size));
		}

		public static void AddPropertyToType(PropertyInfo prop, Type type) {
			MethodInfo getMethod = prop.GetGetMethod(true);
			MethodInfo setMethod = prop.GetSetMethod(true);

			if (getMethod != null && setMethod != null) {
				syncVarProps[type].Add(prop);
			} else {
				TinyLogger.LogError("TinyNetSyncVar used on property without get and/or set: " + prop.Name);
			}
		}
	}
}
