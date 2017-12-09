using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace TinyBirdNet {

	public class NetworkScenePostProcess {

		[PostProcessScene]
		public static void OnPostProcessScene() {
			int nextSceneId = 1;

			foreach (TinyNetIdentity tinyNetId in MonoBehaviour.FindObjectsOfType<TinyNetIdentity>()) {
				// if we had a [ConflictComponent] attribute that would be better than this check.
				// also there is no context about which scene this is in.
				if (tinyNetId.GetComponent<TinyNetGameManager>() != null) {
					Debug.LogError("TinyNetGameManager has a TinyNetIdentity component. This will cause the TinyNetGameManager object to be disabled, so it is not recommended.");
				}

				if (tinyNetId.isClient || tinyNetId.isServer) {
					continue;
				}

				tinyNetId.gameObject.SetActive(false);
				tinyNetId.ForceSceneId(nextSceneId++);
			}
		}
	}
}
