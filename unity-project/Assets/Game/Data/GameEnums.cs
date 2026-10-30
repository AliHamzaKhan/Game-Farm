namespace FarmQuest.Data
{
    public enum CropCategory { Vegetable, Fruit, Special }
    public enum Season { None, Spring, Summer, Autumn, Winter }
    public enum ItemCategory { Seed, Crop, AnimalProduct, Food, Fertilizer, Compost, Tool, Equipment, Decoration, Material }

    /// <summary>Quality grades (§8 GDD / §14 spec). Ordered low → high.</summary>
    public enum QualityGrade { Normal = 0, Good = 1, Excellent = 2, Premium = 3 }

    public enum SoilState
    {
        Grass = 0,        // untouched
        Cleared = 1,      // debris removed
        Dug = 2,          // dug up
        Prepared = 3,     // raked, ready for seeds
        Planted = 4,      // seed in ground
        Growing = 5,      //alias: Planted but past sprout; kept for clarity in views
        ReadyToHarvest = 6,
        Harvested = 7     // briefly shown, then returns to Prepared
    }

    public enum WaterState { Dry = 0, Moist = 1, WellWatered = 2 }

    /// <summary>
    /// Crop lifecycle. Seed..Fruiting span the growth timer (per-crop weights);
    /// Mature means the timer has finished and the crop can be harvested.
    /// </summary>
    public enum GrowthStage { Seed = 0, Sprout = 1, Vegetative = 2, Flowering = 3, Fruiting = 4, Mature = 5 }

    /// <summary>What a crop needs right now, highest priority first in CropInstance.Health.</summary>
    public enum CropHealth
    {
        Healthy = 0,
        NeedsWater = 1,
        NeedsFertilizer = 2,
        NutrientDeficiency = 3,
        PestAttack = 4,
        NeedsPruning = 5,
        ReadyToHarvest = 6
    }

    /// <summary>Crop-specific nutrient. Each maps to an inventory item "nutrient_&lt;name&gt;".</summary>
    public enum NutrientType { None = 0, Nitrogen = 1, Potassium = 2, Calcium = 3 }
}
