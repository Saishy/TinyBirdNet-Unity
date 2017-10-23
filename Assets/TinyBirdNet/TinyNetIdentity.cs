using UnityEngine;
using System.Collections;
using TinyBirdUtils;
using UnityEditor;

namespace TinyBirdNet {

	/// <summary>
	/// Any GameObject that contains this component can be Spawned accross the network.
	/// </summary>
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	[AddComponentMenu("TinyBirdNet/TinyNetIdentity")]
	public class TinyNetIdentity : MonoBehaviour, ITinyNetInstanceID {

		public uint NetworkID { get; protected set; }

		[SerializeField] bool _serverOnly;
		[SerializeField] bool _localPlayerAuthority;
		[SerializeField] string _assetGUID;

		bool _hasAuthority;

		public bool isServer { get { return TinyNetGameManager.instance.isServer; } }
		public bool isClient { get { return TinyNetGameManager.instance.isClient; } }

		public bool hasAuthority { get { return _hasAuthority; } }

		public string assetId {
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

		public void ReceiveNetworkID(ushort newID) {
			NetworkID = newID;
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

			if (TinyNetLogLevel.logDev) { TinyLogger.Log("OnStartServer " + gameObject + " netId:" + NetworkID); }
		}

		/// <summary>
		/// Called when an object is spawned on the client.
		/// </summary>
		public void OnStartClient() {
			if (TinyNetLogLevel.logDev) { TinyLogger.Log("OnStartClient " + gameObject + " netId:" + NetworkID + " localPlayerAuthority: " + _localPlayerAuthority); }
		}
	}
}