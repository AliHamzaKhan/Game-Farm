using System.Collections.Generic;
using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Machines;
using NUnit.Framework;
using UnityEngine;

namespace FarmQuest.Tests
{
    /// <summary>Equipment ownership, tractor stats, mode gating, irrigation (§58).</summary>
    public class EquipmentTests
    {
        private EquipmentService _equipment;

        private EquipmentData MakeEquipment(string id, EquipmentType type, int level, int cost,
            int radius = 0, float speed = 0, float waterInterval = 0, int waterCount = 0)
        {
            var e = ScriptableObject.CreateInstance<EquipmentData>();
            e.equipmentId = id;
            e.displayName = id;
            e.type = type;
            e.unlockLevel = level;
            e.cost = cost;
            e.workRadius = radius;
            e.speedMultiplier = speed;
            e.waterIntervalSeconds = waterInterval;
            e.waterCount = waterCount;
            return e;
        }

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            var db = ScriptableObject.CreateInstance<MachineDatabase>();
            db.equipment = new List<EquipmentData>
            {
                MakeEquipment("small_tractor", EquipmentType.Tractor, 15, 2500, radius: 1, speed: 1f),
                MakeEquipment("large_tractor", EquipmentType.Tractor, 23, 8000, radius: 2, speed: 1.2f),
                MakeEquipment("seeder", EquipmentType.Seeder, 15, 1200),
                MakeEquipment("sprayer", EquipmentType.Sprayer, 16, 900),
                MakeEquipment("harvester", EquipmentType.Harvester, 25, 10000),
                MakeEquipment("auto_planter", EquipmentType.Planter, 24, 6000),
                MakeEquipment("sprinkler", EquipmentType.Irrigation, 14, 1500, waterInterval: 90f, waterCount: 8),
                MakeEquipment("irrigation", EquipmentType.Irrigation, 20, 5000, waterInterval: 60f, waterCount: 20),
            };
            db.Initialize();

            var prog = ScriptableObject.CreateInstance<ProgressionData>();
            for (int i = 1; i <= 30; i++)
                prog.levels.Add(new LevelEntry { level = i, title = "L" + i, xpRequiredCumulative = (i - 1) * 100 });
            var progression = new ProgressionService(prog);
            progression.Restore(2900, 30);

            var economy = new EconomyService();
            economy.AddCoins(100000, "test");

            ServiceLocator.Register(economy);
            ServiceLocator.Register(progression);
            _equipment = new EquipmentService(db);
            ServiceLocator.Register(db);
            ServiceLocator.Register(_equipment);
        }

        [TearDown]
        public void TearDown() => ServiceLocator.Clear();

        [Test]
        public void BuyEquipment_GrantsOwnership_AndSpendsCoins()
        {
            Assert.IsTrue(_equipment.BuyEquipment("small_tractor"));
            Assert.IsTrue(_equipment.Has("small_tractor"));
            Assert.AreEqual(97500, ServiceLocator.Get<EconomyService>().Coins);
        }

        [Test]
        public void BuyEquipment_NoDoubleBuy()
        {
            Assert.IsTrue(_equipment.BuyEquipment("small_tractor"));
            Assert.IsFalse(_equipment.BuyEquipment("small_tractor"));
        }

        [Test]
        public void GetBestTractor_PicksLargestRadius()
        {
            _equipment.BuyEquipment("small_tractor");
            _equipment.BuyEquipment("large_tractor");
            Assert.AreEqual("large_tractor", _equipment.GetBestTractor().equipmentId);
        }

        [Test]
        public void Modes_GatedByAttachments()
        {
            _equipment.BuyEquipment("small_tractor");
            Assert.IsTrue(_equipment.IsModeUnlocked(TractorMode.Plow));
            Assert.IsFalse(_equipment.IsModeUnlocked(TractorMode.Plant));

            _equipment.BuyEquipment("seeder");
            Assert.IsTrue(_equipment.IsModeUnlocked(TractorMode.Plant));
            Assert.IsFalse(_equipment.IsModeUnlocked(TractorMode.Fertilize));

            _equipment.BuyEquipment("sprayer");
            Assert.IsTrue(_equipment.IsModeUnlocked(TractorMode.Fertilize));
            Assert.IsFalse(_equipment.IsModeUnlocked(TractorMode.Harvest));

            _equipment.BuyEquipment("harvester");
            Assert.IsTrue(_equipment.IsModeUnlocked(TractorMode.Harvest));
        }

        [Test]
        public void ModeRadius_AutoPlanter_WidensPlantMode()
        {
            _equipment.BuyEquipment("small_tractor");
            _equipment.BuyEquipment("seeder");
            Assert.AreEqual(1, _equipment.ModeRadius(TractorMode.Plant));
            _equipment.BuyEquipment("auto_planter");
            Assert.AreEqual(3, _equipment.ModeRadius(TractorMode.Plant));
        }

        [Test]
        public void BestIrrigation_PicksShortestInterval()
        {
            _equipment.BuyEquipment("sprinkler");
            Assert.AreEqual("sprinkler", _equipment.BestIrrigation().equipmentId);
            _equipment.BuyEquipment("irrigation");
            Assert.AreEqual("irrigation", _equipment.BestIrrigation().equipmentId);
        }

        [Test]
        public void Save_RoundTrips_Owned()
        {
            _equipment.BuyEquipment("small_tractor");
            _equipment.BuyEquipment("seeder");
            var saved = _equipment.CaptureState();

            var db = ServiceLocator.Get<MachineDatabase>();
            var fresh = new EquipmentService(db);
            fresh.RestoreState(saved);
            Assert.IsTrue(fresh.Has("small_tractor"));
            Assert.IsTrue(fresh.Has("seeder"));
            Assert.IsFalse(fresh.Has("harvester"));
        }
    }
}
