using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TinyBirdUtils;
using LiteNetLib.Utils;
using TinyBirdNet.Messaging;
using TinyBirdNet.Utils;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TinyBirdNet {

	/// <summary>
	/// Any <see cref="GameObject" /> that contains this component, can be spawned accross the network.
	/// <para> This is basically a container for an "universal id" accross the network. </para>
	/// <para> In addition, TinyBirdNet handles it's spawning, serialization, RPC, and mostly anything you need to create
	/// a new instance of it in a multiplayer game and have it automatically synced. </para>
	/// </summary>
	/// <seealso cref="UnityEngine.MonoBehaviour" />
	/// <seealso cref="TinyBirdNet.ITinyNetInstanceID" />
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	[AddComponentMenu("TinyBirdNet/TinyNetIdentity")]
	public class TinyNetIdentity : MonoBehaviour, ITinyNetInstanceID {

		/// <summary>
		/// Used as a stopgag in case this object has tried to be initialized twice.
		/// </summary>
		protected bool bStartClientTwiceTest = false;

		/// <inheritdoc />
		public TinyNetworkID TinyInstanceID { get; protected set; }

		/// <summary>
		/// If true, this object will not be spawned on clients.
		/// </summary>
		[SerializeField] bool _serverOnly;
		/// <summary>
		/// If true, this object is owned by a client.
		/// <para> This does not have any effect for TinyBirdNet, use it for your game. </para>
		/// </summary>
		[SerializeField] bool _localPlayerAuthority;
		/// <summary>
		/// The asset unique identifier
		/// </summary>
		[SerializeField] string _assetGUID;
		/// <summary>
		/// If this object is a scene object, this will be set.
		/// </summary>
		[SerializeField] int _sceneID;

		/// <summary>
		/// The list of <see cref="ITinyNetComponent"/> components in this <see cref="GameObject"/>.
		/// <para> You can use up to 64 components, the header size is automatically updated. </para>
		/// <para> For now, changing the number of components at runtime is not supported. </para>
		/// </summary>
		ITinyNetComponent[] _tinyNetComponents;

		/// <summary>
		/// The dirty flag is a BitArray that represents if a ITinyNetComponent is dirty.
		/// </summary>
		private BitArray _dirtyFlag;

		public bool IsDirty {
			get; protected set;
		}

		public enum TinyNetComponentEvents {
			OnNetworkCreate,
			OnNetworkDestroy,
			OnStartServer,
			OnStartClient,
			OnStartAuthority,
			OnStopAuthority,
			OnGiveAuthority,
			OnRemoveAuthority,
			OnGiveLocalVisibility,
			OnRemoveLocalVisibility
		}

		Dictionary<TinyNetComponentEvents, LinkedList<System.Action>> _registeredEventHandlers = new Dictionary<TinyNetComponentEvents, LinkedList<System.Action>>();

		protected static TinyNetStateReader _recycleReader = new TinyNetStateReader();
		protected static NetDataWriter _recycleWriter = new NetDataWriter();

		/// <summary>
		/// [Server only] Shortcut, prevents you to have to loop through all connections and objects to find owner.
		/// </summary>
		protected TinyNetConnection _connectionToOwnerClient;

		/// <inheritdoc />
		public bool isServer { get { return TinyNetGameManager.instance.isServer; } }
		/// <inheritdoc />
		public bool isClient { get { return TinyNetGameManager.instance.isClient; } }

		/// <summary>
		/// Gets a value indicating whether this object only exists on the server.
		/// </summary>
		/// <value>
		///   <c>true</c> if server only; otherwise, <c>false</c>.
		/// </value>
		public bool ServerOnly { get { return _serverOnly; } }

		/// <summary>
		/// Gets a value indicating whether this instance has authority.
		/// </summary>
		/// <value>
		///   <c>true</c> if this instance has authority; otherwise, <c>false</c>.
		/// </value>
		public bool HasAuthority { get; private set; }
		//public bool hasOwnership { get { return _bIsOwner;  } }

		//public short playerControllerId { get { return _ownerPlayerId; } }
		/// <summary>
		/// [Server Only] Gets the connection to owner client.
		/// </summary>
		/// <value>
		/// The connection to owner client.
		/// </value>
		public TinyNetConnection ConnectionToOwnerClient { get { return _connectionToOwnerClient; } }

		/// <summary>
		/// Gets the object scene identifier.
		/// </summary>
		/// <value>
		/// The object scene identifier.
		/// </value>
		public int sceneID { get { return _sceneID; } }

		/// <summary>
		/// Gets the asset unique identifier.
		/// </summary>
		/// <value>
		/// The asset unique identifier.
		/// </value>
		public string assetGUID {
			get {
#if UNITY_EDITOR
				// This is important because sometimes OnValidate does not run (like when adding view to prefab with no child links)
				if (!IsValidAssetGUI(_assetGUID)) {
					SetupIDs();
				}
#endif
				return _assetGUID;
			}
		}

		/// <inheritdoc />
		public void ReceiveNetworkID(TinyNetworkID newID) {
			TinyInstanceID = newID;
		}

		/// <summary>
		/// Forces the scene identifier. Only used when fixing duplicate scene IDs duing post-processing
		/// </summary>
		/// <param name="newSceneId">The new scene identifier.</param>
		public void ForceSceneId(int newSceneId) {
			_sceneID = newSceneId;
		}

		/// <summary>
		/// Forces the authority setting.
		/// </summary>
		/// <param name="authority">if set to <c>true</c> it will have authority.</param>
		public void ForceAuthority(bool authority) {
			if (HasAuthority == authority) {
				return;
			}

			HasAuthority = authority;

			if (authority) {
				OnStartAuthority();
			} else {
				OnStopAuthority();
			}
		}

		/// <summary>
		/// Caches the <see cref="ITinyNetComponent"/>
		/// </summary>
		void CacheTinyNetObjects() {
			if (_tinyNetComponents == null) {
				_tinyNetComponents = GetComponentsInChildren<ITinyNetComponent>(true);
			}

			if (_tinyNetComponents.Length <= 8) {
				_dirtyFlag = new BitArray(8);
			} else if (_tinyNetComponents.Length <= 16) {
				_dirtyFlag = new BitArray(16);
			} else if (_tinyNetComponents.Length <= 32) {
				_dirtyFlag = new BitArray(32);
			} else if (_tinyNetComponents.Length <= 64) {
				_dirtyFlag = new BitArray(64);
			} else {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("TinyNetIdentity::CacheTinyNetObjects amount of ITinyNetComponents is bigger than 64."); }
				return;
			}
		}

		/*// Just some dirtyflag testing
		private void Start() {
			_dirtyFlag = new BitArray(13);
			BitArray test = new BitArray(_dirtyFlag.Count);

			for (int i = 0; i < _dirtyFlag.Count; i++) {
				_dirtyFlag.Set(i, Random.Range(0, 2) == 0);
			}

			ushort compact = (ushort)TinyBitArrayUtil.BitArrayToU64(_dirtyFlag);

			Debug.Log(TinyBitArrayUtil.Display(compact));

			TinyBitArrayUtil.U64ToBitArray(compact, test);

			Debug.Log(TinyBitArrayUtil.Display(_dirtyFlag));
			Debug.Log(TinyBitArrayUtil.Display(test));

			for (int i = 0; i < _dirtyFlag.Count; i++) {
				if (_dirtyFlag[i] != test[i]) {
					Debug.Log("FALSE");
				}
			}
		}*/

		public ITinyNetComponent GetComponentById(byte localId) {
			if (localId < 0 || localId >= _tinyNetComponents.Length) {
				return null;
			}

			return _tinyNetComponents[localId];
		}

		public void RegisterEventHandler(TinyNetComponentEvents eventType, System.Action handler) {
			LinkedList<System.Action> handlers;
			if (!_registeredEventHandlers.TryGetValue(eventType, out handlers)) {
				handlers = new LinkedList<System.Action>();
				_registeredEventHandlers.Add(eventType, handlers);
			}

			LinkedListNode<System.Action> nodeToFind = handlers.Find(handler);
			if (nodeToFind == null) {
				handlers.AddLast(handler);
			}
		}

		public void UnregisterEventHandler(TinyNetComponentEvents eventType, System.Action handler) {
			LinkedList<System.Action> handlers;
			if (!_registeredEventHandlers.TryGetValue(eventType, out handlers)) {
				return;
			}

			LinkedListNode<System.Action> nodeToFind = handlers.Find(handler);
			if (nodeToFind != null) {
				handlers.Remove(nodeToFind);
			}
		}

		/// <summary>
		/// [Server Only] Called every server update, after all FixedUpdates.
		/// <para> It is used to check if it is time to send the current state to clients. </para>
		/// </summary>
		public void TinyNetUpdate() {
			IsDirty = false;

			for (int i = 0; i < _tinyNetComponents.Length; i++) {
				_tinyNetComponents[i].TinyNetUpdate();

				_dirtyFlag[i] = _tinyNetComponents[i].IsDirty;
				if (_dirtyFlag[i] == true) {
					IsDirty = true;
				}
			}
		}

		/// <summary>
		/// Called on the server to serialize all <see cref="ITinyNetComponent"/> attached to this prefab.
		/// </summary>
		/// <param name="writer"></param>
		public void TinySerialize(NetDataWriter writer, bool firstStateUpdate) {
			if (firstStateUpdate) {
				for (int i = 0; i < _tinyNetComponents.Length; i++) {
					// We are getting the length of how much this obj wrote.
					_recycleWriter.Reset();

					_tinyNetComponents[i].TinySerialize(_recycleWriter, firstStateUpdate);
					// TODO: Compact this
					writer.Put(_recycleWriter.Length);
					writer.Put(_recycleWriter.Data, 0, _recycleWriter.Length);
				}

				return;
			}

			switch (_dirtyFlag.Length) {
				case 8:
					writer.Put((byte)TinyBitArrayUtil.BitArrayToU64(_dirtyFlag));
					break;
				case 16:
					writer.Put((ushort)TinyBitArrayUtil.BitArrayToU64(_dirtyFlag));
					break;
				case 32:
					writer.Put((uint)TinyBitArrayUtil.BitArrayToU64(_dirtyFlag));
					break;
				case 64:
					writer.Put((ulong)TinyBitArrayUtil.BitArrayToU64(_dirtyFlag));
					break;
			}

			for (int i = 0; i < _tinyNetComponents.Length; i++) {
				if (_dirtyFlag[i] == true) {
					// We are getting the length of how much this obj wrote.
					_recycleWriter.Reset();

					_tinyNetComponents[i].TinySerialize(_recycleWriter, firstStateUpdate);
					//Debug.Log("[Serialize] Size: " + _recycleWriter.Length + ", DirtyFlag: " + TinyBitArrayUtil.Display(_recycleWriter.Data[0]));
					// TODO: Compact this
					writer.Put(_recycleWriter.Length);
					writer.Put(_recycleWriter.Data, 0, _recycleWriter.Length);
				}
			}
		}

		/// <summary>
		/// Deserializes all <see cref="ITinyNetComponent"/> data.
		/// </summary>
		/// <param name="reader">The reader.</param>
		/// <param name="bInitialState">if set to <c>true</c> [b initial state].</param>
		public virtual void TinyDeserialize(TinyNetStateReader reader, bool firstStateUpdate) {
			if (firstStateUpdate && _tinyNetComponents == null) {
				if (TinyNetLogLevel.logWarn) { TinyLogger.LogWarning("TinyNetIdentity::TinyDeserialize called with firstStateUpdate true, but _tinyNetComponents is null."); }
				CacheTinyNetObjects();
			}

			if (firstStateUpdate) {
				for (int i = 0; i < _tinyNetComponents.Length; i++) {
					_tinyNetComponents[i].ReceiveNetworkID(new TinyNetworkID(TinyInstanceID.NetworkID, (byte)(i + 1)));

					_recycleReader.Clear();
					int rSize = reader.GetInt();
					_recycleReader.SetSource(reader.RawData, reader.Position, rSize);

					_recycleReader.SetFrameTick(reader.FrameTick);
					_tinyNetComponents[i].TinyDeserialize(_recycleReader, firstStateUpdate);
					// We jump the reader position to the amount of data we read.
					reader.SkipBytes(rSize);
				}

				return;
			}

			switch (_dirtyFlag.Length) {
				case 8:
					TinyBitArrayUtil.U64ToBitArray(reader.GetByte(), _dirtyFlag);
					break;
				case 16:
					TinyBitArrayUtil.U64ToBitArray(reader.GetUShort(), _dirtyFlag);
					break;
				case 32:
					TinyBitArrayUtil.U64ToBitArray(reader.GetUInt(), _dirtyFlag);
					break;
				case 64:
					TinyBitArrayUtil.U64ToBitArray(reader.GetULong(), _dirtyFlag);
					break;
			}

			for (int i = 0; i < _tinyNetComponents.Length; i++) {
				if (_dirtyFlag[i] == true) {
					_recycleReader.Clear();
					int rSize = reader.GetInt();
					_recycleReader.SetSource(reader.RawData, reader.Position, rSize);

					_recycleReader.SetFrameTick(reader.FrameTick);
					//Debug.Log("[Deserialize] Size: " + rSize + ", DirtyFlag: " + TinyBitArrayUtil.Display(_recycleReader.PeekByte()));
					_tinyNetComponents[i].TinyDeserialize(_recycleReader, firstStateUpdate);
					// We jump the reader position to the amount of data we read.
					reader.SkipBytes(rSize);
				}
			}
		}

#if UNITY_EDITOR
		/// <summary>
		/// Determines whether the guiven GUID is valid.
		/// </summary>
		/// <param name="assetGUID">The asset unique identifier.</param>
		/// <returns>
		///   <c>true</c> if valid; otherwise, <c>false</c>.
		/// </returns>
		bool IsValidAssetGUI(string assetGUID) {
			string test = AssetDatabase.GetAssetPath(gameObject);
			test = AssetDatabase.AssetPathToGUID(test);

			return test.Equals(assetGUID);
		}

		/// <summary>
		/// Determines whether this instance is a prefab.
		/// </summary>
		/// <returns>
		///   <c>true</c> if this instance is a prefab; otherwise, <c>false</c>.
		/// </returns>
		bool IsPrefab() {
			PrefabType prefabType = PrefabUtility.GetPrefabType(gameObject);
			if (prefabType == PrefabType.Prefab)
				return true;
			return false;
		}

		/// <summary>
		/// Sets the asset unique identifier.
		/// </summary>
		/// <param name="newGUID">The new unique identifier.</param>
		void SetAssetGUID(string newGUID) {
			_assetGUID = newGUID;
		}

		/// <summary>
		/// Sets the asset unique identifier.
		/// </summary>
		/// <param name="prefab">The prefab.</param>
		void SetAssetGUID(GameObject prefab) {
			string path = AssetDatabase.GetAssetPath(prefab);
			_assetGUID = AssetDatabase.AssetPathToGUID(path);
		}

		/// <summary>
		/// Called by UnityEditor.
		/// </summary>
		void OnValidate() {
			if (_serverOnly && _localPlayerAuthority) {
				if (TinyNetLogLevel.logWarn) { TinyLogger.LogWarning("Disabling Local Player Authority for " + gameObject + " because it is server-only."); }
				_localPlayerAuthority = false;
			}

			SetupIDs();
		}

		/// <summary>
		/// Setups the ids.
		/// </summary>
		void SetupIDs() {
			if (IsPrefab()) {
				SetAssetGUID(gameObject);
			}
		}
#endif
		/// <summary>
		/// Sets the asset unique identifier during play.
		/// </summary>
		/// <param name="newAssetGUID">The new asset unique identifier.</param>
		public void SetDynamicAssetGUID(string newAssetGUID) {
			if (_assetGUID == null || _assetGUID == string.Empty || /*!IsValidAssetGUI(_assetGUID) || */_assetGUID.Equals(newAssetGUID)) {
				_assetGUID = newAssetGUID;
			} else {
				if (TinyNetLogLevel.logWarn) { TinyLogger.LogWarning("SetDynamicAssetId object already has an assetId <" + _assetGUID + ">"); }
			}
		}

		/// <summary>
		/// Called when this object is created.
		/// <para> Always called, regardless of being a client or server. Called before variables are synced. (Order: 0) </para>
		/// </summary>
		public virtual void OnNetworkCreate() {
			CacheTinyNetObjects();

			LinkedList<System.Action> handlers;
			if (!_registeredEventHandlers.TryGetValue(TinyNetComponentEvents.OnNetworkCreate, out handlers)) {
				return;
			}

			for (LinkedListNode<System.Action> currentNode = handlers.First; currentNode != null; currentNode = currentNode.Next) {
				currentNode.Value();
			}
		}

		/// <summary>
		/// Called when the object receives an order to be destroyed from the network,
		/// in a listen server the object could just be unspawned without being actually destroyed.
		/// </summary>
		public virtual void OnNetworkDestroy() {
			LinkedList<System.Action> handlers;
			if (!_registeredEventHandlers.TryGetValue(TinyNetComponentEvents.OnNetworkDestroy, out handlers)) {
				return;
			}

			for (LinkedListNode<System.Action> currentNode = handlers.First; currentNode != null; currentNode = currentNode.Next) {
				currentNode.Value();
			}

			/*for (int i = 0; i < _tinyNetObjects.Length; i++) {
				TinyNetScene.RemoveTinyNetObjectFromList(_tinyNetObjects[i]);
				_tinyNetObjects[i].OnNetworkDestroy();
			}*/
		}

		/// <summary>
		/// Called when an object is spawned on the server.
		/// <para> Called on the server when Spawn is called for this object. (Order: 1) </para>
		/// </summary>
		/// <param name="allowAlreadySetId">If the object already have a NetworkId, it was probably recycled.</param>
		public void OnStartServer(bool allowAlreadySetId) {
			if (_localPlayerAuthority) {
				// local player on server has NO authority
				HasAuthority = false;
			} else {
				// enemy on server has authority
				HasAuthority = true;
			}

			// If the instance/net ID is invalid here then this is an object instantiated from a prefab and the server should assign a valid ID
			if (TinyInstanceID == null) {
				TinyInstanceID = TinyNetGameManager.instance.NextNetworkID;
			} else {
				if (allowAlreadySetId) {
					//allowed
				} else {
					if (TinyNetLogLevel.logError) { TinyLogger.LogError("Object has already set netId " + TinyInstanceID + " for " + gameObject); }
					return;
				}
			}

			// If anything goes wrong, blame this person: https://forum.unity.com/threads/getcomponentsinchildren.4582/#post-33983
			for (int i = 0; i < _tinyNetComponents.Length; i++) {
				_tinyNetComponents[i].ReceiveNetworkID(new TinyNetworkID(TinyInstanceID.NetworkID, (byte)(i + 1)));
			}

			{ // Calling OnStartServer on those who registered
				LinkedList<System.Action> handlers;
				if (!_registeredEventHandlers.TryGetValue(TinyNetComponentEvents.OnStartServer, out handlers)) {
					return;
				}

				for (LinkedListNode<System.Action> currentNode = handlers.First; currentNode != null; currentNode = currentNode.Next) {
					currentNode.Value();
				}
			}

			if (TinyNetLogLevel.logDev) { TinyLogger.Log("OnStartServer " + gameObject + " netId:" + TinyInstanceID); }

			if (HasAuthority) {
				OnStartAuthority();
			}
		}

		/// <summary>
		/// Called when an object is spawned on the client.
		/// <para> Called on the client when the object is spawned. Called after variables are synced. (Order: 2) </para>
		/// </summary>
		public void OnStartClient() {
			if (bStartClientTwiceTest) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("OnStartClient CALLED TWICE FOR: " + gameObject + " netId:" + TinyInstanceID + " localPlayerAuthority: " + _localPlayerAuthority); }
			} else {
				bStartClientTwiceTest = true;
			}

			{ // Calling OnStartClient on those who registered
				LinkedList<System.Action> handlers;
				if (!_registeredEventHandlers.TryGetValue(TinyNetComponentEvents.OnStartClient, out handlers)) {
					return;
				}

				for (LinkedListNode<System.Action> currentNode = handlers.First; currentNode != null; currentNode = currentNode.Next) {
					currentNode.Value();
				}
			}

			if (TinyNetLogLevel.logDev) { TinyLogger.Log("OnStartClient " + gameObject + " netId:" + TinyInstanceID + " localPlayerAuthority: " + _localPlayerAuthority); }
		}

		/// <summary>
		/// Called on Server or Client when receiving Authority
		/// </summary>
		public virtual void OnStartAuthority() {
			LinkedList<System.Action> handlers;
			if (!_registeredEventHandlers.TryGetValue(TinyNetComponentEvents.OnStartAuthority, out handlers)) {
				return;
			}

			for (LinkedListNode<System.Action> currentNode = handlers.First; currentNode != null; currentNode = currentNode.Next) {
				currentNode.Value();
			}
		}

		/// <summary>
		/// Called on Server or Client when lost Authorithy.
		/// </summary>
		public virtual void OnStopAuthority() {
			LinkedList<System.Action> handlers;
			if (!_registeredEventHandlers.TryGetValue(TinyNetComponentEvents.OnStopAuthority, out handlers)) {
				return;
			}

			for (LinkedListNode<System.Action> currentNode = handlers.First; currentNode != null; currentNode = currentNode.Next) {
				currentNode.Value();
			}
		}

		/// <summary>
		/// Called on Server when giving Authority to a client.
		/// </summary>
		public virtual void OnGiveAuthority() {
			LinkedList<System.Action> handlers;
			if (!_registeredEventHandlers.TryGetValue(TinyNetComponentEvents.OnGiveAuthority, out handlers)) {
				return;
			}

			for (LinkedListNode<System.Action> currentNode = handlers.First; currentNode != null; currentNode = currentNode.Next) {
				currentNode.Value();
			}
		}

		/// <summary>
		/// Called on Server when removin Authority from a client.
		/// </summary>
		public virtual void OnRemoveAuthority() {
			LinkedList<System.Action> handlers;
			if (!_registeredEventHandlers.TryGetValue(TinyNetComponentEvents.OnRemoveAuthority, out handlers)) {
				return;
			}

			for (LinkedListNode<System.Action> currentNode = handlers.First; currentNode != null; currentNode = currentNode.Next) {
				currentNode.Value();
			}
		}

		/// <summary>
		/// This is only called on a listen server, for spawn and hide messages. Objects being destroyed will trigger OnNetworkDestroy as normal.
		/// </summary>
		public virtual void OnGiveLocalVisibility() {
			LinkedList<System.Action> handlers;
			if (!_registeredEventHandlers.TryGetValue(TinyNetComponentEvents.OnGiveLocalVisibility, out handlers)) {
				return;
			}

			for (LinkedListNode<System.Action> currentNode = handlers.First; currentNode != null; currentNode = currentNode.Next) {
				currentNode.Value();
			}
		}


		/// <summary>
		/// This is only called on a listen server, for spawn and hide messages. Objects being destroyed will trigger OnNetworkDestroy as normal.
		/// </summary>
		public virtual void OnRemoveLocalVisibility() {
			LinkedList<System.Action> handlers;
			if (!_registeredEventHandlers.TryGetValue(TinyNetComponentEvents.OnRemoveLocalVisibility, out handlers)) {
				return;
			}

			for (LinkedListNode<System.Action> currentNode = handlers.First; currentNode != null; currentNode = currentNode.Next) {
				currentNode.Value();
			}
		}

		//Authority?

		// happens on client
		/// <summary>
		/// [Client only] Handles the client authority.
		/// </summary>
		/// <param name="authority">if set to <c>rue</c> [authority].</param>
		internal void HandleClientAuthority(bool authority) {
			if (!_localPlayerAuthority) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("HandleClientAuthority " + gameObject + " does not have localPlayerAuthority"); }

				return;
			}

			ForceAuthority(authority);
		}

		/// <summary>
		/// [Server only] Removes the client authority.
		/// </summary>
		/// <param name="conn">The connection.</param>
		/// <returns><c>false</c> on error, true otherwise.</returns>
		public bool RemoveClientAuthority(TinyNetConnection conn) {
			if (!isServer) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("RemoveClientAuthority can only be called on the server for spawned objects."); }
				return false;
			}

			if (_connectionToOwnerClient == null) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("RemoveClientAuthority for " + gameObject + " has no clientAuthority owner."); }
				return false;
			}

			if (_connectionToOwnerClient != conn) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("RemoveClientAuthority for " + gameObject + " has different owner."); }
				return false;
			}

			_connectionToOwnerClient.RemoveOwnedObject(this);
			_connectionToOwnerClient = null;

			// server now has authority (this is only called on server)
			ForceAuthority(true);

			OnRemoveAuthority();

			// send msg to that client
			var msg = new TinyNetClientAuthorityMessage();
			msg.networkID = TinyInstanceID.NetworkID;
			msg.authority = false;
			TinyNetServer.instance.SendMessageByChannelToTargetConnection(msg, LiteNetLib.DeliveryMethod.ReliableOrdered, conn);

			//Saishy: Still don't have an authority callback
			/*if (clientAuthorityCallback != null) {
				clientAuthorityCallback(conn, this, false);
			}*/
			return true;
		}

		/// <summary>
		/// [Server only] Assigns the client authority.
		/// </summary>
		/// <param name="conn">The connection.</param>
		/// <returns><c>false</c> on error, true otherwise.</returns>
		public bool AssignClientAuthority(TinyNetConnection conn) {
			if (!isServer) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("AssignClientAuthority can only be call on the server for spawned objects."); }
				return false;
			}
			if (!_localPlayerAuthority) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("AssignClientAuthority can only be used for NetworkIdentity component with LocalPlayerAuthority set."); }
				return false;
			}

			if (_connectionToOwnerClient != null && conn != _connectionToOwnerClient) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("AssignClientAuthority for " + gameObject + " already has an owner. Use RemoveClientAuthority() first."); }
				return false;
			}

			if (conn == null) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("AssignClientAuthority for " + gameObject + " owner cannot be null. Use RemoveClientAuthority() instead."); }
				return false;
			}

			_connectionToOwnerClient = conn;
			_connectionToOwnerClient.AddOwnedObject(this);

			// server no longer has authority (this is called on server). Note that local client could re-acquire authority below
			ForceAuthority(false);

			OnGiveAuthority();

			// send msg to that client
			var msg = new TinyNetClientAuthorityMessage();
			msg.networkID = TinyInstanceID.NetworkID;
			msg.authority = true;
			TinyNetServer.instance.SendMessageByChannelToTargetConnection(msg, LiteNetLib.DeliveryMethod.ReliableOrdered, conn);

			//Saishy: Still don't have an authority callback
			/*if (clientAuthorityCallback != null) {
				clientAuthorityCallback(conn, this, true);
			}*/
			return true;
		}
	}
}
