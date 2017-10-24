using LiteNetLib.Utils;
using LiteNetLib;
using UnityEngine;
using System.Collections.Generic;
using TinyBirdNet.Messaging;
using TinyBirdUtils;

namespace TinyBirdNet {

	public class TinyNetMessageHandlers {
		Dictionary<ushort, TinyNetMessageDelegate> _msgHandlers = new Dictionary<ushort, TinyNetMessageDelegate>();

		internal void RegisterHandlerSafe(ushort msgType, TinyNetMessageDelegate handler) {
			if (handler == null) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("RegisterHandlerSafe id:" + msgType + " handler is null"); }
				return;
			}

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("RegisterHandlerSafe id:" + msgType + " handler:" + handler.Method.Name); }
			if (_msgHandlers.ContainsKey(msgType)) {
				//if (TinyNetLogLevel.logError) { TinyLogger.LogError("RegisterHandlerSafe id:" + msgType + " handler:" + handler.Method.Name + " conflict"); }
				return;
			}
			_msgHandlers.Add(msgType, handler);
		}

		public void RegisterHandler(ushort msgType, TinyNetMessageDelegate handler) {
			if (handler == null) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("RegisterHandler id:" + msgType + " handler is null"); }
				return;
			}

			if (msgType <= TinyNetMsgType.InternalHighest) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("RegisterHandler: Cannot replace system message handler " + msgType); }
				return;
			}

			if (_msgHandlers.ContainsKey(msgType)) {
				if (TinyNetLogLevel.logDebug) { TinyLogger.Log("RegisterHandler replacing " + msgType); }

				_msgHandlers.Remove(msgType);
			}
			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("RegisterHandler id:" + msgType + " handler:" + handler.Method.Name); }
			_msgHandlers.Add(msgType, handler);
		}

		public void UnregisterHandler(ushort msgType) {
			_msgHandlers.Remove(msgType);
		}

		internal TinyNetMessageDelegate GetHandler(ushort msgType) {
			if (_msgHandlers.ContainsKey(msgType)) {
				return _msgHandlers[msgType];
			}
			return null;
		}

		internal Dictionary<ushort, TinyNetMessageDelegate> GetHandlers() {
			return _msgHandlers;
		}

		internal void ClearMessageHandlers() {
			_msgHandlers.Clear();
		}
	}
}

namespace TinyBirdNet.Messaging {

	public interface ITinyNetMessage {
		// De-serialize the contents of the reader into this message
		void Deserialize(NetDataReader reader);

		// Serialize the contents of this message into the writer
		void Serialize(NetDataWriter writer);

		ushort msgType { get; }
	}

	// Handles network messages on client and server
	public delegate void TinyNetMessageDelegate(TinyNetMessageReader netMsg);

	// built-in system network messages
	public class TinyNetMsgType {
		// internal system messages - cannot be replaced by user code
		public const ushort ObjectDestroy = 1;
		public const ushort Rpc = 2;
		public const ushort ObjectSpawnMessage = 3;
		public const ushort Owner = 4;
		public const ushort Command = 5;
		public const ushort LocalPlayerTransform = 6;
		public const ushort SyncEvent = 7;
		public const ushort StateUpdate = 8;
		public const ushort SyncList = 9;
		public const ushort ObjectSpawnScene = 10;
		public const ushort NetworkInfo = 11;
		public const ushort SpawnFinished = 12;
		public const ushort ObjectHide = 13;
		public const ushort CRC = 14;
		public const ushort LocalClientAuthority = 15;
		public const ushort LocalChildTransform = 16;
		public const ushort PeerClientAuthority = 17;

		// used for profiling
		internal const ushort UserMessage = 0;
		internal const ushort HLAPIMsg = 28;
		internal const ushort LLAPIMsg = 29;
		internal const ushort HLAPIResend = 30;
		internal const ushort HLAPIPending = 31;

		public const ushort InternalHighest = 31;

		// public system messages - can be replaced by user code
		public const ushort Connect = 32;
		public const ushort Disconnect = 33;
		public const ushort Error = 34;
		public const ushort Ready = 35;
		public const ushort NotReady = 36;
		public const ushort AddPlayer = 37;
		public const ushort RemovePlayer = 38;
		public const ushort Scene = 39;
		public const ushort Animation = 40;
		public const ushort AnimationParameters = 41;
		public const ushort AnimationTrigger = 42;
		public const ushort LobbyReadyToBegin = 43;
		public const ushort LobbySceneLoaded = 44;
		public const ushort LobbyAddPlayerFailed = 45;
		public const ushort LobbyReturnToLobby = 46;
#if ENABLE_UNET_HOST_MIGRATION
        public const ushort ReconnectPlayer = 47;
#endif

		//NOTE: update msgLabels below if this is changed.
		public const ushort Highest = 47;

