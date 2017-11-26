using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine.SceneManagement;
using TinyBirdNet.Messaging;
using TinyBirdUtils;

namespace TinyBirdNet {

	public class TinyNetGameManager : MonoBehaviour {

		public static TinyNetGameManager instance;

		/// <summary>
		/// Current scene name at runtime.
		/// </summary>
		static public string networkSceneName = "";

		/// <summary>
		///  Stores the scene changing async operation, used to check if a scene loading was finished.
		/// </summary>
		static AsyncOperation s_LoadingSceneAsync;

		[SerializeField] List<GameObject> registeredPrefabs;

		[SerializeField] List<string> prefabsGUID;

		/**<summary>int is the asset index in TinyNetGameManager</summary>*/
		protected Dictionary<int, SpawnDelegate> _spawnHandlers = new Dictionary<int, SpawnDelegate>();
		/**<summary>int is the asset index in TinyNetGameManager</summary>*/
		protected Dictionary<int, UnSpawnDelegate> _unspawnHandlers = new Dictionary<int, UnSpawnDelegate>();

		public LogFilter currentLogFilter = LogFilter.Info;

		[SerializeField] protected int maxNumberOfPlayers = 4;
		protected int port = 7777;
		protected int pingInterval = 1000;

		public bool bNatPunchEnabled { get; protected set; }

		public int MaxNumberOfPlayers { get { return maxNumberOfPlayers; } }
		public int Port { get { return port; } }
		public int PingInterval {
			get { return pingInterval; }
			set {
				if (serverManager != null) {
					serverManager.SetPingInterval(value);
				}
				if (clientManager != null) {
					clientManager.SetPingInterval(value);
				}

				pingInterval = value;
			}
		}

		protected TinyNetServer serverManager;
		protected TinyNetClient clientManager;

		public bool isServer { get { return serverManager != null && serverManager.isRunning; } }
		public bool isClient { get { return clientManager != null && clientManager.isRunning; } }
		public bool isListenServer { get { return isServer && isClient; } }
		//public bool isStandalone { get; protected set; }

		private int _nextNetworkID = 0;
		public int NextNetworkID {
			get {
				return ++_nextNetworkID;
			}
		}

		void Awake() {
			instance = this;

			TinyNetLogLevel.currentLevel = currentLogFilter;

			TinyNetReflector.GetAllSyncVarProps();

			//serverManager = new TinyNetServer();
			//clientManager = new TinyNetClient();

			AwakeVirtual();
		}

		/** <summary>Please override this function to use the Awake call.</summary> */
		protected virtual void AwakeVirtual() { }

		void Start() {
			StartVirtual();
		}

		/** <summary>Please override this function to use the Start call.</summary> */
		protected virtual void StartVirtual() {
			StartCoroutine(TinyNetUpdate());
		}

		void Update() {
			if (serverManager != null) {
				serverManager.InternalUpdate();
			}
			if (clientManager != null) {
				clientManager.InternalUpdate();
			}

			CheckForSceneLoad();

			UpdateVirtual();
		}

		/// <summary>
		/// Please override this function to use the Update call.
		/// </summary>
		protected virtual void UpdateVirtual() {
		}

		IEnumerator TinyNetUpdate() {
			while (true) {
				if (serverManager != null) {
					serverManager.TinyNetUpdate();
				}
				if (clientManager != null) {
					clientManager.TinyNetUpdate();
				}

				yield return null;
			}
		}

		void OnDestroy() {
			ClearNetManager();
		}

		//============ Assets Methods =======================//

		public int GetAssetIdFromPrefab(GameObject prefab) {
			return registeredPrefabs.IndexOf(prefab);
		}

		public int GetAssetIdFromAssetGUID(string assetGUID) {
			return prefabsGUID.IndexOf(assetGUID);
		}

		public string GetAssetGUIDFromAssetId(int assetId) {
			return prefabsGUID[assetId];
		}

		public GameObject GetPrefabFromAssetId(int assetId) {
			return registeredPrefabs[assetId];
		}

		public GameObject GetPrefabFromAssetGUID(string assetGUID) {
			return registeredPrefabs[prefabsGUID.IndexOf(assetGUID)];
		}

		public int GetAmountOfRegisteredAssets() {
			return registeredPrefabs.Count;
		}

		void MakeListOfPrefabsGUID() {
			prefabsGUID = new List<string>(registeredPrefabs.Count);

			for (int i = 0; i < registeredPrefabs.Count; i++) {
				prefabsGUID.Add(registeredPrefabs[i].GetComponent<TinyNetIdentity>().assetGUID);
			}
		}

		/*public Dictionary<string, GameObject> GetDictionaryOfAssetGUIDToPrefabs() {
			Dictionary<string, GameObject> result = new Dictionary<string, GameObject>(registeredPrefabs.Count);

			for (int i = 0; i < registeredPrefabs.Count; i++) {
				result.Add(registeredPrefabs[i].GetComponent<TinyNetIdentity>().assetGUID, registeredPrefabs[i]);
			}

			return result;
		}*/

		/// <summary>
		/// Receives a new list of registered prefabs from the custom Editor.
		/// </summary>
		public void RebuildAllRegisteredPrefabs(GameObject[] newArray) {
			registeredPrefabs = new List<GameObject>(newArray);
		}

		public void UnregisterSpawnHandler(int assetIndex) {
			_spawnHandlers.Remove(assetIndex);
			_unspawnHandlers.Remove(assetIndex);
		}

