using System;
using System.Collections.Generic;
using FarmQuest.Core.Save;
using FarmQuest.Core.Services;
using FarmQuest.Core.Time;
using FarmQuest.Data;
using UnityEngine;

namespace FarmQuest.Systems.Farming
{
    public struct HarvestResult
    {
        public bool success;
        public string cropId;
        public int quantity;
        public QualityGrade quality;
        public int xp;
    }

    /// <summary>
    /// LOGIC: grid ownership, soil workflow, planting, watering, harvest (§6).
    /// Pure C# and testable; views/ UI react through GameEvents.TileChanged.
    /// Coordinates Inventory (seeds in, crops out) and Economy (XP).
    /// </summary>
    public class FarmService
    {
        private readonly FarmLayoutData _layout;
        private readonly CropDatabase _database;
        private readonly ITimeService _time;
        private readonly Dictionary<Vector2Int, FarmTile> _tiles = new Dictionary<Vector2Int, FarmTile>();
        private readonly HashSet<int> _unlockedRegions = new HashSet<int>();
        private readonly System.Random _rng = new System.Random();

        public int Width => _layout.gridWidth;
        public int Height => _layout.gridHeight;
        public float TileSize => _layout.tileSize;

        public FarmService(FarmLayoutData layout, CropDatabase database, ITimeService time)
        {
            _layout = layout;
            _database = database;
            _time = time;
            BuildGrid();
        }

        private void BuildGrid()
        {
            _tiles.Clear();
            _unlockedRegions.Clear();
            foreach (var region in _layout.regions)
            {
                if (region.unlockedByDefault) _unlockedRegions.Add(region.regionId);
                for (int x = region.area.xMin; x < region.area.xMax; x++)
                    for (int y = region.area.yMin; y < region.area.yMax; y++)
                    {
                        var pos = new Vector2Int(x, y);
                        if (!_tiles.ContainsKey(pos))
                            _tiles[pos] = new FarmTile(pos, region.regionId, region.unlockedByDefault);
                    }
            }
        }

        public FarmTile GetTile(Vector2Int pos)
        {
            _tiles.TryGetValue(pos, out var tile);
            return tile;
        }

