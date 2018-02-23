using UnityEngine;
using System.Collections;
using TinyBirdUtils;
using LiteNetLib.Utils;
using TinyBirdNet.Messaging;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TinyBirdNet {

	/// <summary>
	/// Any <see cref="GameObject" /> that contains this component, can be spawned accross the network.
	/// <para>This is basically a container for an "universal id" accross the network.</para>
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
		public int NetworkID { get; protected set; }

		/// <summary>
		/// If true, this object will not be spawned on clients.
		/// </summary>
		[SerializeField] bool _serverOnly;
		/// <summary>
		/// If true, this object is owned by a client
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
		/// The list of <see cref="ITinyNetObject"/> components in this <see cref="GameObject"/>.
		/// </summary>
		ITinyNetObject[] _tinyNetObjects;

		//bool _bIsOwner;
		/// <summary>
		/// If this instance has authorithy
		/// </summary>
		bool _hasAuthority;

		//Saishy: Is it possible for a client to be the owner but not have authority? What would that imply?

		/// <summary>
		/// [Server only] Shortcut, prevents you to have to loop through all connections and objects to find owner.
		/// </summary>
		TinyNetConnection _ConnectionToOwnerClient;
		///<summary>Not implemented yet</summary>
		short _ownerPlayerId = -1;

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
		public bool hasAuthority { get { return _hasAuthority; } }
		//public bool hasOwnership { get { return _bIsOwner;  } }

		//public short playerControllerId { get { return _ownerPlayerId; } }
		/// <summary>
		/// [Server Only] Gets the connection to owner client.
		/// </summary>
		/// <value>
		/// The connection to owner client.
		/// </value>
		public TinyNetConnection connectionToOwnerClient { get { return _ConnectionToOwnerClient; } }

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
		public void ReceiveNetworkID(int newID) {
			NetworkID = newID;
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
			if (_hasAuthority == authority) {
				return;
			}

			_hasAuthority = authority;

			if (authority) {
				OnStartAuthority();
			} else {
				OnStopAuthority();
			}
		}

		/// <summary>
		/// Caches the <see cref="ITinyNetObject"/>
		/// </summary>
		void CacheTinyNetObjects() {
			if (_tinyNetObjects == null) {
				_tinyNetObjects = GetComponentsInChildren<ITinyNetObject>(true);
			}
		}

		/// <summary>
		/// Called on the server to serialize all <see cref="ITinyNetObject"/> attached to this prefab.
		/// </summary>
		/// <param name="writer"></param>
		public void SerializeAllTinyNetObjects(NetDataWriter writer) {
			for (int i = 0; i < _tinyNetObjects.Length; i++) {
				ITinyNetObject obj = _tinyNetObjects[i];
				obj.TinySerialize(writer, true);
			}
		}

		/// <summary>
		/// Deserializes all <see cref="ITinyNetObject"/> data.
		/// </summary>
		/// <param name="reader">The reader.</param>
		/// <param name="bInitialState">if set to <c>true</c> [b initial state].</param>
		public void DeserializeAllTinyNetObjects(NetDataReader reader, bool bInitialState) {
			if (bInitialState && _tinyNetObjects == null) {
				CacheTinyNetObjects();
			}

			for (int i = 0; i < _tinyNetObjects.Length; i++) {
				_tinyNetObjects[i].TinyDeserialize(reader, bInitialState);
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
		/// Used by the server to have a shortcut in the case a client owns this object.
		/// <para>Not implemmented yet.</para>
		/// </summary>
		/// <param name="conn">The connection that owns this object.</param>
		/// <param name="newPlayerControllerId">The player controller identifier that owns this object.</param>
		public void SetConnectionToClient(TinyNetConnection conn, short newPlayerControllerId) {
			_ownerPlayerId = newPlayerControllerId;
			_ConnectionToOwnerClient = conn;
		}


		/// <summary>
		/// Called when this object is created.
		/// </summary>
		public virtual void OnNetworkCreate() {
			CacheTinyNetObjects();

			for (int i = 0; i < _tinyNetObjects.Length; i++) {
				_tinyNetObjects[i].OnNetworkCreate();
			}
		}

		/// <summary>
		/// Called when destroyed by the network.
		/// </summary>
		public virtual void OnNetworkDestroy() {
			for (int i = 0; i < _tinyNetObjects.Length; i++) {
				TinyNetScene.RemoveTinyNetObjectFromList(_tinyNetObjects[i]);
				_tinyNetObjects[i].OnNetworkDestroy();
			}
		}

		/// <summary>
		/// Called when an object is spawned on the server.
		/// </summary>
		/// <param name="allowNonZeroNetId">If the object already have a NetworkId, it was probably recycled.</param>
		public void OnStartServer(bool allowNonZeroNetId) {
			if (_localPlayerAuthority) {
				// local player on server has NO authority
				_hasAuthority = false;
			} else {
				// enemy on server has authority
				_hasAuthority = true;
			}

			// If the instance/net ID is invalid here then this is an object instantiated from a prefab and the server should assign a valid ID
			if (NetworkID == 0) {
				NetworkID = TinyNetGameManager.instance.NextNetworkID;

				for (int i = 0; i < _tinyNetObjects.Length; i++) {
					_tinyNetObjects[i].ReceiveNetworkID(TinyNetGameManager.instance.NextNetworkID);
				}
			} else {
				if (allowNonZeroNetId) {
					//allowed
				} else {
					if (TinyNetLogLevel.logError) { TinyLogger.LogError("Object has non-zero netId " + NetworkID + " for " + gameObject); }
					return;
				}
			}

			for (int i = 0; i < _tinyNetObjects.Length; i++) {
				TinyNetScene.AddTinyNetObjectToList(_tinyNetObjects[i]);
				_tinyNetObjects[i].OnStartServer();
			}

			if (TinyNetLogLevel.logDev) { TinyLogger.Log("OnStartServer " + gameObject + " netId:" + NetworkID); }

			if (_hasAuthority) {
				OnStartAuthority();
			}
		}

		/// <summary>
		/// Called when an object is spawned on the client.
		/// </summary>
		public void OnStartClient() {
			if (bStartClientTwiceTest) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("OnStartClient CALLED TWICE FOR: " + gameObject + " netId:" + NetworkID + " localPlayerAuthority: " + _localPlayerAuthority); }
			} else {
				bStartClientTwiceTest = true;
			}

			for (int i = 0; i < _tinyNetObjects.Length; i++) {
				if (!isServer) {
					TinyNetScene.AddTinyNetObjectToList(_tinyNetObjects[i]);
				}
				_tinyNetObjects[i].OnStartClient();
			}

			if (TinyNetLogLevel.logDev) { TinyLogger.Log("OnStartClient " + gameObject + " netId:" + NetworkID + " localPlayerAuthority: " + _localPlayerAuthority); }
		}

		/// <summary>
		/// Called when [start local player].
		/// </summary>
		public virtual void OnStartLocalPlayer() {
			for (int i = 0; i < _tinyNetObjects.Length; i++) {
				_tinyNetObjects[i].OnStartLocalPlayer();
			}
		}

		/// <summary>
		/// Called when [start authority].
		/// </summary>
		public virtual void OnStartAuthority() {
			for (int i = 0; i < _tinyNetObjects.Length; i++) {
				_tinyNetObjects[i].OnStartAuthority();
			}
		}

		/// <summary>
		/// Called when [stop authority].
		/// </summary>
		public virtual void OnStopAuthority() {
			for (int i = 0; i < _tinyNetObjects.Length; i++) {
				_tinyNetObjects[i].OnStopAuthority();
			}
		}

		/// <summary>
		/// Called when [give authority].
		/// </summary>
		public virtual void OnGiveAuthority() {
			for (int i = 0; i < _tinyNetObjects.Length; i++) {
				_tinyNetObjects[i].OnGiveAuthority();
			}
		}

		/// <summary>
		/// Called when [remove authority].
		/// </summary>
		public virtual void OnRemoveAuthority() {
			for (int i = 0; i < _tinyNetObjects.Length; i++) {
				_tinyNetObjects[i].OnRemoveAuthority();
			}
		}

		/// <summary>
		/// Called when [set local visibility].
		/// </summary>
		/// <param name="vis">if set to <c>true</c> [vis].</param>
		public virtual void OnSetLocalVisibility(bool vis) {
			for (int i = 0; i < _tinyNetObjects.Length; i++) {
				_tinyNetObjects[i].OnSetLocalVisibility(vis);
			}
		}

		//Authority?

		// happens on client
		/// <summary>
		/// [Client only] Handles the client authority.
		/// </summary>
		/// <param name="authority">if set to <c>true</c> [authority].</param>
		internal void HandleClientAuthority(bool authority) {
			if (!_localPlayerAuthority) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("HandleClientAuthority " + gameObject + " does not have localPlayerAuthority"); }

				return;
			}

			ForceAuthority(authority);
		}

		/// <summary>
		/// [Server only] Removes the client authority.
		/// <para>Not implemmented yet.</para>
		/// </summary>
		/// <param name="conn">The connection.</param>
		/// <returns></returns>
		public bool RemoveClientAuthority(TinyNetConnection conn) {
			if (!isServer) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("RemoveClientAuthority can only be called on the server for spawned objects."); }
				return false;
			}

			if (_ConnectionToOwnerClient == null) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("RemoveClientAuthority for " + gameObject + " has no clientAuthority owner."); }
				return false;
			}

			if (_ConnectionToOwnerClient != conn) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("RemoveClientAuthority for " + gameObject + " has different owner."); }
				return false;
			}

			_ConnectionToOwnerClient.RemoveOwnedObject(this);
			_ConnectionToOwnerClient = null;

			// server now has authority (this is only called on server)
			ForceAuthority(true);

			OnRemoveAuthority();

			// send msg to that client
			var msg = new TinyNetClientAuthorityMessage();
			msg.networkID = NetworkID;
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
		/// <returns></returns>
		public bool AssignClientAuthority(TinyNetConnection conn) {
			if (!isServer) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("AssignClientAuthority can only be call on the server for spawned objects."); }
				return false;
			}
			if (!_localPlayerAuthority) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("AssignClientAuthority can only be used for NetworkIdentity component with LocalPlayerAuthority set."); }
				return false;
			}

			if (_ConnectionToOwnerClient != null && conn != _ConnectionToOwnerClient) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("AssignClientAuthority for " + gameObject + " already has an owner. Use RemoveClientAuthority() first."); }
				return false;
			}

			if (conn == null) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("AssignClientAuthority for " + gameObject + " owner cannot be null. Use RemoveClientAuthority() instead."); }
				return false;
			}

			_ConnectionToOwnerClient = conn;
			_ConnectionToOwnerClient.AddOwnedObject(this);

			// server no longer has authority (this is called on server). Note that local client could re-acquire authority below
			ForceAuthority(false);

			OnGiveAuthority();

			// send msg to that client
			var msg = new TinyNetClientAuthorityMessage();
			msg.networkID = NetworkID;
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
