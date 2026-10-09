using UnityEngine;

namespace FarmQuest.Data
{
    /// <summary>Data-driven item definition (§18). Seeds, crops, products, tools…</summary>
    [CreateAssetMenu(fileName = "Item_New", menuName = "FarmQuest/Item Data")]
    public class ItemData : ScriptableObject
    {
        public string itemId = "seed_carrot";
        public string displayName = "Carrot Seeds";
        public ItemCategory category = ItemCategory.Seed;
        public Sprite icon;
        public int maxStack = 99;
        public int buyPrice = 10;
        public int sellPrice;
        /// <summary>For seeds: which crop this plants. Empty for non-seeds.</summary>
        public string cropId;
    }
}