        public IEnumerable<FarmTile> AllTiles() => _tiles.Values;
        public bool IsRegionUnlocked(int regionId) => _unlockedRegions.Contains(regionId);
        public int UnlockedRegionCount() => _unlockedRegions.Count;

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            return new Vector3(gridPos.x * TileSize, 0f, gridPos.y * TileSize);
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPos.x / TileSize),
                Mathf.RoundToInt(worldPos.z / TileSize));
        }

        // ---------- soil workflow ----------

        public bool TryClear(FarmTile tile)
        {
            if (tile == null || !tile.CanClear()) return false;
            tile.Clear();
            GameEvents.RaiseTileChanged(tile);
            GameEvents.RaiseTileCleared();
            return true;
        }

        public bool TryDig(FarmTile tile)
        {
            if (tile == null || !tile.CanDig()) return false;
            tile.Dig();
            ServiceLocator.Get<Economy.EconomyService>().AddXp(1, "dig");
            GameEvents.RaiseTileChanged(tile);
            GameEvents.RaiseTileDug();
            return true;
        }

        public bool TryPrepare(FarmTile tile)
        {
            if (tile == null || !tile.CanPrepare()) return false;
            tile.Prepare();
            GameEvents.RaiseTileChanged(tile);
            GameEvents.RaiseTilePrepared();
            return true;
        }

        public bool TryPlant(FarmTile tile, string cropId)
        {
            if (tile == null || !tile.CanPlant()) return false;
            var data = _database.GetCrop(cropId);
            if (data == null) return false;

            var inventory = ServiceLocator.Get<Inventory.InventoryService>();
            string seedItemId = "seed_" + cropId;
            if (!inventory.TryRemove(seedItemId, 1))
            {
                GameEvents.RaiseToast($"No {data.displayName} seeds — visit the seed shop!");
                return false;
            }

            var crop = new CropInstance(data, _time, _time.UtcNow.Ticks);
            if (tile.CompostPrepared) { crop.ApplyCompost(); tile.ClearCompostPrepared(); }
            if (tile.NutrientPrepared) { crop.ApplyNutrient(); tile.ClearNutrientPrepared(); }
            // Phase 5: out-of-season crops grow slower, never blocked.
            if (ServiceLocator.TryGet(out World.SeasonService seasons))
            {
                var current = seasons.CurrentSeason;
                if (data.season != Data.Season.None && data.season != current)
                {
                    crop.GrowthTimeMultiplier = 2f;
                    GameEvents.RaiseToast($"{data.displayName} grows slower out of season 🌱");
                }
            }
            tile.Plant(crop);
            ServiceLocator.Get<Systems.Farming.CropGrowthService>().Track(crop, tile);
            ServiceLocator.Get<Economy.EconomyService>().AddXp(3, "plant");
            GameEvents.RaiseTileChanged(tile);
            GameEvents.RaiseCropPlanted(cropId);
            return true;
        }

        public bool TryWater(FarmTile tile)
        {
            if (tile == null || !tile.CanWater()) return false;
            tile.Crop.Water();
            tile.SetWaterState(tile.Crop.WateringsDone >= tile.Crop.Data.waterRequired
                ? WaterState.WellWatered : WaterState.Moist);
            ServiceLocator.Get<Economy.EconomyService>().AddXp(2, "water");
            GameEvents.RaiseTileChanged(tile);
            GameEvents.RaiseCropWatered();
            return true;
        }

        public bool TryApplyCompost(FarmTile tile)
        {
            if (tile == null || tile.SoilState < SoilState.Prepared || tile.SoilState > SoilState.Growing) return false;
            var inventory = ServiceLocator.Get<Inventory.InventoryService>();
            if (!inventory.TryRemove("compost", 1))
            {
                GameEvents.RaiseToast("No compost — build a compost bin!");
                return false;
            }
            if (tile.HasCrop) tile.Crop.ApplyCompost();
            else tile.PrepareCompost(); // worked into the soil; transfers to crop at planting
            GameEvents.RaiseTileChanged(tile);
            return true;
        }

        /// <summary>
        /// Applies nutrients (fertilizer item): on a growing crop directly, or
        /// worked into prepared soil for the next planting. Needed for ⭐⭐⭐.
        /// </summary>
        public bool TryApplyNutrient(FarmTile tile)
        {
            if (tile == null || !tile.IsUnlocked) return false;
            if (tile.SoilState < SoilState.Prepared || tile.SoilState > SoilState.Growing) return false;
            var inventory = ServiceLocator.Get<Inventory.InventoryService>();
            if (!inventory.TryRemove("fertilizer", 1))
            {
                GameEvents.RaiseToast("No fertilizer — buy some at the market!");
                return false;
            }
            if (tile.HasCrop) tile.Crop.ApplyNutrient();
            else tile.PrepareNutrient(); // transfers to crop at planting
            GameEvents.RaiseTileChanged(tile);
            return true;
        }

        public HarvestResult TryHarvest(FarmTile tile)
        {
            var result = new HarvestResult { success = false };
            if (tile == null || !tile.CanHarvest()) return result;

            var crop = tile.TakeHarvest();
            var quality = crop.CalculateQuality();
            int baseQty = _rng.Next(crop.Data.harvestMin, crop.Data.harvestMax + 1);
            int qty = Mathf.Max(1, Mathf.RoundToInt(baseQty * CropInstance.QualityYieldMultiplier(quality)));
            int xp = crop.Data.xpReward + (int)quality * 2;

            var inventory = ServiceLocator.Get<Inventory.InventoryService>();
            inventory.Add("crop_" + crop.Data.cropId, qty, quality);

            ServiceLocator.Get<Economy.EconomyService>().AddXp(xp, "harvest");
            ServiceLocator.Get<Systems.Farming.CropGrowthService>().Untrack(crop);

            GameEvents.RaiseTileChanged(tile);
            GameEvents.RaiseCropHarvested(crop, qty);

            result.success = true;
            result.cropId = crop.Data.cropId;
            result.quantity = qty;
            result.quality = quality;
            result.xp = xp;
            return result;
        }

        // ---------- regions ----------

        public bool TryUnlockRegion(int regionId)
        {
            FarmRegion region = null;
            foreach (var r in _layout.regions)
                if (r.regionId == regionId) { region = r; break; }
            if (region == null || _unlockedRegions.Contains(regionId)) return false;

            var progression = ServiceLocator.Get<Economy.ProgressionService>();
            if (progression.Level < region.unlockLevel)
            {
                GameEvents.RaiseToast($"Reach level {region.unlockLevel} to unlock {region.regionName}.");
                return false;
            }
            if (!ServiceLocator.Get<Economy.EconomyService>().TrySpend(region.unlockCost))
            {
                GameEvents.RaiseToast($"Need {region.unlockCost} coins for {region.regionName}.");
                return false;
            }

            _unlockedRegions.Add(regionId);
            foreach (var tile in _tiles.Values)
                if (tile.RegionId == regionId)
                {
                    tile.IsUnlocked = true;
                    GameEvents.RaiseTileChanged(tile);
                }
            ServiceLocator.Get<Economy.EconomyService>().AddXp(100, "unlock_land");
            GameEvents.RaiseToast($"{region.regionName} unlocked!");
            GameEvents.RaiseRegionUnlocked(regionId);
            return true;
        }

        // ---------- save ----------

        public FarmSaveData CaptureState()
        {
            var data = new FarmSaveData();
            foreach (var tile in _tiles.Values)
            {
                data.tiles.Add(new FarmTileSaveData
                {
                    x = tile.GridPosition.x,
                    y = tile.GridPosition.y,
                    regionId = tile.RegionId,
                    unlocked = tile.IsUnlocked,
                    soilState = (int)tile.SoilState,
                    cropId = tile.Crop?.Data.cropId,
                    plantedAtTicks = tile.Crop?.PlantedAtTicks ?? 0,
                    wateringsDone = tile.Crop?.WateringsDone ?? 0,
                    compostApplied = tile.Crop?.CompostApplied ?? false,
                    nutrientApplied = tile.Crop?.NutrientApplied ?? false,
                    growthTimeMultiplier = tile.Crop != null ? tile.Crop.GrowthTimeMultiplier : 1f,
                    compostPrepared = tile.CompostPrepared,
                    nutrientPrepared = tile.NutrientPrepared
                });
            }
            data.unlockedRegions.AddRange(_unlockedRegions);
            return data;
        }

        public void RestoreState(FarmSaveData data)
        {
            BuildGrid();
            if (data == null) return;
            _unlockedRegions.Clear();
            foreach (var id in data.unlockedRegions) _unlockedRegions.Add(id);

            var growth = ServiceLocator.Get<CropGrowthService>();
            foreach (var t in data.tiles)
            {
                var tile = GetTile(new Vector2Int(t.x, t.y));
                if (tile == null) continue;
                tile.RegionId = t.regionId;
                tile.IsUnlocked = t.unlocked;

                CropInstance crop = null;
                if (!string.IsNullOrEmpty(t.cropId))
                {
                    var cropData = _database.GetCrop(t.cropId);
                    if (cropData != null)
                    {
                        crop = new CropInstance(cropData, _time, t.plantedAtTicks);
                        for (int i = 0; i < t.wateringsDone; i++) crop.Water();
                        if (t.compostApplied) crop.ApplyCompost();
                        if (t.nutrientApplied) crop.ApplyNutrient();
                        crop.GrowthTimeMultiplier = t.growthTimeMultiplier > 0 ? t.growthTimeMultiplier : 1f;
                        growth.Track(crop, tile);
                    }
                }
                tile.Restore((SoilState)t.soilState, crop,
                    crop != null ? WaterState.Moist : WaterState.Dry);
                if (t.compostPrepared) tile.PrepareCompost();
                if (t.nutrientPrepared) tile.PrepareNutrient();
            }
        }
    }
}
