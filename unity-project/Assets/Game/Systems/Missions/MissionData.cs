using UnityEngine;

namespace FarmQuest.Systems.Missions
{
    public enum MissionType
    {
        HarvestCrops,  // harvest N crops (optionally of one type)
        WaterCrops,    // water N times
        PlantSeeds,    // plant N seeds
        SellCrops,     // sell N harvested items
        EarnCoins      // earn N coins from any source
    }

    /// <summary>Data-driven daily mission definition (§29). New missions = new asset.</summary>
    [CreateAssetMenu(fileName = "Mission_New", menuName = "FarmQuest/Mission Data")]
    public class MissionData : ScriptableObject
    {
        public string missionId = "harvest_5";
        public MissionType type = MissionType.HarvestCrops;
        [Tooltip("Crop id filter, e.g. 'carrot'. Empty = any crop.")]
        public string targetCropId = "";
        public string targetCropName = "";
        public int targetCount = 5;
        public int rewardCoins = 50;
        public int rewardXp = 30;
        public int minLevel = 1;

        public string GetTitle()
        {
            string what = string.IsNullOrEmpty(targetCropName) ? "crops" : targetCropName + "s";
            switch (type)
            {
                case MissionType.HarvestCrops: return $"Harvest {targetCount} {what}";
                case MissionType.WaterCrops: return $"Water crops {targetCount} times";
                case MissionType.PlantSeeds: return $"Plant {targetCount} seeds";
                case MissionType.SellCrops: return $"Sell {targetCount} {what}";
                case MissionType.EarnCoins: return $"Earn {targetCount} coins";
                default: return missionId;
            }
        }
    }
}
