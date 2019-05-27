using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine.SceneManagement;
using TinyBirdNet.Messaging;
using TinyBirdUtils;

namespace TinyBirdNet {

	/// <summary>
	/// This class manages and communicates with 
	/// </summary>
	/// <seealso cref="UnityEngine.MonoBehaviour" />
	public class TinyNetGameManager : MonoBehaviour {

		/// <summary>
		/// The singleton instance.
		/// </summary>
		public static TinyNetGameManager instance;

		public static readonly Guid ApplicationGUID = Guid.NewGuid();
		public static readonly string ApplicationGUIDString = ApplicationGUID.ToString();

		/// <summary>
		/// Insert here a unique key per version of your game, if the key mismatches the player will be denied connection.
		/// </summary>
		public string multiplayerConnectKey = "TinyBirdNet Default Key";

		/// <summary>
		/// The network state update will happen every x fixed frames.
		/// </summary>
		[Range(1, 60)]
		public int NetworkEveryXFixedFrames = 1;

		/// <summary>
		/// Current scene name at runtime.
		/// </summary>
		static public string networkSceneName = "";

		/// <summary>
		/// Stores the scene changing async operation, used to check if a scene loading was finished.
		/// </summary>
		static AsyncOperation s_LoadingSceneAsync;

		/// <summary>
		/// The prefabs registered to use networking.
		/// </summary>
		[SerializeField] List<GameObject> registeredPrefabs;

		/// <summary>
		/// The prefabs unique identifier.
		/// </summary>
		[SerializeField] List<string> prefabsGUID;

		/// <summary>
		/// The spawn handlers.
		/// <para><c>int</c> is the asset index in <see cref="registeredPrefabs"/>.</para>
		/// </summary>
		protected Dictionary<int, SpawnDelegate> _spawnHandlers = new Dictionary<int, SpawnDelegate>();
		/// <summary>
		/// The unspawn handlers.
		/// <para><c>int</c> is the asset index in <see cref="registeredPrefabs"/>.</para>
		/// </summary>
		protected Dictionary<int, UnSpawnDelegate> _unspawnHandlers = new Dictionary<int, UnSpawnDelegate>();

		/// <summary>
		/// The current log filter for <see cref="TinyLogger"/>.
		/// </summary>
		public LogFilter currentLogFilter = LogFilter.Info;

		/// <summary>
		/// The maximum number of players allowed in the network.
		/// </summary>
		[SerializeField] protected int maxNumberOfPlayers = 4;
		/// <summary>
		/// The port
		/// </summary>
		protected int port = 7777;
		/// <summary>
		/// The ping interval in ms.
		/// </summary>
		protected int pingInterval = 1000;

		/// <summary>
		/// Gets or sets a value indicating whether nat punch is enabled.
		/// <para>Needs custom implementation to work.</para>
		/// </summary>
		/// <value>
		///   <c>true</c> if nat punch is enabled; otherwise, <c>false</c>.
		/// </value>
		public bool bNatPunchEnabled { get; protected set; }

		/// <summary>
		/// Gets the maximum number of players.
		/// </summary>
		/// <value>
		/// The maximum number of players.
		/// </value>
		public int MaxNumberOfPlayers { get { return maxNumberOfPlayers; } }
		/// <summary>
		/// Gets the port.
		/// </summary>
		/// <value>
		/// The port.
		/// </value>
		public int Port { get { return port; } }
		/// <summary>
		/// Gets or sets the ping interval.
		/// </summary>
		/// <value>
		/// The ping interval.
		/// </value>
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

		/// <summary>
		/// The server scene manager.
		/// </summary>
		protected TinyNetServer serverManager;
		/// <summary>
		/// The client scene manager.
		/// </summary>
		protected TinyNetClient clientManager;

		/// <summary>
		/// Gets a value indicating whether this instance is server.
		/// </summary>
		/// <value>
		///   <c>true</c> if this instance is server; otherwise, <c>false</c>.
		/// </value>
		public bool isServer { get { return serverManager != null && serverManager.isRunning; } }
		/// <summary>
		/// Gets a value indicating whether this instance is client.
		/// </summary>
		/// <value>
		///   <c>true</c> if this instance is client; otherwise, <c>false</c>.
		/// </value>
		public bool isClient { get { return clientManager != null && clientManager.isRunning; } }
		/// <summary>
		/// Gets a value indicating whether this instance is a listen server.
		/// </summary>
		/// <value>
		///   <c>true</c> if this instance is a listen server; otherwise, <c>false</c>.
		/// </value>
		public bool isListenServer { get { return isServer && isClient; } }
		//public bool isStandalone { get; protected set; }

