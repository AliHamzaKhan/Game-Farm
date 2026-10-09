using FarmQuest.Core.Services;
using FarmQuest.Systems.Farming;
using UnityEngine;

namespace FarmQuest.Systems.Machines
{
    public struct WorkResult
    {
        public int cleared;
        public int dug;
        public int prepared;
        public int planted;
        public int watered;
        public int fertilized;
        public int harvested;
        public int harvestQty;
        public int TotalActions => cleared + dug + prepared + planted + watered + fertilized + harvested;
    }

    /// <summary>
    /// LOGIC: applies tractor work to a tile area (§23). Pure C# — the
    /// MonoBehaviour only drives movement. Reuses FarmService primitives so
    /// all XP/inventory/events stay consistent.
    /// </summary>
    public class TractorWorkService
    {
        private readonly FarmService _farm;

        public TractorWorkService(FarmService farm) { _farm = farm; }

        public WorkResult DoWork(Vector2Int center, int radius, TractorMode mode, string seedCropId)
        {
            var result = new WorkResult();
            for (int dx = -radius; dx <= radius; dx++)
                for (int dz = -radius; dz <= radius; dz++)
                {
                    var tile = _farm.GetTile(center + new Vector2Int(dx, dz));
                    if (tile == null || !tile.IsUnlocked) continue;
                    switch (mode)
                    {
                        case TractorMode.Plow:
                            if (tile.CanClear() && _farm.TryClear(tile)) result.cleared++;
                            else if (tile.CanDig() && _farm.TryDig(tile)) result.dug++;
                            else if (tile.CanPrepare() && _farm.TryPrepare(tile)) result.prepared++;
                            break;
                        case TractorMode.Plant:
                            if (tile.CanPlant() && _farm.TryPlant(tile, seedCropId)) result.planted++;
                            break;
                        case TractorMode.Fertilize:
                            if (_farm.TryApplyNutrient(tile)) result.fertilized++;
                            break;
                        case TractorMode.Harvest:
                            var harvest = _farm.TryHarvest(tile);
                            if (harvest.success)
                            {
                                result.harvested++;
                                result.harvestQty += harvest.quantity;
                            }
                            break;
                    }
                }
            return result;
        }
    }
}
