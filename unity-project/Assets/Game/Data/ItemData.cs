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

        [Header("Free-to-play economy")]
        public ItemTier tier = ItemTier.Basic;
        [Tooltip("Gem price; 0 = not sold for gems. buyPrice 0 = not sold for coins.")]
        public int gemPrice;
        [Tooltip("What using this item on a crop does (consumables).")]
        public ConsumableEffect effect = ConsumableEffect.None;
        [Tooltip("Strength of the effect, e.g. 0.25 = 25% of the growth timer.")]
        public float effectValue;
        [Tooltip("World-tour level (0 = Karachi) from which shops offer it.")]
        public int unlockCityIndex;
        [TextArea] public string benefit = "";
    }
}