		/// <summary>
		/// The next network identifier
		/// </summary>
		private int _nextNetworkID = 0;
		/// <summary>
		/// Gets the next network identifier.
		/// </summary>
		/// <value>
		/// The next network identifier.
		/// </value>
		public TinyNetworkID NextNetworkID {
			get {
				return new TinyNetworkID(_nextNetworkID++);
			}
		}

		/// <summary>
		/// The next player identifier
		/// </summary>
		private int _nextPlayerID = 0;
		/// <summary>
		/// Gets the next player identifier.
		/// </summary>
		/// <value>
		/// The next player identifier.
		/// </value>
		public int NextPlayerID {
			get {
				return _nextPlayerID++;
			}
		}

		/// <summary>
		/// The current game tick, used to calculate the network state, buffer and reconciliation.
		/// </summary>
		private int _currentFixedFrame = 0;

		/// <summary>
		/// The current game tick, used to calculate the network state, buffer and reconciliation.
		/// </summary>
		public int CurrentGameTick {
			get {
				return _currentFixedFrame;
			}
		}

		/// <summary>
		/// Awake is run before Start and there is no guarantee anything else has been initialized. Called by UnityEngine.
		/// </summary>
		void Awake() {
			instance = this;

			TinyNetLogLevel.currentLevel = currentLogFilter;

			MakeListOfPrefabsGUID();

			TinyNetReflector.GetAllSyncVarProps();

			//serverManager = new TinyNetServer();
			//clientManager = new TinyNetClient();

			AwakeVirtual();
		}

		/// <summary>
		/// Provides a function to be overrrided in case you need to add something in the Awake call.
		/// </summary>
		protected virtual void AwakeVirtual() { }

		/// <summary>
		/// Starts this instance. Called by UnityEngine.
		/// </summary>
		void Start() {
			StartVirtual();
		}

		/// <summary>
		/// Provides a function to be overrrided in case you need to add something in the Start call.
		/// </summary>
		protected virtual void StartVirtual() {
			StartCoroutine(TinyNetUpdate());
		}

		/// <summary>
		/// Called every frame update by the UnityEngine.
		/// </summary>
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
		/// Provides a function to be overrrided in case you need to add something in the Update call.
		/// </summary>
		protected virtual void UpdateVirtual() {
		}

		/// <summary>
		/// A coroutine used to call a network update after every physics update from a fixed step from UnityEngine.
		/// </summary>
		IEnumerator TinyNetUpdate() {
			while (true) {
				if (serverManager != null) {
					serverManager.TinyNetUpdate();
				}
				if (clientManager != null) {
					clientManager.TinyNetUpdate();
				}

				yield return new WaitForFixedUpdate();
				_currentFixedFrame++;
			}
		}

		/// <summary>
		/// Called by UnityEngine when destroyed.
		/// </summary>
		void OnDestroy() {
			ClearNetManager();
		}

		//============ Assets Methods =======================//

		/// <summary>
		/// Gets the asset identifier from a prefab.
		/// </summary>
		/// <param name="prefab">The prefab.</param>
		/// <returns></returns>
		public int GetAssetIdFromPrefab(GameObject prefab) {
			return registeredPrefabs.IndexOf(prefab);
		}

		/// <summary>
		/// Gets the asset identifier from an asset unique identifier.
		/// <para>The GUID is provided by Unity, the id is generated by TinyBirdNet for easier network handling.</para>
		/// </summary>
		/// <param name="assetGUID">The asset unique identifier.</param>
		/// <returns></returns>
		public int GetAssetIdFromAssetGUID(string assetGUID) {
			return prefabsGUID.IndexOf(assetGUID);
		}

		/// <summary>
		/// Gets the asset unique identifier from an asset identifier.
		/// </summary>
		/// <param name="assetId">The asset identifier.</param>
		/// <returns></returns>
		public string GetAssetGUIDFromAssetId(int assetId) {
			return prefabsGUID[assetId];
		}

