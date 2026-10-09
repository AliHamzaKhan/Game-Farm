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
}
