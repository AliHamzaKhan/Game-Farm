using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmQuest.Data
{
    [Serializable]
    public class LevelEntry
    {
        public int level = 1;
        public string title = "New Farmer";
        [Tooltip("Cumulative XP required to REACH this level.")]
        public int xpRequiredCumulative;
    }

    /// <summary>Data-driven XP/level table (§27). Generated for levels 1–30.</summary>
    [CreateAssetMenu(fileName = "Progression", menuName = "FarmQuest/Progression Data")]
    public class ProgressionData : ScriptableObject
    {
        public List<LevelEntry> levels = new List<LevelEntry>();

        public int MaxLevel => levels.Count;

        public int GetLevelForXp(int xp)
        {
            int level = 1;
            foreach (var entry in levels)
            {
                if (xp >= entry.xpRequiredCumulative) level = entry.level;
                else break;
            }
            return level;
        }

        public string GetTitle(int level)
        {
            foreach (var entry in levels)
                if (entry.level == level) return entry.title;
            return "Farmer";
        }

        public int GetXpForLevel(int level)
        {
            foreach (var entry in levels)
                if (entry.level == level) return entry.xpRequiredCumulative;
            return int.MaxValue;
        }
    }
}