		public void RegisterSpawnHandler(int assetIndex, SpawnDelegate spawnHandler, UnSpawnDelegate unspawnHandler) {
			if (spawnHandler == null || unspawnHandler == null) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("RegisterSpawnHandler custom spawn function null for " + assetIndex); }
				return;
			}

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("RegisterSpawnHandler asset '" + assetIndex + "' " + spawnHandler.Method.Name + "/" + unspawnHandler.Method.Name); }

			_spawnHandlers[assetIndex] = spawnHandler;
			_unspawnHandlers[assetIndex] = unspawnHandler;
		}

		public bool GetSpawnHandler(string assetGUID, out SpawnDelegate handler) {
			return GetSpawnHandler(GetAssetIdFromAssetGUID(assetGUID), out handler);
		}

		public bool GetSpawnHandler(int assetIndex, out SpawnDelegate handler) {
			if (_spawnHandlers.ContainsKey(assetIndex)) {
				handler = _spawnHandlers[assetIndex];
				return true;
			}
			handler = null;
			return false;
		}

		public bool InvokeUnSpawnHandler(string assetGUID, GameObject obj) {
			return InvokeUnSpawnHandler(GetAssetIdFromAssetGUID(assetGUID), obj);
		}

		public bool InvokeUnSpawnHandler(int assetIndex, GameObject obj) {
			if (_unspawnHandlers.ContainsKey(assetIndex) && _unspawnHandlers[assetIndex] != null) {
				UnSpawnDelegate handler = _unspawnHandlers[assetIndex];
				handler(obj);
				return true;
			}
			return false;
		}

		//============ Net Management =======================//

		protected virtual void ClearNetManager() {
			if (serverManager != null) {
				serverManager.ClearNetManager();
			}

			if (clientManager != null) {
				clientManager.ClearNetManager();
			}
		}

		/** <summary>Changes the current max amount of players, this only has an effect before starting a Server.</summary> */
		public virtual void SetMaxNumberOfPlayers(int newNumber) {
			if (serverManager != null) {
				return;
			}
			maxNumberOfPlayers = newNumber;
		}

		/** <summary>Changes the port that will be used for hosting, this only has an effect before starting a Server.</summary> */
		public virtual void SetPort(int newPort) {
			if (serverManager != null) {
				return;
			}
			port = newPort;
		}

		public virtual void ToggleNatPunching(bool bNewState) {
			bNatPunchEnabled = bNewState;
		}

		/// <summary>
		/// Prepares this game to work as a server.
		/// </summary>
		public virtual void StartServer() {
			serverManager = new TinyNetServer();

			serverManager.StartServer(port, maxNumberOfPlayers);
		}

		/// <summary>
		/// Prepares this game to work as a client.
		/// </summary>
		public virtual void StartClient() {
			clientManager = new TinyNetClient();

			clientManager.StartClient();
		}

		/// <summary>
		/// Attempts to connect to the target server, StartClient() must have been called before.
		/// </summary>
		/// <param name="hostAddress">An IPv4 or IPv6 string containing the address of the server.</param>
		/// <param name="hostPort">An int representing the port to use for the connection.</param>
		public virtual void ClientConnectTo(string hostAddress, int hostPort) {
			clientManager.ClientConnectTo(hostAddress, hostPort);
		}

		//============ Scenes Methods =======================//

		/// <summary>
		/// Checks if a scene load was requested and if it finished.
		/// </summary>
		protected virtual void CheckForSceneLoad() {
			if (instance == null)
				return;

			if (s_LoadingSceneAsync == null)
				return;

			if (!s_LoadingSceneAsync.isDone)
				return;

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("CheckForSceneLoad done readyCon: " + clientManager.tinyNetConns[0]); }

			FinishLoadScene();
			s_LoadingSceneAsync.allowSceneActivation = true;
			s_LoadingSceneAsync = null;
		}

		/// <summary>
		/// Orders the Server to change the given scene.
		/// </summary>
		/// <param name="newSceneName">The name of the scene to change to.</param>
		public virtual void ServerChangeScene(string newSceneName) {
			if (string.IsNullOrEmpty(newSceneName)) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("ServerChangeScene empty scene name"); }
				return;
			}

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("ServerChangeScene " + newSceneName); }

			serverManager.SetAllClientsNotReady();
			networkSceneName = newSceneName;

			s_LoadingSceneAsync = SceneManager.LoadSceneAsync(newSceneName);

			TinyNetStringMessage msg = new TinyNetStringMessage(networkSceneName);
			msg.msgType = TinyNetMsgType.Scene;
			serverManager.SendMessageByChannelToAllConnections(msg, SendOptions.ReliableOrdered);
		}

		public virtual void ClientChangeScene(string newSceneName, bool forceReload) {
			if (string.IsNullOrEmpty(newSceneName)) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("ClientChangeScene empty scene name"); }
				return;
			}

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("ClientChangeScene newSceneName:" + newSceneName + " networkSceneName:" + networkSceneName); }


			if (newSceneName == networkSceneName) {
				if (!forceReload) {
					FinishLoadScene();
					return;
				}
			}

			s_LoadingSceneAsync = SceneManager.LoadSceneAsync(newSceneName);
			networkSceneName = newSceneName;
		}

		public virtual void FinishLoadScene() {
			if (isClient) {
				clientManager.ClientFinishLoadScene();
			} else {
				if (TinyNetLogLevel.logDev) { TinyLogger.Log("FinishLoadScene client is null"); }
			}

			if (isServer) {
				serverManager.SpawnAllObjects();
				serverManager.OnServerSceneChanged(networkSceneName);
			}

			if (isClient && clientManager.isConnected) {
				clientManager.OnClientSceneChanged();
			}
		}

		//============ Players Methods ======================//

		

		

		//============ Messages Handlers ====================//


	}
}
