using FarmQuest.Core.Services;
using FarmQuest.Systems.Buildings;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>House upgrade panel (§24): current level, next level, upgrade button.</summary>
    public class HousePanelController : MonoBehaviour
    {
        public string buildingId = "house";
        public Text titleText;
        public Text infoText;
        public Button upgradeButton;
        public Button closeButton;

        private BuildingService _buildings;

        private void Awake()
        {
            closeButton?.onClick.AddListener(() => gameObject.SetActive(false));
            upgradeButton?.onClick.AddListener(() =>
            {
                _buildings.BuyOrUpgrade(buildingId);
                Refresh();
            });
        }

        private void OnEnable()
        {
            _buildings = ServiceLocator.Get<BuildingService>();
            GameEvents.BuildingUpgraded += OnUpgraded;
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.BuildingUpgraded -= OnUpgraded;
        }

        private void OnUpgraded(string id, int level)
        {
            if (id == buildingId) Refresh();
        }

        private void Refresh()
        {
            var data = _buildings.GetData(buildingId);
            if (data == null) return;
            int level = _buildings.GetLevel(buildingId);
            int max = _buildings.MaxLevel(buildingId);

            if (titleText != null)
                titleText.text = level > 0 ? data.levels[level - 1].levelName : data.displayName;
            if (infoText != null)
                infoText.text = level > 0 ? data.levels[level - 1].description : "Not built yet.";

            if (upgradeButton != null)
            {
                if (level >= max)
                {
                    upgradeButton.interactable = false;
                    SetLabel(upgradeButton, "MAX LEVEL ⭐");
                }
                else
                {
                    int cost = data.levels[level].cost;
                    string nextName = data.levels[level].levelName;
                    upgradeButton.interactable = true;
                    SetLabel(upgradeButton, $"Upgrade to {nextName}\n🪙{cost}");
                }
            }
        }

        private static void SetLabel(Button b, string text)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
        }
    }
}
