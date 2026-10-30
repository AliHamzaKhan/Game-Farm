using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FarmQuest.Editor
{
    /// <summary>
    /// Main.unity is generated, not committed. If it's missing when the editor
    /// opens, generate the game data (when absent) and build the scene, then
    /// open it. Runs at most once per editor session and only after importing
    /// finishes, so asset writes can't retrigger it in a reload loop.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoSceneSetup
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string DataProbe = "Assets/Resources/Data/CropDatabase.asset";
        private const string RanKey = "FarmQuest.AutoSceneSetup.Ran";

        static AutoSceneSetup()
        {
            if (Application.isBatchMode || SessionState.GetBool(RanKey, false)) return;
            EditorApplication.update += WaitForIdle;
        }

        private static void WaitForIdle()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorApplication.update -= WaitForIdle;

            // Mark first: anything below may trigger a reload, which must not rerun this.
            SessionState.SetBool(RanKey, true);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) != null) return;

            Debug.Log("[FarmQuest] Main.unity not found — creating it now.");
            if (AssetDatabase.LoadAssetAtPath<Object>(DataProbe) == null)
                GameDataGenerator.GenerateAll();
            SceneSetup.CreateTestScenes();
            OpenMainScene();
        }

        [MenuItem("FarmQuest/Setup/Open Main Scene")]
        public static void OpenMainScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) == null)
            {
                Debug.LogWarning("[FarmQuest] Main.unity doesn't exist yet. Run FarmQuest/Setup/Create Test Scenes.");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(MainScenePath);
        }
    }
}
