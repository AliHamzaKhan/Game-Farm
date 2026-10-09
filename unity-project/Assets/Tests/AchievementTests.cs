using System.Collections.Generic;
using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Missions;
using NUnit.Framework;
using UnityEngine;

namespace FarmQuest.Tests
{
    /// <summary>Achievements: thresholds, one-time unlock, rewards (§58).</summary>
    public class AchievementTests
    {
        private AchievementService _achievements;
        private EconomyService _economy;

        private AchievementData MakeAchievement(string id, AchievementType type, int threshold, int coins)
        {
            var a = ScriptableObject.CreateInstance<AchievementData>();
            a.achievementId = id;
            a.title = id;
            a.type = type;
            a.threshold = threshold;
            a.rewardCoins = coins;
            a.rewardXp = 10;
            return a;
        }

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            var prog = ScriptableObject.CreateInstance<ProgressionData>();
            prog.levels.Add(new LevelEntry { level = 1, title = "New Farmer", xpRequiredCumulative = 0 });
            prog.levels.Add(new LevelEntry { level = 2, title = "Garden Farmer", xpRequiredCumulative = 100 });
            _economy = new EconomyService();
            ServiceLocator.Register(_economy);
            ServiceLocator.Register(new ProgressionService(prog));

            _achievements = new AchievementService(new List<AchievementData>
            {
                MakeAchievement("first_harvest", AchievementType.TotalHarvests, 1, 100),
                MakeAchievement("rich_100", AchievementType.TotalCoinsEarned, 100, 50),
            });
            ServiceLocator.Register(_achievements);
        }

        [TearDown]
        public void TearDown() => ServiceLocator.Clear();

        private static void RaiseHarvest(int qty)
        {
            var crop = ScriptableObject.CreateInstance<CropData>();
            crop.cropId = "carrot";
            var time = new FakeTimeService();
            GameEvents.RaiseCropHarvested(new Systems.Farming.CropInstance(crop, time, time.Now.Ticks), qty);
        }

        [Test]
        public void Unlock_Fires_AtThreshold_WithReward()
        {
            string unlocked = null;
            GameEvents.AchievementUnlocked += id => unlocked = id;

            RaiseHarvest(1);

            Assert.AreEqual("first_harvest", unlocked);
            Assert.IsTrue(_achievements.IsUnlocked("first_harvest"));
            Assert.AreEqual(100, _economy.Coins); // reward granted
        }

        [Test]
        public void Unlock_Happens_OnlyOnce()
        {
            RaiseHarvest(1);
            long afterFirst = _economy.Coins;
            RaiseHarvest(5);
            Assert.AreEqual(afterFirst, _economy.Coins); // no double reward
        }

        [Test]
        public void CoinsEarned_Tracks_Lifetime()
        {
            _economy.AddCoins(60, "test");
            Assert.IsFalse(_achievements.IsUnlocked("rich_100"));
            _economy.AddCoins(40, "test");
            Assert.IsTrue(_achievements.IsUnlocked("rich_100"));
        }

        [Test]
        public void Save_RoundTrips_Unlocks_And_Counters()
        {
            RaiseHarvest(3);
            var saved = _achievements.CaptureState();

            var fresh = new AchievementService(new List<AchievementData>
            {
                MakeAchievement("first_harvest", AchievementType.TotalHarvests, 1, 100),
            });
            fresh.RestoreState(saved);

            Assert.IsTrue(fresh.IsUnlocked("first_harvest"));
            Assert.AreEqual(3, fresh.TotalHarvests);
        }
    }
}
