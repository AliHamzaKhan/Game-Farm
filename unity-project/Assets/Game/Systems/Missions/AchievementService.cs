using System.Collections.Generic;
using FarmQuest.Core.Save;
using FarmQuest.Core.Services;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Farming;

namespace FarmQuest.Systems.Missions
{
    /// <summary>
    /// Long-term achievements (§29): lifetime counters, one-time unlocks,
    /// trophy fanfare + rewards. Counters persist in AchievementSaveData.
    /// </summary>
    public class AchievementService
    {
        private readonly List<AchievementData> _definitions;
        private readonly HashSet<string> _unlocked = new HashSet<string>();

        public int TotalHarvests { get; private set; }
        public long TotalCoinsEarned { get; private set; }
        public int TotalItemsSold { get; private set; }
        public int MissionsCompleted { get; private set; }
        public int RegionsUnlocked { get; private set; }
        public int HighestStoryChapter { get; private set; }

        public AchievementService(List<AchievementData> definitions)
        {
            _definitions = definitions ?? new List<AchievementData>();
            GameEvents.CropHarvested += (crop, qty) => { TotalHarvests += qty; CheckAll(); };
            GameEvents.CoinsEarned += amount => { TotalCoinsEarned += amount; CheckAll(); };
            GameEvents.ItemsSold += (id, qty) => { TotalItemsSold += qty; CheckAll(); };
            GameEvents.MissionClaimed += id => { MissionsCompleted++; CheckAll(); };
            GameEvents.RegionUnlocked += id => { RegionsUnlocked++; CheckAll(); };
            GameEvents.LevelUp += level => CheckAll();
            GameEvents.StoryChapterCompleted += chapter => { HighestStoryChapter = chapter + 1; CheckAll(); };
        }

        private int CurrentLevel =>
            ServiceLocator.TryGet(out ProgressionService p) ? p.Level : 1;

        private void CheckAll()
        {
            foreach (var def in _definitions)
            {
                if (def == null || _unlocked.Contains(def.achievementId)) continue;
                if (IsMet(def)) Unlock(def);
            }
        }

        private bool IsMet(AchievementData def)
        {
            switch (def.type)
            {
                case AchievementType.TotalHarvests: return TotalHarvests >= def.threshold;
                case AchievementType.TotalCoinsEarned: return TotalCoinsEarned >= def.threshold;
                case AchievementType.TotalItemsSold: return TotalItemsSold >= def.threshold;
                case AchievementType.ReachLevel: return CurrentLevel >= def.threshold;
                case AchievementType.UnlockLand: return RegionsUnlocked >= def.threshold;
                case AchievementType.CompleteMissions: return MissionsCompleted >= def.threshold;
                case AchievementType.CompleteStoryChapter: return HighestStoryChapter >= def.threshold;
                default: return false;
            }
        }

        private void Unlock(AchievementData def)
        {
            _unlocked.Add(def.achievementId);
            var economy = ServiceLocator.Get<EconomyService>();
            economy.AddCoins(def.rewardCoins, "achievement");
            economy.AddXp(def.rewardXp, "achievement");
            GameEvents.RaiseAchievementUnlocked(def.achievementId);
            GameEvents.RaiseToast($"🏆 {def.title}! +{def.rewardCoins} coins");
        }

        public bool IsUnlocked(string achievementId) => _unlocked.Contains(achievementId);
        public int UnlockedCount => _unlocked.Count;
        public IReadOnlyList<AchievementData> Definitions => _definitions;

        // ---------- save ----------
        public AchievementSaveData CaptureState()
        {
            return new AchievementSaveData
            {
                unlocked = new List<string>(_unlocked),
                totalHarvests = TotalHarvests,
                totalCoinsEarned = (int)System.Math.Min(int.MaxValue, TotalCoinsEarned),
                totalItemsSold = TotalItemsSold,
                missionsCompleted = MissionsCompleted,
                regionsUnlocked = RegionsUnlocked,
                highestStoryChapter = HighestStoryChapter
            };
        }

        public void RestoreState(AchievementSaveData data)
        {
            _unlocked.Clear();
            if (data == null) return;
            foreach (var id in data.unlocked) _unlocked.Add(id);
            TotalHarvests = data.totalHarvests;
            TotalCoinsEarned = data.totalCoinsEarned;
            TotalItemsSold = data.totalItemsSold;
            MissionsCompleted = data.missionsCompleted;
            RegionsUnlocked = data.regionsUnlocked;
            HighestStoryChapter = data.highestStoryChapter;
        }
    }
}
