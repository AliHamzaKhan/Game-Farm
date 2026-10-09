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
        [Tooltip("Visual stages, e.g. seed/sprout/small/medium/mature/ready.")]
        public int growthStages = 6;
        public int waterRequired = 1;
        public bool compostRequiredForTopQuality = true;
        public Season preferredSeason = Season.None;

        [Header("Presentation")]
        public Sprite icon;
        [Tooltip("One prefab per growth stage (index 0 = seed).")]
        public GameObject[] stagePrefabs = new GameObject[0];
        public AudioClip plantSound;
        public AudioClip harvestSound;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(cropId)) cropId = name.ToLowerInvariant().Replace("crop_", "");
            if (harvestMax < harvestMin) harvestMax = harvestMin;
            if (growthStages < 2) growthStages = 2;
        }
    }
}
