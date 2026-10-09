using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Input;
using UnityEngine;

namespace FarmQuest.Systems.Farming
{
    /// <summary>
    /// Routes taps on farm tiles to the plot panel or quick actions.
    /// The currently selected seed (from the seed shop) is kept here.
    /// </summary>
    public class FarmInteractionController : MonoBehaviour
    {
        public UnityEngine.Camera farmCamera;
        public LayerMask tileLayer;
        public UI.PlotPanelController plotPanel;

        public string SelectedSeedCropId { get; set; } = "carrot";

        private FarmService _farm;
        private IInputService _input;

        private void Start()
        {
            _farm = ServiceLocator.Get<FarmService>();
            _input = ServiceLocator.Get<IInputService>();
            _input.Tapped += OnTapped;
            if (farmCamera == null) farmCamera = UnityEngine.Camera.main;
        }

        private void OnDestroy()
        {
            if (_input != null) _input.Tapped -= OnTapped;
        }

        private void OnTapped(Input.TapInfo tap)
        {
            if (farmCamera == null) return;
            var ray = farmCamera.ScreenPointToRay(tap.ScreenPosition);
            if (!Physics.Raycast(ray, out var hit, 500f, tileLayer)) return;

            var view = hit.collider.GetComponentInParent<FarmTileView>();
            if (view == null || view.Tile == null) return;

            var tile = view.Tile;
            if (!tile.IsUnlocked)
            {
                GameEvents.RaiseToast("Unlock this land first!");
                return;
            }

            // Smart quick actions for the obvious cases; panel for choices.
            if (tile.CanHarvest())
            {
                var result = _farm.TryHarvest(tile);
                if (result.success)
                    GameEvents.RaiseToast($"+{result.quantity} {result.cropId} ({result.quality})!");
                return;
            }

            if (plotPanel != null) plotPanel.Show(tile);
        }

        /// <summary>Called by PlotPanel buttons.</summary>
        public void DoClear(FarmTile t) => _farm.TryClear(t);
        public void DoDig(FarmTile t) => _farm.TryDig(t);
        public void DoPrepare(FarmTile t) => _farm.TryPrepare(t);
        public void DoPlant(FarmTile t) => _farm.TryPlant(t, SelectedSeedCropId);
        public void DoWater(FarmTile t) => _farm.TryWater(t);
        public void DoCompost(FarmTile t) => _farm.TryApplyCompost(t);
        public void DoNutrient(FarmTile t) => _farm.TryApplyNutrient(t);
    }
}
