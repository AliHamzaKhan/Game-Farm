using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmQuest.Core.Services
{
    /// <summary>
    /// Tiny helper: the Bootstrap scene loads the Farm scene after Bootstrapper
    /// finishes its Awake() boot. (Start runs after all Awakes, so services
    /// and the save are ready.)
    /// </summary>
    public class BootstrapSceneLoader : MonoBehaviour
    {
        public string farmSceneName = "Farm";

        private void Start()
        {
            SceneManager.LoadScene(farmSceneName);
        }
    }
}
