using FarmQuest.Core.Services;
using FarmQuest.Systems.Animals;
using UnityEngine;

namespace FarmQuest.Systems.Decor
{
    /// <summary>
    /// Keeps placed-decoration visuals in sync with DecorService.Placed.
    /// </summary>
    public class DecorViewManager : MonoBehaviour
    {
        private void Start()
        {
            GameEvents.DecorChanged += Sync;
            Sync();
        }

        private void OnDestroy()
        {
            GameEvents.DecorChanged -= Sync;
        }

        private void Sync()
        {
            if (!ServiceLocator.TryGet(out DecorService decor)) return;
            if (!ServiceLocator.TryGet(out AnimalDatabase database)) return;

            foreach (Transform child in transform) Destroy(child.gameObject);

            foreach (var placed in decor.Placed)
            {
                var data = database.GetDecor(placed.DecorId);
                if (data == null || data.prefab == null) continue;
                var go = Instantiate(data.prefab,
                    new Vector3(placed.X, 0f, placed.Z),
                    Quaternion.Euler(0f, placed.RotY, 0f), transform);
                go.name = $"Decor_{placed.DecorId}";
            }
        }
    }
}