		static internal string[] msgLabels =
		{
			"none",
			"ObjectDestroy",
			"Rpc",
			"ObjectSpawnMessage",
			"Owner",
			"Command",
			"LocalPlayerTransform",
			"SyncEvent",
			"StateUpdate",
			"SyncList",
			"ObjectSpawnScene", // 10
            "NetworkInfo",
			"SpawnFinished",
			"ObjectHide",
			"CRC",
			"LocalClientAuthority",
			"LocalChildTransform",
			"PeerClientAuthority",
			"",
			"",
			"", // 20
            "",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"",
			"", // 30
            "", // - SystemInternalHighest
            "Connect", // 32,
            "Disconnect",
			"Error",
			"Ready",
			"NotReady",
			"AddPlayer",
			"RemovePlayer",
			"Scene",
			"Animation", // 40
            "AnimationParams",
			"AnimationTrigger",
			"LobbyReadyToBegin",
			"LobbySceneLoaded",
			"LobbyAddPlayerFailed", // 45
            "LobbyReturnToLobby", // 46
#if ENABLE_UNET_HOST_MIGRATION
            "ReconnectPlayer", // 47
#endif
        };

		static public string MsgTypeToString(ushort value) {
			if (value < 0 || value > Highest) {
				return string.Empty;
			}

			string result = msgLabels[value];

			if (string.IsNullOrEmpty(result)) {
				result = "[" + value + "]";
			}

			return result;
		}
	}

	public class TinyNetMessageReader {
		public const int MaxMessageSize = (64 * 1024) - 1;

		public ushort msgType;
		public TinyNetConnection tinyNetConn;
		public NetDataReader reader;
		public SendOptions channelId;

		public static string Dump(byte[] payload, int sz) {
			string outStr = "[";

			for (int i = 0; i < sz; i++) {
				outStr += (payload[i] + " ");
			}

			outStr += "]";
			return outStr;
		}

		public TMsg ReadMessage<TMsg>() where TMsg : ITinyNetMessage, new() {
			var msg = new TMsg();
			msg.Deserialize(reader);
			return msg;
		}

		public void ReadMessage<TMsg>(TMsg msg) where TMsg : ITinyNetMessage {
			msg.Deserialize(reader);
		}
	}

	public class TinyNetObjectDestroyMessage : ITinyNetMessage {
		public int networkID;

		public ushort msgType { get { return 1; } }

		public virtual void Deserialize(NetDataReader reader) {
			networkID = reader.GetInt();
		}

		public virtual void Serialize(NetDataWriter writer) {
			writer.Put(networkID);
		}
	}

	public class TinyNetObjectSpawnMessage : ITinyNetMessage {
		public int networkID;
		public int assetId;
		public Vector3 position;
		public byte[] initialState;

		public ushort msgType { get { return 3; } }

		public virtual void Deserialize(NetDataReader reader) {
			networkID = reader.GetInt();
			assetId = reader.GetInt();
			position = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
			initialState = reader.GetRemainingBytes();
		}

		public virtual void Serialize(NetDataWriter writer) {
			writer.Put(networkID);
			writer.Put(assetId);
			writer.Put(position.x);
			writer.Put(position.y);
			writer.Put(position.z);
			writer.Put(initialState);
		}
	}

	public class TinyNetOwnerMessage : ITinyNetMessage {
		public int networkID;
		public short connectId;

		public ushort msgType { get { return 4; } }

		public void Deserialize(NetDataReader reader) {
			networkID = reader.GetInt();
			connectId = reader.GetShort();
		}

		public void Serialize(NetDataWriter writer) {
			writer.Put(networkID);
			writer.Put(connectId);
		}
	}

	public class TinyNetObjectSpawnSceneMessage : ITinyNetMessage {
		public int networkID;
		public int sceneId;
		public Vector3 position;
		public byte[] initialState;

		public ushort msgType { get { return 10; } }

		public void Deserialize(NetDataReader reader) {
			networkID = reader.GetInt();
			sceneId = reader.GetInt();
			position = new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
			initialState = reader.GetRemainingBytes();
		}

		public void Serialize(NetDataWriter writer) {
			writer.Put(networkID);
			writer.Put(sceneId);
			writer.Put(position.x);
			writer.Put(position.y);
			writer.Put(position.z);
			writer.Put(initialState);
		}
	}

	public class TinyNetObjectSpawnFinishedMessage : ITinyNetMessage {
		public byte state;

		public ushort msgType { get { return 12; } }

		public void Deserialize(NetDataReader reader) {
			state = reader.GetByte();
		}

		public void Serialize(NetDataWriter writer) {
			writer.Put(state);
		}
	}

	public class TinyNetReadyMessage : ITinyNetMessage {

		public ushort msgType { get { return 35; } }

		public void Deserialize(NetDataReader reader) {
		}

		public void Serialize(NetDataWriter writer) {
		}
	}

	public class TinyNetNotReadyMessage : ITinyNetMessage {

		public ushort msgType { get { return 36; } }

		public void Deserialize(NetDataReader reader) {
		}

		public void Serialize(NetDataWriter writer) {
		}
	}

	public class TinyNetRemovePlayerMessage : ITinyNetMessage {
		public long connectId;

		public ushort msgType { get { return 38; } }

		public void Deserialize(NetDataReader reader) {
			connectId = (short)reader.GetLong();
		}

		public void Serialize(NetDataWriter writer) {
			writer.Put(connectId);
		}
	}
}
