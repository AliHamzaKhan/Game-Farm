using FarmQuest.Core.Services;
using UnityEngine;

namespace FarmQuest.Systems.Buildings
{
    /// <summary>
    /// VIEW: swaps the house prefab per level (spec §60 — upgrades are visible).
    /// Assign one prefab per level (index 0 = level 1).
    /// </summary>
    public class HouseView : MonoBehaviour
    {
        public string buildingId = "house";
        public GameObject[] levelPrefabs = new GameObject[0];

        private GameObject _current;

        private void Start()
        {
            GameEvents.BuildingUpgraded += OnUpgraded;
            if (ServiceLocator.TryGet(out BuildingService buildings))
                ShowLevel(buildings.GetLevel(buildingId));
        }

        private void OnDestroy()
        {
            GameEvents.BuildingUpgraded -= OnUpgraded;
        }

        private void OnUpgraded(string id, int level)
        {
            if (id == buildingId) ShowLevel(level);
        }

        private void ShowLevel(int level)
        {
            if (_current != null) Destroy(_current);
            _current = null;
            int index = Mathf.Clamp(level - 1, 0, levelPrefabs.Length - 1);
            if (levelPrefabs.Length > 0 && levelPrefabs[index] != null)
                _current = Instantiate(levelPrefabs[index], transform.position, transform.rotation, transform);
        }
    }
}
