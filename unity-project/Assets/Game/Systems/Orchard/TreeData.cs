using UnityEngine;

namespace FarmQuest.Systems.Orchard
{
    /// <summary>Data-driven fruit tree (§16 spec / orchard land). Planted once, regrows.</summary>
    [CreateAssetMenu(fileName = "Tree_New", menuName = "FarmQuest/Tree Data")]
    public class TreeData : ScriptableObject
    {
        public string treeId = "apple_tree";
        public string displayName = "Apple Tree";
        public int unlockLevel = 17;
        public int saplingCost = 250;
        [Tooltip("Crop id of the fruit, e.g. 'apple' (uses CropData yield/prices).")]
        public string fruitCropId = "apple";
        [Tooltip("Seconds between harvests.")]
        public float regrowIntervalSeconds = 2700f;
        public int xpReward = 20;
        public Sprite icon;
        public GameObject prefab;
    }
}
