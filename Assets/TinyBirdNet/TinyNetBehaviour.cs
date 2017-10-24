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

		public int NetworkID { get; protected set; }

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

		private string[] propertiesName;
		private Type[] propertiesTypes;

		private BitArray _dirtyFlag = new BitArray(32);
		public BitArray DirtyFlag { get { return _dirtyFlag; } private set { _dirtyFlag = value; } }

		protected float _lastSendTime;

		public bool isServer { get { return TinyNetGameManager.instance.isServer; } }
		public bool isClient { get { return TinyNetGameManager.instance.isClient; } }
		public bool hasAuthority { get { return netIdentity.hasAuthority; } }

		TinyNetIdentity _netIdentity;
		protected TinyNetIdentity netIdentity {
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

		/*protected void CreateDirtyFlag() {
			_dirtyFlag = new BitArray(TinyNetStateSyncer.GetNumberOfSyncedProperties(GetType()));
		}*/

		protected void SetDirtyFlag(int index, bool bValue) {
			_dirtyFlag[index] = bValue;
		}

		private void UpdateDirtyFlag() {
			TinyNetStateSyncer.UpdateDirtyFlagOf(this, DirtyFlag);

			_lastSendTime = Time.time;
		}

		public void CreateAccessors() {
			Type type;

			for (int i = 0; i < propertiesName.Length; i++) {
				type = propertiesTypes[i];

				if (type == typeof(byte)) {
					byteAccessor.Add(propertiesName[i], new TinyNetPropertyAccessor<byte>(propertiesName[i]));
				} else if (type == typeof(sbyte)) {
					sbyteAccessor.Add(propertiesName[i], new TinyNetPropertyAccessor<sbyte>(propertiesName[i]));
				} else if (type == typeof(short)) {
					shortAccessor.Add(propertiesName[i], new TinyNetPropertyAccessor<short>(propertiesName[i]));
				} else if (type == typeof(ushort)) {
					ushortAccessor.Add(propertiesName[i], new TinyNetPropertyAccessor<ushort>(propertiesName[i]));
				} else if (type == typeof(int)) {
					intAccessor.Add(propertiesName[i], new TinyNetPropertyAccessor<int>(propertiesName[i]));
				} else if (type == typeof(uint)) {
					uintAccessor.Add(propertiesName[i], new TinyNetPropertyAccessor<uint>(propertiesName[i]));
				} else if (type == typeof(long)) {
					longAccessor.Add(propertiesName[i], new TinyNetPropertyAccessor<long>(propertiesName[i]));
				} else if (type == typeof(ulong)) {
					ulongAccessor.Add(propertiesName[i], new TinyNetPropertyAccessor<ulong>(propertiesName[i]));
				} else if (type == typeof(float)) {
					floatAccessor.Add(propertiesName[i], new TinyNetPropertyAccessor<float>(propertiesName[i]));
				} else if (type == typeof(double)) {
					doubleAccessor.Add(propertiesName[i], new TinyNetPropertyAccessor<double>(propertiesName[i]));
				} else if (type == typeof(bool)) {
					boolAccessor.Add(propertiesName[i], new TinyNetPropertyAccessor<bool>(propertiesName[i]));
				} else if (type == typeof(string)) {
					stringAccessor.Add(propertiesName[i], new TinyNetPropertyAccessor<string>(propertiesName[i]));
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

		public void TinySerialize(NetDataWriter writer, bool firstStateUpdate) {
			writer.Put(TinyNetStateSyncer.DirtyFlagToInt(DirtyFlag));

			Type type;
			int maxSyncVar = propertiesName.Length;

			for (int i = 0; i < maxSyncVar; i++) {
				if (DirtyFlag[i] == false) {
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

		public void TinyDeserialize(NetDataReader reader, bool firstStateUpdate) {
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

		public bool IsTimeToUpdate() {
			if (Time.time - _lastSendTime > GetNetworkSendInterval()) {
				UpdateDirtyFlag();
				return true;
			}

			return false;
		}

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

		public virtual int GetNetworkChannel() {
			return (int)LiteNetLib.SendOptions.ReliableOrdered;
		}

		public virtual float GetNetworkSendInterval() {
			return 0.1f;
		}
	}
}