using System.Collections;
using System.Collections.Generic;
using System;
using LiteNetLib;

namespace TinyBirdNet {

	public enum RPCTarget {
		Server,
		ClientOwner,
		Everyone
	}

	public enum RPCCallers {
		Server,
		ClientOwner,
		Anyone
	}

	/// <summary>
	/// When used on a method allows it to be executed remotely on another machine when called.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public class TinyNetRPC : Attribute {

		private RPCTarget targets;
		private RPCCallers callers;

		private DeliveryMethod sendOption;

		public RPCTarget Targets {
			get { return targets; }
		}

		public RPCCallers Callers {
			get { return callers; }
		}

		public DeliveryMethod SendOption {
			get { return sendOption; }
			set { sendOption = value; }
		}

		public TinyNetRPC(RPCTarget newTargets, RPCCallers newCallers) {
			targets = newTargets;
			callers = newCallers;
		}
	}
}
