using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

namespace TinyBirdNet {

	/// <summary>
	/// Custom inspector for the <see cref="TinyNetGameManager"/> class.
	/// </summary>
	/// <seealso cref="UnityEditor.Editor" />
	[CustomEditor(typeof(TinyNetGameManager), true)]
	public class TinyNetGameManagerEditor : Editor {

		protected virtual bool HasCustomInspector { get { return false; } }

		static GUIContent targetFPSButton = new GUIContent("Change Physics FPS to 60", "This will change your fixed timestep to 0.01666667. If you don't know what you are doing, click here!");
		static GUIContent connectKeyLabel = new GUIContent("Connect Key:", "Insert here a unique key per version of your game, if the key mismatches the player will be denied connection.");
		static GUIContent networkFramesLabel = new GUIContent("Update Network every X Fixed Frames", "This will change how often the server sends the current state of the game to clients.");
		static GUIContent logFilterLabel = new GUIContent("LogFilter:", "Your console will only display logs of this level or higher.");

		SerializedProperty _registeredPrefabs;
		SerializedProperty _maxNumberOfPlayers;
		SerializedProperty _networkEveryXFixedFrames;
		SerializedProperty _multiplayerConnectKey;
		SerializedProperty _currentLogFilter;

		void OnEnable() {
			_registeredPrefabs = serializedObject.FindProperty("registeredPrefabs");

			_maxNumberOfPlayers = serializedObject.FindProperty("maxNumberOfPlayers");

			_networkEveryXFixedFrames = serializedObject.FindProperty("NetworkEveryXFixedFrames");

			_multiplayerConnectKey = serializedObject.FindProperty("multiplayerConnectKey");

			_currentLogFilter = serializedObject.FindProperty("currentLogFilter");
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();

			TinyNetGameManager netGameManager = target as TinyNetGameManager;

			if (GUILayout.Button(targetFPSButton)) {
				ChangeGameTargetFPSTo60();
			}

			//EditorGUILayout.LabelField("Update Network every X Fixed Frames");
			EditorGUILayout.LabelField(networkFramesLabel);
			EditorGUILayout.PropertyField(_networkEveryXFixedFrames, GUIContent.none);
			//netGameManager.NetworkEveryXFixedFrames = EditorGUILayout.IntSlider(netGameManager.NetworkEveryXFixedFrames, 1, 60);

			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(_maxNumberOfPlayers);

			EditorGUILayout.PropertyField(_multiplayerConnectKey, connectKeyLabel);
			//netGameManager.multiplayerConnectKey = EditorGUILayout.TextField(connectKeyLabel, netGameManager.multiplayerConnectKey);

			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(_registeredPrefabs, true);

			if (GUILayout.Button("Register all TinyNetIdentity prefabs")) {
				netGameManager.RebuildAllRegisteredPrefabs(GetAllAssetsWithTinyNetIdentity());
			}

			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(_currentLogFilter, logFilterLabel);
			//netGameManager.currentLogFilter = (LogFilter)EditorGUILayout.EnumPopup("LogFilter:", netGameManager.currentLogFilter);

			if (netGameManager.ShowDebugStats) {
				if (GUILayout.Button("Hide Stats Overlay")) {
					netGameManager.ShowDebugStats = false;
				}
			} else {
				if (GUILayout.Button("Show Stats Overlay")) {
					netGameManager.ShowDebugStats = true;
				}
			}

			EditorGUILayout.Space();
			//EditorGUILayout.LabelField("Child fields");

			FieldInfo[] childFields = target.GetType().GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			if (!HasCustomInspector) {
				foreach (FieldInfo field in childFields) {
					//if(field.IsNotSerialized || field.IsStatic)
					//{
					//    continue;
					//}

					if (field.IsPublic || field.GetCustomAttribute(typeof(SerializeField)) != null) {

						EditorGUILayout.PropertyField(serializedObject.FindProperty(field.Name));
					}
				}
			}

			serializedObject.ApplyModifiedProperties();
		}

		GameObject[] GetAllAssetsWithTinyNetIdentity() {
			List<GameObject> result = new List<GameObject>();

			string[] guids = AssetDatabase.FindAssets("t:GameObject", null);

			for (int i = 0; i < guids.Length; i++) {
				GameObject gObj = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[i]));

				if (gObj.GetComponent<TinyNetIdentity>() != null) {
					result.Add(gObj);
				}
			}

			return result.ToArray();
		}

		void ChangeGameTargetFPSTo60() {
			Time.fixedDeltaTime = 0.01666667f;
			Time.maximumDeltaTime = 0.01666667f * 5;
			Time.maximumParticleDeltaTime = 0.01666667f * 2;
		}
	}
}
