using UnityEngine;
using System.Collections;
using TinyBirdUtils;
using UnityEditor;
using LiteNetLib.Utils;

namespace TinyBirdNet {

	/// <summary>
	/// Any GameObject that contains this component can be Spawned accross the network.
	/// </summary>
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	[AddComponentMenu("TinyBirdNet/TinyNetIdentity")]
	public class TinyNetIdentity : MonoBehaviour, ITinyNetInstanceID {

		public int NetworkID { get; protected set; }

		[SerializeField] bool _serverOnly;
		[SerializeField] bool _localPlayerAuthority;
		[SerializeField] string _assetGUID;
		[SerializeField] int _sceneID;

		ITinyNetObject[] _tinyNetObjects;

		bool _hasAuthority;

		public bool isServer { get { return TinyNetGameManager.instance.isServer; } }
		public bool isClient { get { return TinyNetGameManager.instance.isClient; } }

		public bool ServerOnly { get { return _serverOnly; } }

		public bool hasAuthority { get { return _hasAuthority; } }

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

		public virtual void OnNetworkCreate() {
			CacheTinyNetObjects();

			for (int i = 0; i < _tinyNetObjects.Length; i++) {
				TinyNetScene.AddTinyNetObjectToList(_tinyNetObjects[i]);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="bLocalClient">If true, means we are doing a NetworkDestroy on a client of a listen server.</param>
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
	}
}