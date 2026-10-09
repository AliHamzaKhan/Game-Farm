using UnityEngine;

namespace FarmQuest.Systems.Events
{
    /// <summary>Calendar live event (§44): monthly festivals with gentle bonuses.</summary>
    [CreateAssetMenu(fileName = "GameEvent_New", menuName = "FarmQuest/Game Event Data")]
    public class GameEventData : ScriptableObject
    {
        public string eventId = "spring_festival";
        public string displayName = "Spring Festival";
        [Range(1, 12)] public int month = 3;
        [Range(1, 31)] public int startDay = 20;
        [Range(1, 31)] public int endDay = 27;
        [TextArea] public string description = "Celebrate the new season!";
        [Tooltip("Multiplicative sell-price bonus while active (1 = none).")]
        public float sellPriceBonus = 1.1f;
        [Tooltip("XP multiplier while active (1 = none).")]
        public float xpMultiplier = 1.5f;

        public bool IsActiveNow()
        {
            var now = System.DateTime.Now;
            if (now.Month != month) return false;
            return now.Day >= startDay && now.Day <= endDay;
        }
    }
}