		/// <summary>
		/// Gets the prefab from an asset identifier.
		/// </summary>
		/// <param name="assetId">The asset identifier.</param>
		/// <returns></returns>
		public GameObject GetPrefabFromAssetId(int assetId) {
			if (registeredPrefabs.Count <= assetId || assetId < 0) {
				return null;
			}
			return registeredPrefabs[assetId];
		}

		/// <summary>
		/// Gets the prefab from an asset unique identifier.
		/// </summary>
		/// <param name="assetGUID">The asset unique identifier.</param>
		/// <returns></returns>
		public GameObject GetPrefabFromAssetGUID(string assetGUID) {
			return registeredPrefabs[prefabsGUID.IndexOf(assetGUID)];
		}

		/// <summary>
		/// Gets the amount of registered assets.
		/// </summary>
		/// <returns></returns>
		public int GetAmountOfRegisteredAssets() {
			return registeredPrefabs.Count;
		}

		/// <summary>
		/// Makes the list of prefabs unique identifier.
		/// </summary>
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

		/// <summary>
		/// Unregisters a spawn handler.
		/// </summary>
		/// <param name="assetIndex">Id of the asset.</param>
		public void UnregisterSpawnHandler(int assetIndex) {
			_spawnHandlers.Remove(assetIndex);
			_unspawnHandlers.Remove(assetIndex);
		}

