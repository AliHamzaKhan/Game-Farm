using FarmQuest.Data;
using UnityEngine;

namespace FarmQuest.Systems.Farming
{
    /// <summary>
    /// DATA: one farm grid cell (§6). Pure C# — no MonoBehaviour, fully testable.
    /// Views observe it via GameEvents.TileChanged.
    /// </summary>
    public class FarmTile
    {
        public Vector2Int GridPosition { get; }
        public int RegionId { get; set; }
        public bool IsUnlocked { get; set; }
        public SoilState SoilState { get; private set; }
        public CropInstance Crop { get; private set; }
        public WaterState WaterState { get; private set; }

        public bool HasCrop => Crop != null;

        /// <summary>Compost worked into prepared soil before planting.</summary>
        public bool CompostPrepared { get; private set; }

        /// <summary>Nutrients worked into prepared soil before planting.</summary>
        public bool NutrientPrepared { get; private set; }

        public FarmTile(Vector2Int gridPosition, int regionId, bool unlocked)
        {
            GridPosition = gridPosition;
            RegionId = regionId;
            IsUnlocked = unlocked;
            SoilState = SoilState.Grass;
            WaterState = WaterState.Dry;
        }

        public bool CanClear() => IsUnlocked && SoilState == SoilState.Grass;
        public bool CanDig() => IsUnlocked && SoilState == SoilState.Cleared;
        public bool CanPrepare() => IsUnlocked && SoilState == SoilState.Dug;
        public bool CanPlant() => IsUnlocked && SoilState == SoilState.Prepared && !HasCrop;
        public bool CanWater() => HasCrop && !Crop.IsReady;
        public bool CanHarvest() => HasCrop && Crop.IsReady;

        public void Clear() { if (CanClear()) SoilState = SoilState.Cleared; }
        public void Dig() { if (CanDig()) SoilState = SoilState.Dug; }
        public void Prepare() { if (CanPrepare()) SoilState = SoilState.Prepared; }

        public void PrepareCompost()
        {
            if (SoilState == SoilState.Prepared && !HasCrop) CompostPrepared = true;
        }

        public void ClearCompostPrepared() => CompostPrepared = false;

        public void PrepareNutrient()
        {
            if (SoilState == SoilState.Prepared && !HasCrop) NutrientPrepared = true;
        }

        public void ClearNutrientPrepared() => NutrientPrepared = false;

        public void Plant(CropInstance crop)
        {
            if (!CanPlant()) return;
            Crop = crop;
            SoilState = SoilState.Planted;
            WaterState = WaterState.Dry;
        }

        public void SetWaterState(WaterState state) => WaterState = state;

        public void MarkGrowing()
        {
            if (SoilState == SoilState.Planted) SoilState = SoilState.Growing;
        }

        public void MarkReady()
        {
            if (HasCrop) SoilState = SoilState.ReadyToHarvest;
        }

        /// <summary>Returns the harvested crop and resets the tile to Prepared.</summary>
        public CropInstance TakeHarvest()
        {
            if (!CanHarvest()) return null;
            var crop = Crop;
            Crop = null;
            SoilState = SoilState.Prepared; // ready for the next planting
            WaterState = WaterState.Dry;
            return crop;
        }

        // ---- save support ----
        public void Restore(SoilState soil, CropInstance crop, WaterState water)
        {
            SoilState = soil;
            Crop = crop;
            WaterState = water;
        }
    }
}
