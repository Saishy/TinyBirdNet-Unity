using System.Diagnostics;

namespace TinyBirdUtils {

	/// <summary>
	/// Helper that removes debug calls from release builds.
	/// <para>Currently not working due to a bug in Unity [Conditional].</para>
	/// </summary>
	public class TinyLogger {

		//[Conditional("DEBUG")]
		public static void Log(object message, UnityEngine.Object context = null) {
			if (!UnityEngine.Debug.isDebugBuild) {
				return;
			}

			if (context != null) {
				UnityEngine.Debug.Log(message, context);
			} else {
				UnityEngine.Debug.Log(message);
			}
		}

		//[Conditional("DEBUG")]
		public static void LogError(object message, UnityEngine.Object context = null) {
			if (!UnityEngine.Debug.isDebugBuild) {
				return;
			}

			if (context != null) {
				UnityEngine.Debug.LogError(message, context);
			} else {
				UnityEngine.Debug.LogError(message);
			}
		}

		//[Conditional("DEBUG")]
		public static void LogWarning(object message, UnityEngine.Object context = null) {
			if (!UnityEngine.Debug.isDebugBuild) {
				return;
			}

			if (context != null) {
				UnityEngine.Debug.LogWarning(message, context);
			} else {
				UnityEngine.Debug.LogWarning(message);
			}
		}
	}
}
