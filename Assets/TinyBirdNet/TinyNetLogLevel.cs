using UnityEngine;

namespace TinyBirdNet {

	/// <summary>
	/// A simple log filter level to use in debug logs.
	/// </summary>
	public class TinyNetLogLevel {

		static public LogFilter currentLevel = LogFilter.Dev;

		static public bool logDev { get { return currentLevel <= LogFilter.Dev; } }
		static public bool logDebug { get { return currentLevel <= LogFilter.Debug; } }
		static public bool logInfo { get { return currentLevel <= LogFilter.Info; } }
		static public bool logWarn { get { return currentLevel <= LogFilter.Warn; } }
		static public bool logError { get { return currentLevel <= LogFilter.Error; } }
	}

	/// <summary>
	/// The available levels of filter.
	/// </summary>
	public enum LogFilter {
		Dev = 0,
		Debug = 1,
		Info = 2,
		Warn = 3,
		Error = 4
	};
}
