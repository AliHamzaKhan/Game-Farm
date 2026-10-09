using System.Collections.Generic;
using FarmQuest.Core.Services;
using FarmQuest.Core.Time;
using FarmQuest.Data;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Inventory;
using FarmQuest.Systems.Machines;
using FarmQuest.Systems.Processing;
using NUnit.Framework;
using UnityEngine;

namespace FarmQuest.Tests
{
    /// <summary>Production queues: start/collect/cancel/save (§58).</summary>
    public class ProductionTests
    {
        private FakeTimeService _time;
        private ProductionService _production;
        private InventoryService _inventory;

        private ItemData MakeItem(string id, ItemCategory cat, int sell)
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.itemId = id;
            item.displayName = id;
            item.category = cat;
            item.maxStack = 99;
            item.sellPrice = sell;
            return item;
        }

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            _time = new FakeTimeService();
            ServiceLocator.Register<ITimeService>(_time);

            var cropDb = ScriptableObject.CreateInstance<CropDatabase>();
            cropDb.items = new List<ItemData>
            {
                MakeItem("crop_wheat", ItemCategory.Crop, 12),
                MakeItem("flour", ItemCategory.Food, 25),
            };
            cropDb.Initialize();

            var prog = ScriptableObject.CreateInstance<ProgressionData>();
            for (int i = 1; i <= 30; i++)
                prog.levels.Add(new LevelEntry { level = i, title = "L" + i, xpRequiredCumulative = (i - 1) * 100 });
            var progression = new ProgressionService(prog);
            progression.Restore(2900, 30);

            var machineDb = ScriptableObject.CreateInstance<MachineDatabase>();
            var flour = ScriptableObject.CreateInstance<RecipeData>();
            flour.recipeId = "flour";
            flour.displayName = "Flour";
            flour.buildingId = "mill";
            flour.unlockLevel = 1;
            flour.inputs.Add(new RecipeIngredient { itemId = "crop_wheat", count = 3 });
            flour.outputItemId = "flour";
            flour.outputCount = 2;
            flour.durationSeconds = 300f;
            flour.xpReward = 10;
            machineDb.recipes = new List<RecipeData> { flour };
            machineDb.Initialize();

            ServiceLocator.Register(new EconomyService());
            ServiceLocator.Register(progression);
            _inventory = new InventoryService(cropDb);
            ServiceLocator.Register(_inventory);

            _production = new ProductionService(machineDb, _time);
            ServiceLocator.Register(_production);
        }

        [TearDown]
        public void TearDown() => ServiceLocator.Clear();

        [Test]
        public void StartProduction_ConsumesInputs_AndQueues()
        {
            _inventory.Add("crop_wheat", 10);
            Assert.IsTrue(_production.StartProduction("flour"));
            Assert.AreEqual(7, _inventory.GetCount("crop_wheat"));
            Assert.AreEqual(1, _production.QueueFor("mill").Count);
            Assert.IsFalse(_production.QueueFor("mill")[0].IsReady);
        }

        [Test]
        public void StartProduction_FailsWithoutInputs()
        {
            _inventory.Add("crop_wheat", 2);
            Assert.IsFalse(_production.StartProduction("flour"));
            Assert.AreEqual(0, _production.QueueFor("mill").Count);
        }

        [Test]
        public void StartProduction_RespectsQueueCapacity()
        {
            _inventory.Add("crop_wheat", 30);
            Assert.IsTrue(_production.StartProduction("flour"));
            Assert.IsTrue(_production.StartProduction("flour"));
            Assert.IsFalse(_production.StartProduction("flour"), "3rd batch should fail: queue full");
        }

        [Test]
        public void Collect_AfterDuration_GivesOutput()
        {
            _inventory.Add("crop_wheat", 10);
            _production.StartProduction("flour");
            _time.AdvanceSeconds(301);
            Assert.AreEqual(1, _production.CollectBuilding("mill"));
            Assert.AreEqual(2, _inventory.GetCount("flour"));
            Assert.AreEqual(0, _production.QueueFor("mill").Count);
        }

        [Test]
        public void Collect_BeforeReady_GivesNothing()
        {
            _inventory.Add("crop_wheat", 10);
            _production.StartProduction("flour");
            Assert.AreEqual(0, _production.CollectBuilding("mill"));
            Assert.AreEqual(0, _inventory.GetCount("flour"));
        }

        [Test]
        public void Cancel_RefundsInputs()
        {
            _inventory.Add("crop_wheat", 10);
            _production.StartProduction("flour");
            var batch = _production.QueueFor("mill")[0];
            Assert.IsTrue(_production.CancelProduction(batch));
            Assert.AreEqual(10, _inventory.GetCount("crop_wheat"));
            Assert.AreEqual(0, _production.QueueFor("mill").Count);
        }

        [Test]
        public void Save_RoundTrips_Queue()
        {
            _inventory.Add("crop_wheat", 10);
            _production.StartProduction("flour");
            var saved = _production.CaptureState();

            var fresh = new ProductionService(ServiceLocator.Get<MachineDatabase>(), _time);
            fresh.RestoreState(saved);
            Assert.AreEqual(1, fresh.QueueFor("mill").Count);

            _time.AdvanceSeconds(301);
            Assert.AreEqual(1, fresh.ReadyCount("mill"));
        }
    }
}
