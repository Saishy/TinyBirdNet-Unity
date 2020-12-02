using UnityEditor;

namespace TinyBirdNet {

    [InitializeOnLoad]
    public class ExecutionOrderManager : Editor {

        const int EXECUTION_ORDER = -10;

        static ExecutionOrderManager() {
            if (EditorApplication.isPlaying || EditorApplication.isPaused) {
                return;
            }
            // Get the name of the script we want to change it's execution order
            string scriptName = typeof(TinyNetGameManager).Name;

            // Iterate through all scripts (Might be a better way to do this?)
            foreach (MonoScript monoScript in MonoImporter.GetAllRuntimeMonoScripts()) {
                // If found our script
                if (monoScript.name == scriptName) {
                    // And it's not at the execution time we want already
                    // (Without this we will get stuck in an infinite loop)
                    if (MonoImporter.GetExecutionOrder(monoScript) > EXECUTION_ORDER) {
                        MonoImporter.SetExecutionOrder(monoScript, EXECUTION_ORDER);
                    }
                    break;
                }
            }
        }
    }
}