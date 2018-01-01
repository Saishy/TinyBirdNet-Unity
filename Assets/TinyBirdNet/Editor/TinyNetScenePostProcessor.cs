using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace TinyBirdNet {

	public class NetworkScenePostProcess {

		[PostProcessScene]
		public static void OnPostProcessScene() {
			int nextSceneId = 1;
			TinyNetIdentity[] tnis = MonoBehaviour.FindObjectsOfType<TinyNetIdentity>();

			for (int i = 0; i < tnis.Length; i++) {
				// if we had a [ConflictComponent] attribute that would be better than this check.
				// also there is no context about which scene this is in.
				if (tnis[i].GetComponent<TinyNetGameManager>() != null) {
					Debug.LogError("TinyNetGameManager has a TinyNetIdentity component. This will cause the TinyNetGameManager object to be disabled, so it is not recommended.");
				}

				if (tnis[i].isClient || tnis[i].isServer) {
					continue;
				}

				tnis[i].gameObject.SetActive(false);
				tnis[i].ForceSceneId(nextSceneId++);
			}
		}
	}
}
