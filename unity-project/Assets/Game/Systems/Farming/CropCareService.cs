using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Inventory;
using UnityEngine;

namespace FarmQuest.Systems.Farming
{
    /// <summary>
    /// LOGIC: crop-specific care actions beyond watering/fertilizer (which stay
    /// in FarmService): pest spraying, pruning and nutrients. Consumes items from
    /// the inventory; effects are stored on the CropInstance and saved with it.
    /// </summary>
    public class CropCareService
    {
        public const string PesticideItemId = "pesticide";

        public bool TrySpray(FarmTile tile)
        {
            if (tile == null || !tile.HasCrop || !tile.Crop.HasPests) return false;
            if (!ServiceLocator.Get<InventoryService>().TryRemove(PesticideItemId, 1))
            {
                GameEvents.RaiseSupplyMissing(PesticideItemId);
                GameEvents.RaiseToast("No pest spray — buy some at the market!");
                return false;
            }
            tile.Crop.Spray();
            Done(tile, "spray", 3, "Pests gone!");
            return true;
        }

        public bool TryPrune(FarmTile tile)
        {
            if (tile == null || !tile.HasCrop || !tile.Crop.CanPrune) return false;
            tile.Crop.Prune(); // shears are free: pruning is a skill, not a purchase
            Done(tile, "prune", 3, "Pruned! This plant will give more.");
            return true;
        }

        public bool TryApplyNutrient(FarmTile tile)
        {
            if (tile == null || !tile.HasCrop || tile.Crop.IsReady) return false;
            var crop = tile.Crop;
            var type = crop.Data.requiredNutrient;
            if (type == NutrientType.None || crop.SpecialNutrientApplied) return false;
            string itemId = CropInstance.NutrientItemId(type);
            if (!ServiceLocator.Get<InventoryService>().TryRemove(itemId, 1))
            {
                GameEvents.RaiseToast($"{crop.Data.displayName} needs {type} nutrient — buy some at the market!");
                return false;
            }
            crop.ApplySpecialNutrient();
            Done(tile, "nutrient", 2, $"{type} nutrient added!");
            return true;
        }

        /// <summary>
        /// Uses a consumable from the bag on a planted crop. Effects come from the
        /// item's data (ConsumableEffect + effectValue), so new items need no code.
        /// Returns false (and keeps the item) when it would do nothing.
        /// </summary>
        public bool TryUseConsumable(FarmTile tile, string itemId)
        {
            if (tile == null || !tile.HasCrop || tile.Crop.IsReady) return false;
            var db = ServiceLocator.Get<CropDatabase>();
            var item = db.GetItem(itemId);
            if (item == null || item.effect == ConsumableEffect.None) return false;
            if (!CanUse(tile.Crop, item)) return false;
            var inventory = ServiceLocator.Get<InventoryService>();
            if (!inventory.TryRemove(itemId, 1))
            {
                GameEvents.RaiseSupplyMissing(itemId);
                return false;
            }
            Apply(tile.Crop, item);
            Done(tile, "consumable", 2, $"{item.displayName} used!");
            return true;
        }

        /// <summary>True when the item would actually help this crop right now.</summary>
        public static bool CanUse(CropInstance crop, ItemData item)
        {
            if (crop == null || crop.IsReady || item == null) return false;
            switch (item.effect)
            {
                case ConsumableEffect.Fertilizer: return !crop.NutrientApplied;
                case ConsumableEffect.Compost: return !crop.CompostApplied;
                case ConsumableEffect.PestSpray: return crop.HasPests;
                case ConsumableEffect.PremiumFertilizer: return !crop.NutrientApplied;
                case ConsumableEffect.LiquidNutrient: return !crop.SpecialNutrientApplied;
                case ConsumableEffect.GrowthBooster: return crop.RemainingSeconds > 1f;
                case ConsumableEffect.SuperCompost:
                    return !crop.CompostApplied || !crop.NutrientApplied || !crop.SpecialNutrientApplied || crop.HasPests;
                default: return false;
            }
        }

        private static void Apply(CropInstance crop, ItemData item)
        {
            float timer = (float)crop.DurationSeconds;
            switch (item.effect)
            {
                case ConsumableEffect.Fertilizer: crop.ApplyNutrient(); break;
                case ConsumableEffect.Compost: crop.ApplyCompost(); break;
                case ConsumableEffect.PestSpray: crop.Spray(); break;
                case ConsumableEffect.PremiumFertilizer:
                    crop.ApplyNutrient();
                    crop.ReduceRemainingTime(Mathf.Min(crop.RemainingSeconds, timer * item.effectValue));
                    break;
                case ConsumableEffect.LiquidNutrient:
                    crop.ApplySpecialNutrient();
                    crop.BonusYieldMultiplier *= 1f + item.effectValue;
                    break;
                case ConsumableEffect.GrowthBooster:
                    // A shortcut, not a skip: at most effectValue of the timer per booster.
                    crop.ReduceRemainingTime(Mathf.Min(crop.RemainingSeconds, timer * item.effectValue));
                    break;
                case ConsumableEffect.SuperCompost:
                    crop.ApplyCompost();
                    crop.ApplyNutrient();
                    crop.ApplySpecialNutrient();
                    crop.Spray();
                    break;
            }
        }

        private static void Done(FarmTile tile, string action, int xp, string toast)
        {
            ServiceLocator.Get<Economy.EconomyService>().AddXp(xp, action);
            GameEvents.RaiseTileChanged(tile);
            GameEvents.RaiseCropCared(action);
            GameEvents.RaiseToast(toast);
        }
    }
}
