using System.Collections.Generic;
using System.Linq;
using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Missions;
using NUnit.Framework;
using UnityEngine;

namespace FarmQuest.Tests
{
    /// <summary>Daily missions: generation, progress, completion, claiming (§58).</summary>
    public class MissionTests
    {
        private MissionService _missions;
        private EconomyService _economy;

        private MissionData MakeMission(string id, MissionType type, int count, int coins, int xp)
        {
            var m = ScriptableObject.CreateInstance<MissionData>();
            m.missionId = id;
            m.type = type;
            m.targetCount = count;
            m.rewardCoins = coins;
            m.rewardXp = xp;
            return m;
        }

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            var prog = ScriptableObject.CreateInstance<ProgressionData>();
            prog.levels.Add(new LevelEntry { level = 1, title = "New Farmer", xpRequiredCumulative = 0 });
            _economy = new EconomyService();
            var progression = new ProgressionService(prog);
            ServiceLocator.Register(_economy);
            ServiceLocator.Register(progression);

            var pool = new List<MissionData>
            {
                MakeMission("harvest_5", MissionType.HarvestCrops, 5, 50, 30),
                MakeMission("water_10", MissionType.WaterCrops, 10, 60, 35),
                MakeMission("earn_500", MissionType.EarnCoins, 500, 120, 60),
            };
            _missions = new MissionService(pool);
            ServiceLocator.Register(_missions);
            _missions.EnsureDailyMissions();
        }

        [TearDown]
        public void TearDown() => ServiceLocator.Clear();

        [Test]
        public void DailyMissions_AreGenerated()
        {
            Assert.AreEqual(3, _missions.Active.Count);
        }

        [Test]
        public void Progress_TracksHarvestEvents()
        {
            var crop = ScriptableObject.CreateInstance<CropData>();
            crop.cropId = "carrot";
            var time = new FakeTimeService();
            var instance = new Systems.Farming.CropInstance(crop, time, time.Now.Ticks);

            GameEvents.RaiseCropHarvested(instance, 3);
            // Position-independent: the daily shuffle is day-seeded.
            var mission = _missions.Active.First(m => m.Data.type == MissionType.HarvestCrops);
            Assert.AreEqual(3, mission.Progress);
            Assert.IsFalse(mission.Completed);

            GameEvents.RaiseCropHarvested(instance, 5); // capped at target
            Assert.AreEqual(5, mission.Progress);
            Assert.IsTrue(mission.Completed);
        }

        [Test]
        public void Claim_GrantsRewards_Once()
        {
            var crop = ScriptableObject.CreateInstance<CropData>();
            crop.cropId = "carrot";
            var time = new FakeTimeService();
            var instance = new Systems.Farming.CropInstance(crop, time, time.Now.Ticks);
            GameEvents.RaiseCropHarvested(instance, 5);

            Assert.IsTrue(_missions.Claim("harvest_5"));
            Assert.AreEqual(50, _economy.Coins);
            Assert.IsFalse(_missions.Claim("harvest_5")); // no double-claim
            Assert.AreEqual(50, _economy.Coins);
        }

        [Test]
        public void Claim_Fails_WhenIncomplete()
        {
            Assert.IsFalse(_missions.Claim("harvest_5"));
        }

        [Test]
        public void EarnCoins_Mission_TracksCoins()
        {
            _economy.AddCoins(200, "test");
            _economy.AddCoins(300, "test");
            var mission = _missions.Active.First(m => m.Data.type == MissionType.EarnCoins);
            Assert.AreEqual(500, mission.Progress);
            Assert.IsTrue(mission.Completed);
        }

        [Test]
        public void Save_RoundTrips_Progress()
        {
            var crop = ScriptableObject.CreateInstance<CropData>();
            crop.cropId = "carrot";
            var time = new FakeTimeService();
            GameEvents.RaiseCropHarvested(new Systems.Farming.CropInstance(crop, time, time.Now.Ticks), 2);

            var saved = _missions.CaptureState();
            var fresh = new MissionService(new List<MissionData>
            {
                MakeMission("harvest_5", MissionType.HarvestCrops, 5, 50, 30),
            });
            fresh.RestoreState(saved);
            // Same-day stamp restores progress instead of regenerating.
            Assert.AreEqual(1, fresh.Active.Count);
            Assert.AreEqual(2, fresh.Active[0].Progress);
        }
    }
}
