using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmQuest.Data
{
    public enum FarmUpgradeEffect
    {
        GrowthSpeed,    // crops grow faster: growth time x (1 - value * level)
        YieldBonus,     // harvests yield more: x (1 + value * level)
        PestResistance, // fewer pest attacks: chance x (1 - value * level)
        SeedSaver       // chance (value * level) to get the seed back when planting
    }

    [Serializable]
    public class FarmUpgradeDef
    {
        public string upgradeId = "fast_growth";
        public string displayName = "Rich Soil";
        [TextArea] public string description = "Crops grow faster.";
        public FarmUpgradeEffect effect;
        [Tooltip("Effect strength per level, e.g. 0.05 = 5% per level.")]
        public float valuePerLevel = 0.05f;
        public int maxLevel = 5;
        public long baseCost = 300;
        [Tooltip("Cost multiplier per level already owned.")]
        public float costGrowth = 1.8f;

        public long CostForLevel(int currentLevel) =>
            (long)Math.Round(baseCost * Math.Pow(costGrowth, currentLevel));
    }

    /// <summary>Data-driven farm upgrades (§7 of the world-tour spec). Add rows, no code.</summary>
    [CreateAssetMenu(fileName = "FarmUpgradeDatabase", menuName = "FarmQuest/Farm Upgrade Database")]
    public class FarmUpgradeDatabase : ScriptableObject
    {
        public List<FarmUpgradeDef> upgrades = new List<FarmUpgradeDef>();

        public FarmUpgradeDef Get(string id)
        {
            foreach (var u in upgrades)
                if (u != null && u.upgradeId == id) return u;
            return null;
        }
    }
}
