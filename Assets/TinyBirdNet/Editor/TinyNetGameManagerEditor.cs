using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace TinyBirdNet {

	[CustomEditor(typeof(TinyNetGameManager), true)]
	public class TinyNetGameManagerEditor : Editor {

		SerializedProperty _registeredPrefabs;
		SerializedProperty _maxNumberOfPlayers;

		void OnEnable() {
			_registeredPrefabs = serializedObject.FindProperty("registeredPrefabs");
			_maxNumberOfPlayers = serializedObject.FindProperty("maxNumberOfPlayers");
		}

		public override void OnInspectorGUI() {
			serializedObject.Update();

			TinyNetGameManager netGameManager = target as TinyNetGameManager;

			EditorGUILayout.LabelField("Update Network every X Fixed Frames");
			EditorGUILayout.IntSlider(netGameManager.NetworkEveryXFixedFrames, 1, 60);

			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(_maxNumberOfPlayers);

			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(_registeredPrefabs, true);

			if (GUILayout.Button("Register all TinyNetIdentity prefabs")) {
				netGameManager.RebuildAllRegisteredPrefabs(GetAllAssetsWithTinyNetIdentity());
			}

			EditorGUILayout.Space();

			netGameManager.currentLogFilter = (LogFilter)EditorGUILayout.EnumPopup("LogFilter:", netGameManager.currentLogFilter);

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
	}
}
