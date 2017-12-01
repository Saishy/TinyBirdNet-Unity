using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TinyBirdUtils;
using UnityEngine;

namespace TinyBirdNet {

	/// <summary>
	/// This class is used to get all properties marked as SyncVar.
	/// </summary>
	public static class TinyNetReflector {

		//static Dictionary<Type, PropertyInfo[]> syncVarProps = new Dictionary<Type, PropertyInfo[]>();

		//public static Dictionary<Type, PropertyInfo> SyncVarProps { get { return syncVarProps; } }

		public static List<Type> GetAllClassesAndChildsOf<T>() where T : class {
			List<Type> types = new List<Type>();

			foreach (Type type in
				Assembly.GetAssembly(typeof(T)).GetTypes()
				.Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T)))) {
				types.Add(type);
			}

			return types;
		}

		public static void GetAllSyncVarProps() {
			List<Type> types = GetAllClassesAndChildsOf<TinyNetBehaviour>();

			foreach (Type type in types) {
				PropertyInfo[] props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
					.Where(prop => Attribute.IsDefined(prop, typeof(TinyNetSyncVar)))
					.OrderBy(info => info.Name).ToArray();

				if (props.Length < 32) {

					TinyNetStateSyncer.InitializePropertyInfoListOfType(props.Length, type);

					for (int i = 0; i < props.Length; i++) {
						if (TinyNetSyncVar.allowedTypes.Contains(props[i].PropertyType)) {
							if (TinyNetLogLevel.logDev) { TinyLogger.Log(props[i].Name); }

							//MethodInfo getMethod = props[i].GetGetMethod(true);
							//MethodInfo setMethod = props[i].GetSetMethod(true);

							TinyNetStateSyncer.AddPropertyToType(props[i], type);
						} else {
							if (TinyNetLogLevel.logError) { TinyLogger.LogError("TinyNetSyncVar used in incompatible property type: " + props[i].Name); }
						}
					}
				} else {
					if (TinyNetLogLevel.logError) { TinyLogger.LogError("ERROR: " + type + " have more than 32 syncvar"); }
				}

				// Time for the RPC methods
				MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
					.Where(method => Attribute.IsDefined(method, typeof(TinyNetRPC)))
					.OrderBy(info => info.Name).ToArray();

				TinyNetStateSyncer.InitializeRPCMethodsOfType(methods.Length, type);

				ParameterInfo[] pars;
				bool bValid = true;
				TinyNetRPC rpcAttribute;

				for (int i = 0; i < methods.Length; i++) {
					pars = methods[i].GetParameters();
					rpcAttribute = (TinyNetRPC)methods[i].GetCustomAttributes(typeof(TinyNetRPC), true)[0];

					bValid = true;
					for (int x = 0; x < pars.Length; x++) {
						if (!TinyNetSyncVar.allowedTypes.Contains(pars[x].ParameterType)) {
							if (TinyNetLogLevel.logError) { TinyLogger.LogError("TinyNetRPC used with incompatible parameter: " + pars[x].Name); }
							bValid = false;
						}
					}

					if (bValid) {
						if (TinyNetLogLevel.logDev) { TinyLogger.Log(methods[i].Name); }

						TinyNetStateSyncer.AddRPCMethodNameToType(methods[i].Name, rpcAttribute.Targets, rpcAttribute.Callers, type);
					}
				}
			}
		}
	}
}
