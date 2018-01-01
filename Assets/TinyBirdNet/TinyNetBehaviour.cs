using LiteNetLib.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TinyBirdUtils;
using UnityEngine;

namespace TinyBirdNet {

	[RequireComponent(typeof(TinyNetIdentity))]
	public class TinyNetBehaviour : MonoBehaviour, ITinyNetObject {

		protected static NetDataWriter rpcRecycleWriter = new NetDataWriter();

		public int NetworkID { get; protected set; }

		private Dictionary<string, TinyNetPropertyAccessor<byte>> byteAccessor = new Dictionary<string, TinyNetPropertyAccessor<byte>>();
		private Dictionary<string, TinyNetPropertyAccessor<sbyte>> sbyteAccessor = new Dictionary<string, TinyNetPropertyAccessor<sbyte>>();
		private Dictionary<string, TinyNetPropertyAccessor<short>> shortAccessor = new Dictionary<string, TinyNetPropertyAccessor<short>>();
		private Dictionary<string, TinyNetPropertyAccessor<ushort>> ushortAccessor = new Dictionary<string, TinyNetPropertyAccessor<ushort>>();
		private Dictionary<string, TinyNetPropertyAccessor<int>> intAccessor = new Dictionary<string, TinyNetPropertyAccessor<int>>();
		private Dictionary<string, TinyNetPropertyAccessor<uint>> uintAccessor = new Dictionary<string, TinyNetPropertyAccessor<uint>>();
		private Dictionary<string, TinyNetPropertyAccessor<long>> longAccessor = new Dictionary<string, TinyNetPropertyAccessor<long>>();
		private Dictionary<string, TinyNetPropertyAccessor<ulong>> ulongAccessor = new Dictionary<string, TinyNetPropertyAccessor<ulong>>();
		private Dictionary<string, TinyNetPropertyAccessor<float>> floatAccessor = new Dictionary<string, TinyNetPropertyAccessor<float>>();
		private Dictionary<string, TinyNetPropertyAccessor<double>> doubleAccessor = new Dictionary<string, TinyNetPropertyAccessor<double>>();
		private Dictionary<string, TinyNetPropertyAccessor<bool>> boolAccessor = new Dictionary<string, TinyNetPropertyAccessor<bool>>();
		private Dictionary<string, TinyNetPropertyAccessor<string>> stringAccessor = new Dictionary<string, TinyNetPropertyAccessor<string>>();

		private RPCDelegate[] rpcHandlers;

		private string[] propertiesName;
		private Type[] propertiesTypes;

		// Used for comparisson.
		private bool _bIsDirty = false;
		public bool bIsDirty { get { return _bIsDirty; } set { _bIsDirty = value; } }

		private BitArray _dirtyFlag = new BitArray(32);
		public BitArray DirtyFlag { get { return _dirtyFlag; } private set { _dirtyFlag = value; } }

		protected float _lastSendTime;

		public bool isServer { get { return TinyNetGameManager.instance.isServer; } }
		public bool isClient { get { return TinyNetGameManager.instance.isClient; } }
		public bool hasAuthority { get { return NetIdentity.hasAuthority; } }

		TinyNetIdentity _netIdentity;
		public TinyNetIdentity NetIdentity {
			get {
				if (_netIdentity == null) {
					_netIdentity = GetComponent<TinyNetIdentity>();

					if (_netIdentity == null) {
						TinyLogger.LogError("There is no TinyNetIdentity on this object. Please add one.");
					}

					return _netIdentity;
				}

				return _netIdentity;
			}
		}

		public void ReceiveNetworkID(int newID) {
			NetworkID = newID;
		}

		protected void RegisterRPCDelegate(RPCDelegate rpcDel, string methodName) {
			if (rpcHandlers == null) {
				rpcHandlers = new RPCDelegate[TinyNetStateSyncer.GetNumberOfRPCMethods(GetType())];
			}

			int index = TinyNetStateSyncer.GetRPCMethodIndexFromType(GetType(), methodName);
			rpcHandlers[index] = new RPCDelegate(rpcDel);
		}

		/*protected void CreateDirtyFlag() {
			_dirtyFlag = new BitArray(TinyNetStateSyncer.GetNumberOfSyncedProperties(GetType()));
		}*/

