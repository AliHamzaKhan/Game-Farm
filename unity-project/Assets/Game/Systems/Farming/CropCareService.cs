using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Inventory;

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

        private static void Done(FarmTile tile, string action, int xp, string toast)
        {
            ServiceLocator.Get<Economy.EconomyService>().AddXp(xp, action);
            GameEvents.RaiseTileChanged(tile);
            GameEvents.RaiseCropCared(action);
            GameEvents.RaiseToast(toast);
        }
    }
}
