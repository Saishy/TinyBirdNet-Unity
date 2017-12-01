using UnityEngine;
using System.Collections;
using TinyBirdUtils;
using UnityEditor;
using LiteNetLib.Utils;
using TinyBirdNet.Messaging;

namespace TinyBirdNet {

	/// <summary>
	/// Any GameObject that contains this component can be spawned accross the network.
	/// <para>This is basically a container for an "universal id" accross the network.</para>
	/// </summary>
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	[AddComponentMenu("TinyBirdNet/TinyNetIdentity")]
	public class TinyNetIdentity : MonoBehaviour, ITinyNetInstanceID {

		public int NetworkID { get; protected set; }

		[SerializeField] bool _serverOnly;
		/**<summary>If true, this object is owned by a client</summary>*/
		[SerializeField] bool _localPlayerAuthority;
		[SerializeField] string _assetGUID;
		[SerializeField] int _sceneID;

		ITinyNetObject[] _tinyNetObjects;

		//bool _bIsOwner;
		bool _hasAuthority;

		//Saishy: Is it possible for a client to be the owner but not have authority? What would that imply?

		//Server shortcuts, prevents you to have to loop through all connections and objects to find owner.
		TinyNetConnection _ConnectionToOwnerClient;
		short _ownerPlayerId = -1;

		public bool isServer { get { return TinyNetGameManager.instance.isServer; } }
		public bool isClient { get { return TinyNetGameManager.instance.isClient; } }

		public bool ServerOnly { get { return _serverOnly; } }

		//
		public bool hasAuthority { get { return _hasAuthority; } }
		//public bool hasOwnership { get { return _bIsOwner;  } }

		public short playerControllerId { get { return _ownerPlayerId; } }
		public TinyNetConnection connectionToOwnerClient { get { return _ConnectionToOwnerClient; } }

		public int sceneID { get { return _sceneID; } }

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

		public void ReceiveNetworkID(int newID) {
			NetworkID = newID;
		}

		// only used when fixing duplicate scene IDs duing post-processing
		public void ForceSceneId(int newSceneId) {
			_sceneID = newSceneId;
		}

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

		void CacheTinyNetObjects() {
			if (_tinyNetObjects == null) {
				_tinyNetObjects = GetComponentsInChildren<ITinyNetObject>(true);
			}
		}

		/// <summary>
		/// Called on the server to serialize all ITinyNetObjects attached to this prefab.
		/// </summary>
		/// <param name="writer"></param>
		public void SerializeAllTinyNetObjects(NetDataWriter writer) {
			for (int i = 0; i < _tinyNetObjects.Length; i++) {
				ITinyNetObject obj = _tinyNetObjects[i];
				obj.TinySerialize(writer, true);
			}
		}

		public void DeserializeAllTinyNetObjects(NetDataReader reader, bool bInitialState) {
			if (bInitialState && _tinyNetObjects == null) {
				CacheTinyNetObjects();
			}

			for (int i = 0; i < _tinyNetObjects.Length; i++) {
				_tinyNetObjects[i].TinyDeserialize(reader, bInitialState);
			}
		}

#if UNITY_EDITOR
		bool IsValidAssetGUI(string assetGUID) {
			string test = AssetDatabase.GetAssetPath(gameObject);
			test = AssetDatabase.AssetPathToGUID(test);

			return test.Equals(assetGUID);
		}

		bool IsPrefab() {
			PrefabType prefabType = PrefabUtility.GetPrefabType(gameObject);
			if (prefabType == PrefabType.Prefab)
				return true;
			return false;
		}

		void SetAssetGUID(string newGUID) {
			_assetGUID = newGUID;
		}

		void SetAssetGUID(GameObject prefab) {
			string path = AssetDatabase.GetAssetPath(prefab);
			_assetGUID = AssetDatabase.AssetPathToGUID(path);
		}

		void OnValidate() {
			if (_serverOnly && _localPlayerAuthority) {
				TinyLogger.LogWarning("Disabling Local Player Authority for " + gameObject + " because it is server-only.");
				_localPlayerAuthority = false;
			}

			SetupIDs();
		}

