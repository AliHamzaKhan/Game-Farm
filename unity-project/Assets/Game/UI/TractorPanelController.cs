using FarmQuest.Core.Services;
using FarmQuest.Systems.Machines;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>Tractor HUD while driving (§23): mode buttons + exit.</summary>
    public class TractorPanelController : MonoBehaviour
    {
        public Button plowButton;
        public Button plantButton;
        public Button fertilizeButton;
        public Button harvestButton;
        public Button exitButton;
        public Text statusText;

        private TractorController _tractor;
        private EquipmentService _equipment;

        private void Awake()
        {
            exitButton?.onClick.AddListener(() =>
            {
                if (_tractor != null) _tractor.StopDriving();
            });
            plowButton?.onClick.AddListener(() => SetMode(TractorMode.Plow));
            plantButton?.onClick.AddListener(() => SetMode(TractorMode.Plant));
            fertilizeButton?.onClick.AddListener(() => SetMode(TractorMode.Fertilize));
            harvestButton?.onClick.AddListener(() => SetMode(TractorMode.Harvest));
        }

        public void Show()
        {
            _tractor = FindObjectOfType<TractorController>();
            _equipment = ServiceLocator.Get<EquipmentService>();
            // Sync seed choice with the seed shop.
            var interaction = FindObjectOfType<Systems.Farming.FarmInteractionController>();
            if (_tractor != null && interaction != null)
                _tractor.SelectedSeedCropId = interaction.SelectedSeedCropId;
            gameObject.SetActive(true);
            Refresh();
        }

        public void Hide() => gameObject.SetActive(false);

        private void SetMode(TractorMode mode)
        {
            if (_tractor != null && _tractor.SetMode(mode)) Refresh();
        }

        private void Refresh()
        {
            if (_tractor == null || _equipment == null) return;
            SetModeButton(plowButton, TractorMode.Plow, "Plow");
            SetModeButton(plantButton, TractorMode.Plant, "Plant");
            SetModeButton(fertilizeButton, TractorMode.Fertilize, "Fertilize");
            SetModeButton(harvestButton, TractorMode.Harvest, "Harvest");
            if (statusText != null)
                statusText.text = $"🚜 {_tractor.Mode} (radius {_equipment.ModeRadius(_tractor.Mode)})";
        }

        private void SetModeButton(Button button, TractorMode mode, string label)
        {
            if (button == null) return;
            bool unlocked = _equipment.IsModeUnlocked(mode);
            button.interactable = unlocked;
            var t = button.GetComponentInChildren<Text>();
            if (t != null)
                t.text = _tractor.Mode == mode ? $"▶ {label}" : (unlocked ? label : $"{label} 🔒");
        }
    }
}
