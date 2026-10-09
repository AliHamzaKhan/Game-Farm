using UnityEngine;

namespace FarmQuest.Systems.Missions
{
    public enum AchievementType
    {
        TotalHarvests,     // harvest N crops (lifetime)
        TotalCoinsEarned,  // earn N coins (lifetime)
        TotalItemsSold,    // sell N items (lifetime)
        ReachLevel,        // reach level N
        UnlockLand,        // unlock N regions (total)
        CompleteMissions,  // claim N mission rewards (lifetime)
        CompleteStoryChapter // finish story chapter N
    }

    /// <summary>Data-driven achievement (§29). One-time milestones, never random.</summary>
    [CreateAssetMenu(fileName = "Achievement_New", menuName = "FarmQuest/Achievement Data")]
    public class AchievementData : ScriptableObject
    {
        public string achievementId = "first_harvest";
        public string title = "First Harvest";
        [TextArea] public string description = "Harvest your first crop.";
        public AchievementType type = AchievementType.TotalHarvests;
        public int threshold = 1;
        public int rewardCoins = 100;
        public int rewardXp = 20;
    }
}