		void SetupIDs() {
			if (IsPrefab()) {
				SetAssetGUID(gameObject);
			}
		}
#endif
		public void SetDynamicAssetGUID(string newAssetGUID) {
			if (!IsValidAssetGUI(_assetGUID) || _assetGUID.Equals(newAssetGUID)) {
				_assetGUID = newAssetGUID;
			} else {
				if (TinyNetLogLevel.logWarn) { TinyLogger.LogWarning("SetDynamicAssetId object already has an assetId <" + _assetGUID + ">"); }
			}
		}

		/// <summary>
		/// Used by the server to have a shortcut in the case a client owns this connection.
		/// </summary>
		/// <param name="conn"></param>
		/// <param name="newPlayerControllerId"></param>
		public void SetConnectionToClient(TinyNetConnection conn, short newPlayerControllerId) {
			_ownerPlayerId = newPlayerControllerId;
			_ConnectionToOwnerClient = conn;
		}

		public virtual void OnNetworkCreate() {
			CacheTinyNetObjects();

			for (int i = 0; i < _tinyNetObjects.Length; i++) {
				TinyNetScene.AddTinyNetObjectToList(_tinyNetObjects[i]);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public virtual void OnNetworkDestroy() {
			for (int i = 0; i < _tinyNetObjects.Length; i++) {
				TinyNetScene.RemoveTinyNetObjectFromList(_tinyNetObjects[i]);
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
			} else {
				if (allowNonZeroNetId) {
					//allowed
				} else {
					if (TinyNetLogLevel.logError) { TinyLogger.LogError("Object has non-zero netId " + NetworkID + " for " + gameObject); }
					return;
				}
			}

			for (int i = 0; i < _tinyNetObjects.Length; i++) {
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
			for (int i = 0; i < _tinyNetObjects.Length; i++) {
				_tinyNetObjects[i].OnStartClient();
			}

			if (TinyNetLogLevel.logDev) { TinyLogger.Log("OnStartClient " + gameObject + " netId:" + NetworkID + " localPlayerAuthority: " + _localPlayerAuthority); }
		}

		public virtual void OnStartLocalPlayer() {
			for (int i = 0; i < _tinyNetObjects.Length; i++) {
				_tinyNetObjects[i].OnStartLocalPlayer();
			}
		}

		public virtual void OnStartAuthority() {
			for (int i = 0; i < _tinyNetObjects.Length; i++) {
				_tinyNetObjects[i].OnStartAuthority();
			}
		}

		public virtual void OnStopAuthority() {
			for (int i = 0; i < _tinyNetObjects.Length; i++) {
				_tinyNetObjects[i].OnStopAuthority();
			}
		}

		public virtual void OnSetLocalVisibility(bool vis) {
			for (int i = 0; i < _tinyNetObjects.Length; i++) {
				_tinyNetObjects[i].OnSetLocalVisibility(vis);
			}
		}

		//Authority?

		// happens on client
		internal void HandleClientAuthority(bool authority) {
			if (!_localPlayerAuthority) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("HandleClientAuthority " + gameObject + " does not have localPlayerAuthority"); }

				return;
			}

			ForceAuthority(authority);
		}

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

			// send msg to that client
			var msg = new TinyNetClientAuthorityMessage();
			msg.networkID = NetworkID;
			msg.authority = false;
			TinyNetServer.instance.SendMessageByChannelToTargetConnection(msg, LiteNetLib.SendOptions.ReliableOrdered, conn);

			//Saishy: Still don't have an authority callback
			/*if (clientAuthorityCallback != null) {
				clientAuthorityCallback(conn, this, false);
			}*/
			return true;
		}

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

			// send msg to that client
			var msg = new TinyNetClientAuthorityMessage();
			msg.networkID = NetworkID;
			msg.authority = true;
			TinyNetServer.instance.SendMessageByChannelToTargetConnection(msg, LiteNetLib.SendOptions.ReliableOrdered, conn);

			//Saishy: Still don't have an authority callback
			/*if (clientAuthorityCallback != null) {
				clientAuthorityCallback(conn, this, true);
			}*/
			return true;
		}
	}
}
