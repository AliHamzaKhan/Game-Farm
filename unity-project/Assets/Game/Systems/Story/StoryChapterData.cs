using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmQuest.Systems.Story
{
    public enum ObjectiveType
    {
        HarvestAny,      // harvest N crops total
        EarnCoins,        // earn N coins total (lifetime while chapter active)
        ReachLevel,       // reach level N
        UnlockLand,       // have N regions unlocked total
        SellAny,          // sell N items total
        CompleteMissions  // claim N mission rewards total
    }

    [Serializable]
    public class ObjectiveData
    {
        public ObjectiveType type;
        public int targetCount;
        public string label = "Harvest 3 crops";
    }

    /// <summary>One story chapter (§30): objectives + rewards. Guides, never gates.</summary>
    [CreateAssetMenu(fileName = "Chapter_New", menuName = "FarmQuest/Story Chapter")]
    public class StoryChapterData : ScriptableObject
    {
        public int chapterIndex;
        public string title = "Wake Up, Farm!";
        [TextArea(2, 4)] public string description = "";
        public List<ObjectiveData> objectives = new List<ObjectiveData>();
        public int rewardCoins = 300;
        public int rewardXp = 50;
    }
}