		protected void SetDirtyFlag(int index, bool bValue) {
			_dirtyFlag[index] = bValue;

			if (bValue) {
				_bIsDirty = true;
			}
		}

		private void UpdateDirtyFlag() {
			TinyNetStateSyncer.UpdateDirtyFlagOf(this, _dirtyFlag);

			_lastSendTime = Time.time;
		}

		public void CreateAccessors() {
			Type type;

			for (int i = 0; i < propertiesName.Length; i++) {
				type = propertiesTypes[i];

				if (type == typeof(byte)) {
					byteAccessor.Add(propertiesName[i], new TinyNetPropertyAccessor<byte>(this, propertiesName[i]));
				} else if (type == typeof(sbyte)) {
					sbyteAccessor.Add(propertiesName[i], new TinyNetPropertyAccessor<sbyte>(this, propertiesName[i]));
				} else if (type == typeof(short)) {
					shortAccessor.Add(propertiesName[i], new TinyNetPropertyAccessor<short>(this, propertiesName[i]));
				} else if (type == typeof(ushort)) {
					ushortAccessor.Add(propertiesName[i], new TinyNetPropertyAccessor<ushort>(this, propertiesName[i]));
				} else if (type == typeof(int)) {
					intAccessor.Add(propertiesName[i], new TinyNetPropertyAccessor<int>(this, propertiesName[i]));
				} else if (type == typeof(uint)) {
					uintAccessor.Add(propertiesName[i], new TinyNetPropertyAccessor<uint>(this, propertiesName[i]));
				} else if (type == typeof(long)) {
					longAccessor.Add(propertiesName[i], new TinyNetPropertyAccessor<long>(this, propertiesName[i]));
				} else if (type == typeof(ulong)) {
					ulongAccessor.Add(propertiesName[i], new TinyNetPropertyAccessor<ulong>(this, propertiesName[i]));
				} else if (type == typeof(float)) {
					floatAccessor.Add(propertiesName[i], new TinyNetPropertyAccessor<float>(this, propertiesName[i]));
				} else if (type == typeof(double)) {
					doubleAccessor.Add(propertiesName[i], new TinyNetPropertyAccessor<double>(this, propertiesName[i]));
				} else if (type == typeof(bool)) {
					boolAccessor.Add(propertiesName[i], new TinyNetPropertyAccessor<bool>(this, propertiesName[i]));
				} else if (type == typeof(string)) {
					stringAccessor.Add(propertiesName[i], new TinyNetPropertyAccessor<string>(this, propertiesName[i]));
				}
			}
		}

		/*public TinyNetPropertyAccessor<T> GetAccessor<T>(string propName) {
			TinyNetPropertyAccessor<T> tinyAccessor = null;
			Type type = typeof(T);

			if (type == typeof(byte)) {
				tinyAccessor = (dynamic)byteAccessor[propName];
			} else if (type == typeof(sbyte)) {
				tinyAccessor = (dynamic)sbyteAccessor[propName];
			} else if (type == typeof(short)) {
				tinyAccessor = (dynamic)shortAccessor[propName];
			} else if (type == typeof(ushort)) {
				tinyAccessor = (dynamic)ushortAccessor[propName];
			} else if (type == typeof(int)) {
				tinyAccessor = (dynamic)intAccessor[propName];
			} else if (type == typeof(uint)) {
				tinyAccessor = (dynamic)uintAccessor[propName];
			} else if (type == typeof(long)) {
				tinyAccessor = (dynamic)longAccessor[propName];
			} else if (type == typeof(ulong)) {
				tinyAccessor = (dynamic)ulongAccessor[propName];
			} else if (type == typeof(float)) {
				tinyAccessor = (dynamic)floatAccessor[propName];
			} else if (type == typeof(double)) {
				tinyAccessor = (dynamic)doubleAccessor[propName];
			} else if (type == typeof(bool)) {
				tinyAccessor = (dynamic)boolAccessor[propName];
			} else if (type == typeof(string)) {
				tinyAccessor = (dynamic)stringAccessor[propName];
			}

			return tinyAccessor;
		}*/

