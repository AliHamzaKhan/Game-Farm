using FarmQuest.Core.Services;
using FarmQuest.Systems.Farming;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>
    /// Context panel for one farm tile (§39): shows only the actions that make
    /// sense right now. Big touch targets, minimal text.
    /// </summary>
    public class PlotPanelController : MonoBehaviour
    {
        public Text titleText;
        public Button clearButton;
        public Button digButton;
        public Button prepareButton;
        public Button plantButton;
        public Button waterButton;
        public Button compostButton;
        public Button nutrientButton;
        public Button harvestButton;
        public Button closeButton;

        private FarmTile _tile;
        private FarmInteractionController _interaction;

        private void Awake()
        {
            closeButton?.onClick.AddListener(Hide);
        }

        public void Show(FarmTile tile)
        {
            _tile = tile;
            if (_interaction == null)
                _interaction = FindObjectOfType<FarmInteractionController>();
            gameObject.SetActive(true);
            Refresh();
        }

        public void Hide() => gameObject.SetActive(false);

        private void Refresh()
        {
            if (_tile == null) return;
            string name = _tile.HasCrop ? _tile.Crop.Data.displayName : "Empty Plot";
            if (titleText != null) titleText.text = $"{name}\n{_tile.SoilState}";

            Set(clearButton, _tile.CanClear(), () => _interaction.DoClear(_tile));
            Set(digButton, _tile.CanDig(), () => _interaction.DoDig(_tile));
            Set(prepareButton, _tile.CanPrepare(), () => _interaction.DoPrepare(_tile));
            Set(plantButton, _tile.CanPlant(), () => { _interaction.DoPlant(_tile); Refresh(); });
            Set(waterButton, _tile.CanWater(), () => _interaction.DoWater(_tile));
            Set(compostButton,
                _tile.IsUnlocked && (_tile.SoilState == Data.SoilState.Prepared || _tile.CanWater()),
                () => _interaction.DoCompost(_tile));
            Set(nutrientButton,
                _tile.IsUnlocked && (_tile.SoilState == Data.SoilState.Prepared || _tile.CanWater()),
                () => _interaction.DoNutrient(_tile));
            Set(harvestButton, false, null); // harvest is tap-to-collect; kept visible as hint
            if (harvestButton != null)
            {
                harvestButton.gameObject.SetActive(_tile.CanHarvest());
                var label = harvestButton.GetComponentInChildren<Text>();
                if (label != null) label.text = "Tap the crop to harvest! 🌱";
            }
        }

        private static void Set(Button button, bool visible, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.gameObject.SetActive(visible);
            button.onClick.RemoveAllListeners();
            if (action != null) button.onClick.AddListener(action);
        }
    }
}
