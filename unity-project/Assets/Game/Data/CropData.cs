using UnityEngine;

namespace FarmQuest.Data
{
    /// <summary>
    /// Data-driven crop definition (§7). Add new crops by creating an asset —
    /// no code changes needed. Generated via FarmQuest/Generate/All Game Data.
    /// </summary>
    [CreateAssetMenu(fileName = "Crop_New", menuName = "FarmQuest/Crop Data")]
    public class CropData : ScriptableObject
    {
        [Header("Identity")]
        public string cropId = "carrot";
        public string displayName = "Carrot";
        public CropCategory category = CropCategory.Vegetable;
        public int unlockLevel = 1;

        [Header("Economy (v1 tuning values — see RemoteConfig/Balance)")]
        public int seedPrice = 10;
        public int sellPrice = 6;
        public int harvestMin = 4;
        public int harvestMax = 6;
        public int xpReward = 8;

        [Header("Growth")]
        [Tooltip("Seconds of real time from plant to ready.")]
        public float growthDurationSeconds = 120f;
        [Tooltip("Legacy stage count, kept for old assets. The lifecycle always has 6 GrowthStages.")]
        public int growthStages = 6;
        [Tooltip("Relative duration of Seed, Sprout, Vegetative, Flowering, Fruiting. " +
                 "Mature starts when the timer ends. Empty = equal stages.")]
        public float[] stageWeights = new float[0];
        public int waterRequired = 1;
        public bool compostRequiredForTopQuality = true;
        public Season preferredSeason = Season.None;

        [Header("Care (crop-specific; leave off what a crop doesn't need)")]
        [Tooltip("Seconds a watering lasts. 0 = only the initial waterings count (no drying out).")]
        public float waterIntervalSeconds = 0f;
        [Tooltip("Growth speed lost while dry (0.5 = grows at half speed). Crops never die.")]
        [Range(0f, 0.9f)] public float dryGrowthPenalty = 0.5f;
        [Tooltip("Needs fertilizer from the Vegetative stage; unfertilized harvests yield less.")]
        public bool needsFertilizer;
        [Tooltip("Nutrient wanted from the Flowering stage. Applied = bonus yield, missing = small penalty.")]
        public NutrientType requiredNutrient = NutrientType.None;
        [Tooltip("Can be pruned during Vegetative/Flowering for a yield bonus.")]
        public bool needsPruning;
        [Tooltip("Chance (0-1) a pest attack happens during growth. Spraying clears it.")]
        [Range(0f, 1f)] public float pestChance;

        [Header("Presentation")]
        public Color leafColor = new Color(0.3f, 0.72f, 0.28f);
        public Color flowerColor = new Color(1f, 0.95f, 0.5f);
        public Color produceColor = new Color(0.95f, 0.45f, 0.15f);
        public Sprite icon;
        [Tooltip("One prefab per growth stage (index 0 = seed).")]
        public GameObject[] stagePrefabs = new GameObject[0];
        public UnityEngine.AudioClip plantSound;
        public UnityEngine.AudioClip harvestSound;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(cropId)) cropId = name.ToLowerInvariant().Replace("crop_", "");
            if (harvestMax < harvestMin) harvestMax = harvestMin;
            if (growthStages < 2) growthStages = 2;
        }
    }
}
