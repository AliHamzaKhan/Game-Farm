using UnityEngine;

namespace FarmQuest.Systems.Animals
{
    public enum AnimalHousing { Coop, Barn, Pasture, Pond, Stable, Hive }

    /// <summary>Data-driven animal definition (§21). New animals = new asset.</summary>
    [CreateAssetMenu(fileName = "Animal_New", menuName = "FarmQuest/Animal Data")]
    public class AnimalData : ScriptableObject
    {
        [Header("Identity")]
        public string animalId = "chicken";
        public string displayName = "Chicken";
        public int unlockLevel = 10;
        public int buyPrice = 150;
        public AnimalHousing housing = AnimalHousing.Coop;
        [Tooltip("Farm region that must be unlocked first (e.g. Animal Meadow).")]
        public int requiredRegionId = 2;

        [Header("Production")]
        [Tooltip("Item produced; empty = no product (e.g. horse).")]
        public string productItemId = "egg";
        [Tooltip("Seconds per product at neutral happiness.")]
        public float productionIntervalSeconds = 600f;
        public int maxStoredProducts = 4;
        [Tooltip("Seconds one feeding lasts.")]
        public float feedIntervalSeconds = 7200f;
        public int xpPerCollect = 6;

        [Header("Presentation")]
        public Sprite icon;
        public GameObject prefab;
        public AudioClip productSound;
    }
}
