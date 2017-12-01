using System;
using Mono.Cecil;
using UnityEngine;
using Mono.Cecil.Cil;
using TinyBirdNet;

namespace Weaver {
	// Inherit WeaverComponent to get callbacks and to show up as a componet in our ScriptableObject settings. 
	public class TinyNetRPCComponent : WeaverComponent {
		// Used for logging the name really does not matter. 
		public override string addinName {
			get {
				return "TinyNetRPC Method";
			}
		}

		// Defines which type of callbacks you want. Used as an optimization 
		public override DefinitionType effectedDefintions {
			get {
				return DefinitionType.Method;
			}
		}

		// Invoked for every method in each assembly that is listed in WeavedAssemblies in settings. 
		public override void VisitMethod(MethodDefinition methodDefinition) {
			CustomAttribute serverCommandAttribute = methodDefinition.GetCustomAttribute<TinyNetRPC>();

			if (serverCommandAttribute == null) {
				// Our method does not have the attribute so we skip it. 
				return;
			}

			// Do the IL Injection. 
			//Debug.Log(methodDefinition.Name);
		}
	}
}