#if UNITY_EDITOR
using TinyBirdUtils;
using UnityEditor.Callbacks;
using UnityEngine;

namespace TinyBirdNet {
	/// <summary>
	/// Used to run a method on the PostProcessScene.
	/// </summary>
	public class NetworkScenePostProcess {

		/// <summary>
		/// Called when [PostProcessScene].
		/// <para>Checks all scene objects.</para>
		/// </summary>
		[PostProcessScene]
		public static void OnPostProcessScene() {
			if (Application.isPlaying) {
				return;
            }

			int nextSceneId = 1;
			TinyNetIdentity[] tnis = MonoBehaviour.FindObjectsOfType<TinyNetIdentity>();

			for (int i = 0; i < tnis.Length; i++) {
				// if we had a [ConflictComponent] attribute that would be better than this check.
				// also there is no context about which scene this is in.
				if (tnis[i].GetComponent<TinyNetGameManager>() != null) {
					if (TinyNetLogLevel.logError) { TinyLogger.LogError("TinyNetGameManager has a TinyNetIdentity component. This will cause the TinyNetGameManager object to be disabled, so it is not recommended."); }
				}

				tnis[i].gameObject.SetActive(false);
				tnis[i].ForceSceneId(nextSceneId++);
			}
		}
	}
}
#endif