		public bool CheckIfPropertyUpdated(string propName, Type type) {
			if (type == typeof(byte)) {
				return byteAccessor[propName].CheckIfChangedAndUpdate(this);
			} else if (type == typeof(sbyte)) {
				return sbyteAccessor[propName].CheckIfChangedAndUpdate(this);
			} else if (type == typeof(short)) {
				return shortAccessor[propName].CheckIfChangedAndUpdate(this);
			} else if (type == typeof(ushort)) {
				return ushortAccessor[propName].CheckIfChangedAndUpdate(this);
			} else if (type == typeof(int)) {
				return intAccessor[propName].CheckIfChangedAndUpdate(this);
			} else if (type == typeof(uint)) {
				return uintAccessor[propName].CheckIfChangedAndUpdate(this);
			} else if (type == typeof(long)) {
				return longAccessor[propName].CheckIfChangedAndUpdate(this);
			} else if (type == typeof(ulong)) {
				return ulongAccessor[propName].CheckIfChangedAndUpdate(this);
			} else if (type == typeof(float)) {
				return floatAccessor[propName].CheckIfChangedAndUpdate(this);
			} else if (type == typeof(double)) {
				return doubleAccessor[propName].CheckIfChangedAndUpdate(this);
			} else if (type == typeof(bool)) {
				return boolAccessor[propName].CheckIfChangedAndUpdate(this);
			} else if (type == typeof(string)) {
				return stringAccessor[propName].CheckIfChangedAndUpdate(this);
			}

			return false;
		}

		public virtual void TinySerialize(NetDataWriter writer, bool firstStateUpdate) {
			if (firstStateUpdate) {
				writer.Put(NetworkID);
			}

			writer.Put(TinyNetStateSyncer.DirtyFlagToInt(_dirtyFlag));

			Type type;
			int maxSyncVar = propertiesName.Length;

			for (int i = 0; i < maxSyncVar; i++) {
				if (_dirtyFlag[i] == false) {
					continue;
				}

				type = propertiesTypes[i];

				if (type == typeof(byte)) {
					writer.Put(byteAccessor[propertiesName[i]].Get(this));
				} else if (type == typeof(sbyte)) {
					writer.Put(sbyteAccessor[propertiesName[i]].Get(this));
				} else if (type == typeof(short)) {
					writer.Put(shortAccessor[propertiesName[i]].Get(this));
				} else if (type == typeof(ushort)) {
					writer.Put(ushortAccessor[propertiesName[i]].Get(this));
				} else if (type == typeof(int)) {
					writer.Put(intAccessor[propertiesName[i]].Get(this));
				} else if (type == typeof(uint)) {
					writer.Put(uintAccessor[propertiesName[i]].Get(this));
				} else if (type == typeof(long)) {
					writer.Put(longAccessor[propertiesName[i]].Get(this));
				} else if (type == typeof(ulong)) {
					writer.Put(ulongAccessor[propertiesName[i]].Get(this));
				} else if (type == typeof(float)) {
					writer.Put(floatAccessor[propertiesName[i]].Get(this));
				} else if (type == typeof(double)) {
					writer.Put(doubleAccessor[propertiesName[i]].Get(this));
				} else if (type == typeof(bool)) {
					writer.Put(boolAccessor[propertiesName[i]].Get(this));
				} else if (type == typeof(string)) {
					writer.Put(stringAccessor[propertiesName[i]].Get(this));
				}
			}
		}

		public virtual void TinyDeserialize(NetDataReader reader, bool firstStateUpdate) {
			if (firstStateUpdate) {
				NetworkID = reader.GetInt();
			}

			TinyNetStateSyncer.IntToDirtyFlag(reader.GetInt(), DirtyFlag);

			Type type;
			int maxSyncVar = propertiesName.Length;

			for (int i = 0; i < maxSyncVar; i++) {
				if (DirtyFlag[i] == false) {
					continue;
				}

				type = propertiesTypes[i];

				if (type == typeof(byte)) {
					byteAccessor[propertiesName[i]].Set(this, reader.GetByte());
				} else if (type == typeof(sbyte)) {
					sbyteAccessor[propertiesName[i]].Set(this, reader.GetSByte());
				} else if (type == typeof(short)) {
					shortAccessor[propertiesName[i]].Set(this, reader.GetShort());
				} else if (type == typeof(ushort)) {
					ushortAccessor[propertiesName[i]].Set(this, reader.GetUShort());
				} else if (type == typeof(int)) {
					intAccessor[propertiesName[i]].Set(this, reader.GetInt());
				} else if (type == typeof(uint)) {
					uintAccessor[propertiesName[i]].Set(this, reader.GetUInt());
				} else if (type == typeof(long)) {
					longAccessor[propertiesName[i]].Set(this, reader.GetLong());
				} else if (type == typeof(ulong)) {
					ulongAccessor[propertiesName[i]].Set(this, reader.GetULong());
				} else if (type == typeof(float)) {
					floatAccessor[propertiesName[i]].Set(this, reader.GetFloat());
				} else if (type == typeof(double)) {
					doubleAccessor[propertiesName[i]].Set(this, reader.GetDouble());
				} else if (type == typeof(bool)) {
					boolAccessor[propertiesName[i]].Set(this, reader.GetBool());
				} else if (type == typeof(string)) {
					stringAccessor[propertiesName[i]].Set(this, reader.GetString());
				}
			}
		}

