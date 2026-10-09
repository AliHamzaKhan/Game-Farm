using System.Collections.Generic;
using FarmQuest.Core.Services;
using FarmQuest.Core.Time;
using FarmQuest.Data;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Farming;
using FarmQuest.Systems.Inventory;
using FarmQuest.Systems.Machines;
using NUnit.Framework;
using UnityEngine;

namespace FarmQuest.Tests
{
    /// <summary>Tractor area-work: plow/plant/water/fertilize/harvest (§58).</summary>
    public class TractorWorkTests
    {
        private FakeTimeService _time;
        private FarmService _farm;
        private TractorWorkService _work;
        private InventoryService _inventory;

        private static readonly Vector2Int Center = new Vector2Int(4, 4);

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            _time = new FakeTimeService();
            ServiceLocator.Register<ITimeService>(_time);

            var layout = ScriptableObject.CreateInstance<FarmLayoutData>();
            layout.gridWidth = 8; layout.gridHeight = 8; layout.tileSize = 2f;
            layout.regions = new List<FarmRegion>
            {
                new FarmRegion { regionId = 0, regionName = "Home",
                    area = new RectInt(0, 0, 8, 8), unlockedByDefault = true }
            };

            var cropDb = ScriptableObject.CreateInstance<CropDatabase>();
            var carrot = ScriptableObject.CreateInstance<CropData>();
            carrot.cropId = "carrot";
            carrot.growthDurationSeconds = 60f;
            carrot.growthStages = 2;
            carrot.waterRequired = 1;
            carrot.harvestMin = 4; carrot.harvestMax = 4;
            carrot.xpReward = 8;
            var seed = ScriptableObject.CreateInstance<ItemData>();
            seed.itemId = "seed_carrot"; seed.maxStack = 99; seed.category = ItemCategory.Seed;
            var cropItem = ScriptableObject.CreateInstance<ItemData>();
            cropItem.itemId = "crop_carrot"; cropItem.maxStack = 99;
            cropItem.category = ItemCategory.Crop; cropItem.sellPrice = 6;
            var fert = ScriptableObject.CreateInstance<ItemData>();
            fert.itemId = "fertilizer"; fert.maxStack = 99;
            cropDb.crops = new List<CropData> { carrot };
            cropDb.items = new List<ItemData> { seed, cropItem, fert };
            cropDb.Initialize();

            var prog = ScriptableObject.CreateInstance<ProgressionData>();
            prog.levels.Add(new LevelEntry { level = 1, title = "N", xpRequiredCumulative = 0 });

            ServiceLocator.Register(new EconomyService());
            ServiceLocator.Register(new ProgressionService(prog));
            _farm = new FarmService(layout, cropDb, _time);
            ServiceLocator.Register(_farm);
            ServiceLocator.Register(new CropGrowthService(_time));
            _inventory = new InventoryService(cropDb);
            ServiceLocator.Register(_inventory);

            _work = new TractorWorkService(_farm);
        }

        [TearDown]
        public void TearDown() => ServiceLocator.Clear();

        private void PrepareArea()
        {
            _work.DoWork(Center, 1, TractorMode.Plow, "carrot"); // clear
            _work.DoWork(Center, 1, TractorMode.Plow, "carrot"); // dig
            _work.DoWork(Center, 1, TractorMode.Plow, "carrot"); // prepare
        }

        [Test]
        public void Plow_ProgressesSoil_ThroughStages()
        {
            var r1 = _work.DoWork(Center, 1, TractorMode.Plow, "carrot");
            Assert.AreEqual(9, r1.cleared);
            var r2 = _work.DoWork(Center, 1, TractorMode.Plow, "carrot");
            Assert.AreEqual(9, r2.dug);
            var r3 = _work.DoWork(Center, 1, TractorMode.Plow, "carrot");
            Assert.AreEqual(9, r3.prepared);
        }

        [Test]
        public void Plant_PlantsArea_AndConsumesSeeds()
        {
            PrepareArea();
            _inventory.Add("seed_carrot", 20);
            var r = _work.DoWork(Center, 1, TractorMode.Plant, "carrot");
            Assert.AreEqual(9, r.planted);
            Assert.AreEqual(11, _inventory.GetCount("seed_carrot"));
        }

        [Test]
        public void Water_WatersGrowingCrops()
        {
            PrepareArea();
            _inventory.Add("seed_carrot", 20);
            _work.DoWork(Center, 1, TractorMode.Plant, "carrot");
            var r = _work.DoWork(Center, 1, TractorMode.Water, "carrot");
            Assert.AreEqual(9, r.watered);
        }

        [Test]
        public void Fertilize_AppliesNutrients()
        {
            PrepareArea();
            _inventory.Add("seed_carrot", 20);
            _inventory.Add("fertilizer", 20);
            _work.DoWork(Center, 1, TractorMode.Plant, "carrot");
            var r = _work.DoWork(Center, 1, TractorMode.Fertilize, "carrot");
            Assert.AreEqual(9, r.fertilized);
            Assert.AreEqual(11, _inventory.GetCount("fertilizer"));
        }

        [Test]
        public void Harvest_CollectsReadyCrops()
        {
            PrepareArea();
            _inventory.Add("seed_carrot", 20);
            _work.DoWork(Center, 1, TractorMode.Plant, "carrot");
            _time.AdvanceSeconds(61); // crops ready
            var r = _work.DoWork(Center, 1, TractorMode.Harvest, "carrot");
            Assert.AreEqual(9, r.harvested);
            Assert.AreEqual(36, r.harvestQty); // 9 × 4
            Assert.AreEqual(36, _inventory.GetCount("crop_carrot"));
        }

        [Test]
        public void Work_IgnoresLockedTiles()
        {
            var tile = _farm.GetTile(new Vector2Int(0, 0));
            // Simulate a locked tile by flipping it directly.
            tile.IsUnlocked = false;
            var r = _work.DoWork(new Vector2Int(0, 0), 0, TractorMode.Plow, "carrot");
            Assert.AreEqual(0, r.TotalActions);
        }
    }
}