		/// <summary>
		/// Registers a spawn handler.
		/// </summary>
		/// <param name="assetIndex">Id of the asset.</param>
		/// <param name="spawnHandler">The spawn handler.</param>
		/// <param name="unspawnHandler">The unspawn handler.</param>
		public void RegisterSpawnHandler(int assetIndex, SpawnDelegate spawnHandler, UnSpawnDelegate unspawnHandler) {
			if (spawnHandler == null || unspawnHandler == null) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("RegisterSpawnHandler custom spawn function null for " + assetIndex); }
				return;
			}

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("RegisterSpawnHandler asset '" + assetIndex + "' " + spawnHandler.Method.Name + "/" + unspawnHandler.Method.Name); }

			_spawnHandlers[assetIndex] = spawnHandler;
			_unspawnHandlers[assetIndex] = unspawnHandler;
		}

		/// <summary>
		/// Gets the spawn handler of an asset.
		/// </summary>
		/// <param name="assetGUID">The asset unique identifier.</param>
		/// <param name="handler">The handler.</param>
		/// <returns></returns>
		public bool GetSpawnHandler(string assetGUID, out SpawnDelegate handler) {
			return GetSpawnHandler(GetAssetIdFromAssetGUID(assetGUID), out handler);
		}

		/// <summary>
		/// Gets the spawn handler of an asset.
		/// </summary>
		/// <param name="assetIndex">Index of the asset.</param>
		/// <param name="handler">The handler.</param>
		/// <returns></returns>
		public bool GetSpawnHandler(int assetIndex, out SpawnDelegate handler) {
			if (_spawnHandlers.ContainsKey(assetIndex)) {
				handler = _spawnHandlers[assetIndex];
				return true;
			}
			handler = null;
			return false;
		}

		/// <summary>
		/// Invokes the unspawn handler of an asset.
		/// </summary>
		/// <param name="assetGUID">The asset unique identifier.</param>
		/// <param name="obj">The object.</param>
		/// <returns></returns>
		public bool InvokeUnSpawnHandler(string assetGUID, GameObject obj) {
			return InvokeUnSpawnHandler(GetAssetIdFromAssetGUID(assetGUID), obj);
		}

		/// <summary>
		/// Invokes the unspawn handler of an asset.
		/// </summary>
		/// <param name="assetIndex">Index of the asset.</param>
		/// <param name="obj">The object.</param>
		/// <returns></returns>
		public bool InvokeUnSpawnHandler(int assetIndex, GameObject obj) {
			if (_unspawnHandlers.ContainsKey(assetIndex) && _unspawnHandlers[assetIndex] != null) {
				UnSpawnDelegate handler = _unspawnHandlers[assetIndex];
				handler(obj);
				return true;
			}
			return false;
		}

		//============ Net Management =======================//

		/// <summary>
		/// Clears the net manager.
		/// </summary>
		protected virtual void ClearNetManager() {
			if (serverManager != null) {
				serverManager.ClearNetManager();
			}

			if (clientManager != null) {
				clientManager.ClearNetManager();
			}
		}

		/// <summary>
		/// >Changes the current max amount of players, this only has an effect before starting a Server.
		/// </summary>
		/// <param name="newNumber">The new number.</param>
		public virtual void SetMaxNumberOfPlayers(int newNumber) {
			if (serverManager != null) {
				return;
			}
			maxNumberOfPlayers = newNumber;
		}

		/// <summary>
		/// Changes the port that will be used for hosting, this only has an effect before starting a Server.
		/// </summary>
		/// <param name="newPort">The new port.</param>
		public virtual void SetPort(int newPort) {
			if (serverManager != null) {
				return;
			}
			port = newPort;
		}

		/// <summary>
		/// Toggles the nat punching.
		/// </summary>
		/// <param name="bNewState">if set to <c>true</c> [b new state].</param>
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
		/// Registers message handlers for the server.
		/// </summary>
		public virtual void RegisterMessageHandlersServer() {
		}

		/// <summary>
		/// Registers message handlers for the client.
		/// </summary>
		public virtual void RegisterMessageHandlersClient() {
		}

		/// <summary>
		/// Attempts to connect to the target server, StartClient() must have been called before.
		/// </summary>
		/// <param name="hostAddress">An IPv4 or IPv6 string containing the address of the server.</param>
		/// <param name="hostPort">An int representing the port to use for the connection.</param>
		public virtual void ClientConnectTo(string hostAddress, int hostPort) {
			clientManager.ClientConnectTo(hostAddress, hostPort);
		}

		//============ Server Methods =======================//

		/// <summary>
		/// Called when a client connect to the server.
		/// <para>Currently not implemented!</para>
		/// </summary>
		/// <param name="conn">The connection.</param>
		public void OnClientConnectToServer(TinyNetConnection conn) {

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

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("TinyNetGameManager::CheckForSceneLoad() done"); }

			FinishLoadScene();
			s_LoadingSceneAsync.allowSceneActivation = true;
			s_LoadingSceneAsync = null;
		}

		/// <summary>
		/// Orders the server to change to the given scene.
		/// </summary>
		/// <param name="newSceneName">The name of the scene to change to.</param>
		public virtual void ServerChangeScene(string newSceneName) {
			if (string.IsNullOrEmpty(newSceneName)) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("TinyNetGameManager::ServerChangeScene() empty scene name"); }
				return;
			}

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("TinyNetGameManager::ServerChangeScene() " + newSceneName); }

			serverManager.SetAllClientsNotReady();
			networkSceneName = newSceneName;

			s_LoadingSceneAsync = SceneManager.LoadSceneAsync(newSceneName);

			TinyNetStringMessage msg = new TinyNetStringMessage(networkSceneName);
			msg.msgType = TinyNetMsgType.Scene;
			serverManager.SendMessageByChannelToAllConnections(msg, DeliveryMethod.ReliableOrdered);
		}

		/// <summary>
		/// Orders the client to change to the given scene.
		/// </summary>
		/// <param name="newSceneName">Name of the new scene.</param>
		/// <param name="forceReload">if set to <c>true</c>, force reload.</param>
		public virtual void ClientChangeScene(string newSceneName, bool forceReload) {
			if (string.IsNullOrEmpty(newSceneName)) {
				if (TinyNetLogLevel.logError) { TinyLogger.LogError("TinyNetGameManager::ClientChangeScene() empty scene name"); }
				return;
			}

			if (TinyNetLogLevel.logDebug) { TinyLogger.Log("TinyNetGameManager::ClientChangeScene() newSceneName:" + newSceneName + " networkSceneName:" + networkSceneName); }


			if (newSceneName == networkSceneName) {
				if (!forceReload) {
					FinishLoadScene();
					return;
				}
			}

			s_LoadingSceneAsync = SceneManager.LoadSceneAsync(newSceneName);
			networkSceneName = newSceneName;
		}

		/// <summary>
		/// Called when a scene has finished loading.
		/// </summary>
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
