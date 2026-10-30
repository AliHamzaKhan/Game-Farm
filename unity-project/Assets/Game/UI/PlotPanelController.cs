using System.Text;
using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Ads;
using FarmQuest.Systems.Farming;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>
    /// Context panel for one farm tile (§39): shows only the actions that make
    /// sense right now. Big touch targets, minimal text. While a crop grows it
    /// shows its stage, timer and care needs, plus optional rewarded-ad offers.
    /// </summary>
    public class PlotPanelController : MonoBehaviour
    {
        public Text titleText;
        [Tooltip("Stage, time left and care checklist.")]
        public Text infoText;
        public Button clearButton;
        public Button digButton;
        public Button prepareButton;
        public Button plantButton;
        public Button waterButton;
        public Button compostButton;
        [Tooltip("Fertilizer (item 'fertilizer').")]
        public Button nutrientButton;
        [Tooltip("Crop-specific nutrient (nitrogen/potassium/calcium).")]
        public Button specialNutrientButton;
        public Button sprayButton;
        public Button pruneButton;
        public Button harvestButton;
        [Header("Consumables (shown only when owned)")]
        public Button premiumFertilizerButton;
        public Button liquidNutrientButton;
        public Button growthBoosterButton;
        public Button superCompostButton;
        [Header("Optional rewarded ads")]
        public Text adOfferText;
        public Button adSpeedupButton;
        public Button adStageButton;
        public Button adFinishButton;
        public Button adYieldButton;
        public Button closeButton;

        private FarmTile _tile;
        private FarmInteractionController _interaction;
        private float _refreshTimer;

        private void Awake()
        {
            closeButton?.onClick.AddListener(Hide);
        }

        private void OnEnable() => GameEvents.TileChanged += OnTileChanged;
        private void OnDisable() => GameEvents.TileChanged -= OnTileChanged;

        private void OnTileChanged(FarmTile tile)
        {
            if (tile == _tile) Refresh();
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

        private void Update()
        {
            // Timers tick while the panel is open; refresh twice a second.
            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer > 0f) return;
            _refreshTimer = 0.5f;
            Refresh();
        }

        private void Refresh()
        {
            if (_tile == null || _interaction == null) return;
            var crop = _tile.Crop;
            string name = _tile.HasCrop ? crop.Data.displayName : "Empty Plot";
            if (titleText != null)
                titleText.text = _tile.HasCrop ? $"{name}\n{StageName(crop.Stage)}" : $"{name}\n{_tile.SoilState}";
            if (infoText != null) infoText.text = _tile.HasCrop ? Describe(crop) : SoilHint();

            Set(clearButton, _tile.CanClear(), () => _interaction.DoClear(_tile));
            Set(digButton, _tile.CanDig(), () => _interaction.DoDig(_tile));
            Set(prepareButton, _tile.CanPrepare(), () => _interaction.DoPrepare(_tile));
            Set(plantButton, _tile.CanPlant(), () => _interaction.DoPlant(_tile));
            Set(waterButton, _tile.CanWater() && crop.NeedsWater, () => _interaction.DoWater(_tile));
            Set(compostButton,
                _tile.IsUnlocked && !_tile.CompostPrepared &&
                (_tile.SoilState == SoilState.Prepared && !_tile.HasCrop || (_tile.CanWater() && !crop.CompostApplied)),
                () => _interaction.DoCompost(_tile));
            // Required for some crops (see needs list); optional for the rest, where it still raises quality.
            bool fertilizeNow = _tile.HasCrop
                ? !crop.IsReady && !crop.NutrientApplied
                : _tile.SoilState == SoilState.Prepared && !_tile.NutrientPrepared;
            Set(nutrientButton, _tile.IsUnlocked && fertilizeNow, () => _interaction.DoNutrient(_tile));

            var care = ServiceLocator.Get<CropCareService>();
            Set(specialNutrientButton,
                _tile.HasCrop && !crop.IsReady && crop.Data.requiredNutrient != NutrientType.None &&
                !crop.SpecialNutrientApplied && crop.Stage >= GrowthStage.Vegetative,
                () => care.TryApplyNutrient(_tile));
            SetLabel(specialNutrientButton, _tile.HasCrop ? $"{crop.Data.requiredNutrient} Nutrient" : "Nutrient");
            Set(sprayButton, _tile.HasCrop && crop.HasPests, () => care.TrySpray(_tile));
            Set(pruneButton, _tile.HasCrop && crop.CanPrune, () => care.TryPrune(_tile));
            Set(harvestButton, _tile.CanHarvest(), () =>
            {
                var result = ServiceLocator.Get<FarmService>().TryHarvest(_tile);
                if (result.success)
                    GameEvents.RaiseToast($"+{result.quantity} {crop.Data.displayName} ({result.quality})!");
            });

            RefreshConsumable(premiumFertilizerButton, crop, "premium_fertilizer", care);
            RefreshConsumable(liquidNutrientButton, crop, "liquid_nutrient", care);
            RefreshConsumable(growthBoosterButton, crop, "growth_booster", care);
            RefreshConsumable(superCompostButton, crop, "super_compost", care);

            RefreshAds(crop);
        }

        private void RefreshConsumable(Button button, CropInstance crop, string itemId, CropCareService care)
        {
            if (button == null) return;
            var item = ServiceLocator.Get<CropDatabase>().GetItem(itemId);
            int owned = ServiceLocator.Get<Systems.Inventory.InventoryService>().GetCount(itemId);
            bool show = _tile.HasCrop && owned > 0 && CropCareService.CanUse(crop, item);
            Set(button, show, () => care.TryUseConsumable(_tile, itemId));
            if (show) SetLabel(button, $"{item.displayName} ({owned})");
        }

        private void RefreshAds(CropInstance crop)
        {
            ServiceLocator.TryGet(out RewardedAdService ads);
            bool growing = _tile.HasCrop && !crop.IsReady && ads != null;
            bool speed = growing && ads.CanOfferSpeedup(crop);
            bool stage = growing && ads.CanOfferStageSkip(crop);
            bool finish = growing && ads.CanOfferFinish(crop);
            bool yield = growing && ads.CanOfferYieldBoost(crop);

            if (adOfferText != null)
            {
                adOfferText.gameObject.SetActive(speed || stage || finish || yield);
                if (growing)
                {
                    string lead = crop.RemainingSeconds <= 120f
                        ? $"Your {crop.Data.displayName.ToLowerInvariant()} is almost ready!"
                        : $"Your {crop.Data.displayName.ToLowerInvariant()} is growing.";
                    adOfferText.text = $"{lead}\nOptional: watch an ad to speed up growth ({ads.AdsLeftToday} left today).";
                }
            }

            Set(adSpeedupButton, speed, () => ads.ShowCropSpeedupAd(crop, r => AdResult(r > 0f, $"{Mathf.CeilToInt(r)}s faster!")));
            if (growing) SetLabel(adSpeedupButton, $"Ad: -{Mathf.CeilToInt(ads.SpeedupSeconds(crop))}s");
            Set(adStageButton, stage, () => ads.ShowCropStageSkipAd(crop, r => AdResult(r > 0f, "Stage complete!")));
            Set(adFinishButton, finish, () => ads.ShowCropFinishAd(crop, r => AdResult(r > 0f, "Ready to harvest!")));
            Set(adYieldButton, yield, () => ads.ShowYieldBoostAd(crop, ok => AdResult(ok, "Bigger harvest: +50%!")));
        }

        private void AdResult(bool rewarded, string message)
        {
            // No reward unless the ad SDK confirmed completion.
            GameEvents.RaiseToast(rewarded ? message : "Ad not finished — no reward this time.");
            Refresh();
        }

        private static string Describe(CropInstance crop)
        {
            var sb = new StringBuilder();
            if (crop.IsReady) sb.Append("Ready to harvest!");
            else
                sb.Append($"Ready in {FormatTime(crop.RemainingSeconds)}  ·  next stage in {FormatTime(crop.SecondsToNextStage)}");
            var needs = crop.CurrentNeeds();
            if (needs.Count == 0 && !crop.IsReady) sb.Append("\nHealthy — nothing needed right now.");
            foreach (var need in needs) sb.Append("\n• ").Append(NeedText(crop, need));
            if (crop.Data.needsPruning && crop.Pruned) sb.Append("\nPruned: +25% harvest");
            return sb.ToString();
        }

        private string SoilHint()
        {
            if (_tile.CanClear()) return "Clear the grass to start a plot.";
            if (_tile.CanDig()) return "Dig the soil.";
            if (_tile.CanPrepare()) return "Prepare the soil for seeds.";
            if (_tile.CanPlant())
            {
                string seed = _interaction != null ? _interaction.SelectedSeedCropId : "";
                return $"Plant a seed. Selected: {seed} (change it in Seeds).";
            }
            return "";
        }

        public static string NeedText(CropInstance crop, CropHealth need)
        {
            switch (need)
            {
                case CropHealth.NeedsWater: return "Needs water — growing slowly";
                case CropHealth.NeedsFertilizer: return "Needs fertilizer (or the harvest is smaller)";
                case CropHealth.NutrientDeficiency: return $"Wants {crop.Data.requiredNutrient} nutrient (+15% harvest)";
                case CropHealth.PestAttack: return "Pest attack! Spray it to save the harvest";
                case CropHealth.NeedsPruning: return "Can be pruned now (+25% harvest)";
                case CropHealth.ReadyToHarvest: return "Ready to harvest";
                default: return "Healthy";
            }
        }

        public static string StageName(GrowthStage stage)
        {
            switch (stage)
            {
                case GrowthStage.Seed: return "Seed";
                case GrowthStage.Sprout: return "Sprouting";
                case GrowthStage.Vegetative: return "Growing leaves";
                case GrowthStage.Flowering: return "Flowering";
                case GrowthStage.Fruiting: return "Fruit developing";
                default: return "Ready to harvest";
            }
        }

        public static string FormatTime(float seconds)
        {
            int s = Mathf.CeilToInt(seconds);
            if (s >= 3600) return $"{s / 3600}h {(s % 3600) / 60}m";
            if (s >= 60) return $"{s / 60}m {s % 60:00}s";
            return $"{s}s";
        }

        private static void Set(Button button, bool visible, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.gameObject.SetActive(visible);
            button.onClick.RemoveAllListeners();
            if (action != null) button.onClick.AddListener(action);
        }

        private static void SetLabel(Button button, string label)
        {
            var text = button != null ? button.GetComponentInChildren<Text>(true) : null;
            if (text != null) text.text = label;
        }
    }
}
