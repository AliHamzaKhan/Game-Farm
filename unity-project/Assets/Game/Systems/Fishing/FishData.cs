using UnityEngine;

namespace FarmQuest.Systems.Fishing
{
    public enum FishRarity { Common, Uncommon, Rare, Legendary }

    /// <summary>Fish species (§26 spec). Caught fish are sellable items.</summary>
    [CreateAssetMenu(fileName = "Fish_New", menuName = "FarmQuest/Fish Data")]
    public class FishData : ScriptableObject
    {
        public string fishId = "carp";
        public string displayName = "Carp";
        public FishRarity rarity = FishRarity.Common;
        public int sellPrice = 20;
        public int xpReward = 8;
        public Sprite icon;
    }
}
