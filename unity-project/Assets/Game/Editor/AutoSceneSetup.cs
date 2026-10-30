using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FarmQuest.Editor
{
    /// <summary>
    /// Main.unity is generated, not committed. On editor load, if it's missing,
    /// generate the game data (when absent) and build the scene automatically,
    /// then open it — so a fresh clone is playable without hunting for menus.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoSceneSetup
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string DataProbe = "Assets/Resources/Data/CropDatabase.asset";

        static AutoSceneSetup()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += Run;
        }

        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
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
