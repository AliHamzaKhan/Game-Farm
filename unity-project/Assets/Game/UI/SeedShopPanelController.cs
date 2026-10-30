using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Farming;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>
    /// Seed picker: tap a seed → it becomes the active seed → tap a prepared
    /// plot to plant. Kept separate from Market for one-tap planting flow.
    /// </summary>
    public class SeedShopPanelController : MonoBehaviour
    {
        public Transform listContent;
        public Button seedButtonPrefab;
        public Button closeButton;

        private void Awake()
        {
            closeButton?.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void OnEnable() => Rebuild();

        private void Rebuild()
        {
            if (listContent == null || seedButtonPrefab == null) return;
            foreach (Transform child in listContent) Destroy(child.gameObject);

            var db = ServiceLocator.Get<CropDatabase>();
            var interaction = FindObjectOfType<FarmInteractionController>();

            foreach (var crop in db.crops)
            {
                if (!Systems.World.WorldProgressionService.IsCropAvailable(crop)) continue;
                var button = Instantiate(seedButtonPrefab, listContent);
                var label = button.GetComponentInChildren<Text>();
                int owned = ServiceLocator.Get<Systems.Inventory.InventoryService>().GetCount("seed_" + crop.cropId);
                if (label != null) label.text = $"{crop.displayName}\n{owned} seeds · {PlotPanelController.FormatTime(crop.growthDurationSeconds)}";
                string id = crop.cropId;
                button.onClick.AddListener(() =>
                {
                    if (interaction != null) interaction.SelectedSeedCropId = id;
                    GameEvents.RaiseToast($"{crop.displayName} selected — tap a prepared plot!");
                    gameObject.SetActive(false);
                });
            }
        }
    }
}