		public virtual void SendRPC(NetDataWriter stream, string rpcName) {
			RPCMethodInfo rpcMethodInfo = null;
			int rpcMethodIndex = TinyNetStateSyncer.GetRPCMethodInfoFromType(GetType(), rpcName, ref rpcMethodInfo);

			SendRPC(stream, rpcMethodInfo.target, rpcMethodInfo.caller, rpcMethodIndex);
		}

		public virtual void SendRPC(NetDataWriter stream, RPCTarget target, RPCCallers caller, int rpcMethodIndex) {
			if (target == RPCTarget.ClientOwner) {
				if (!isServer || _netIdentity.hasAuthority) {
					//We are not the server or we are the owner, so we can't or have no need to replicate
					return;
				}
			} else if (target == RPCTarget.Server) {
				if (isServer) {
					//We are the server, no need to replicate
					return;
				}
			} else if (target == RPCTarget.Everyone) {
				if (!isServer) {
					//We are not the server, we don't have a connection to everyone
					return;
				}
			}

			switch(target) {
				case RPCTarget.Server:
					TinyNetClient.instance.SendRPCToServer(stream, rpcMethodIndex, this);
					return;
				case RPCTarget.ClientOwner:
					TinyNetServer.instance.SendRPCToClientOwner(stream, rpcMethodIndex, this);
					return;
				case RPCTarget.Everyone:
					TinyNetServer.instance.SendRPCToAllCLients(stream, rpcMethodIndex, this);
					return;
			}
		}

		public virtual bool InvokeRPC(int rpcMethodIndex, NetDataReader reader) {
			if (rpcHandlers[rpcMethodIndex] == null) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("TinyNetBehaviour::InvokeRPC netId:" + NetworkID + " RPCDelegate is not registered."); }
				return false;
			}

			rpcHandlers[rpcMethodIndex].Invoke(reader);

			return true;
		}

		public void TinyNetUpdate() {
			//if (IsTimeToUpdate()) {
			UpdateDirtyFlag();

			if (_bIsDirty) {
				TinyNetServer.instance.SendStateUpdateToAllConnections(this, GetNetworkChannel());

				_bIsDirty = false;
			}
				
			//}
		}

		/*public bool IsTimeToUpdate() {
			if (_bIsDirty && Time.time - _lastSendTime > GetNetworkSendInterval()) {
				UpdateDirtyFlag();
				return true;
			}

			return false;
		}*/

		/// <summary>
		/// Remember that this is called first and before variables are synced.
		/// </summary>
		public virtual void OnNetworkCreate() {
			TinyNetStateSyncer.OutPropertyNamesFromType(GetType(), out propertiesName);
			TinyNetStateSyncer.OutPropertyTypesFromType(GetType(), out propertiesTypes);

			CreateAccessors();
		}

		public virtual void OnNetworkDestroy() {
		}

		public virtual void OnStartServer() {
		}

		public virtual void OnStartClient() {
		}

		public virtual void OnStartLocalPlayer() {
		}

		public virtual void OnStartAuthority() {
		}

		public virtual void OnStopAuthority() {
		}

		public virtual void OnSetLocalVisibility(bool vis) {
		}

		public virtual LiteNetLib.SendOptions GetNetworkChannel() {
			return LiteNetLib.SendOptions.ReliableOrdered;
		}

		public virtual float GetNetworkSendInterval() {
			return 0.1f;
		}
	}
}
