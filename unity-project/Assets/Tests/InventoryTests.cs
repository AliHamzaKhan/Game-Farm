using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace FarmQuest.Tests
{
    /// <summary>Inventory add/remove/stack behavior (§58).</summary>
    public class InventoryTests
    {
        private InventoryService _inventory;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            var db = ScriptableObject.CreateInstance<CropDatabase>();
            var seed = ScriptableObject.CreateInstance<ItemData>();
            seed.itemId = "seed_carrot";
            seed.displayName = "Carrot Seeds";
            seed.category = ItemCategory.Seed;
            seed.maxStack = 99;
            db.items = new System.Collections.Generic.List<ItemData> { seed };
            db.Initialize();
            _inventory = new InventoryService(db);
        }

        [TearDown]
        public void TearDown() => ServiceLocator.Clear();

        [Test]
        public void Add_IncreasesCount()
        {
            _inventory.Add("seed_carrot", 5);
            Assert.AreEqual(5, _inventory.GetCount("seed_carrot"));
        }

        [Test]
        public void Add_SplitsAcrossMaxStack()
        {
            _inventory.Add("seed_carrot", 250);
            Assert.AreEqual(250, _inventory.GetCount("seed_carrot"));
            Assert.AreEqual(3, _inventory.Slots.Count); // 99 + 99 + 52
        }

        [Test]
        public void TryRemove_DecreasesCount()
        {
            _inventory.Add("seed_carrot", 5);
            Assert.IsTrue(_inventory.TryRemove("seed_carrot", 2));
            Assert.AreEqual(3, _inventory.GetCount("seed_carrot"));
        }

        [Test]
        public void TryRemove_Fails_WhenInsufficient()
        {
            _inventory.Add("seed_carrot", 2);
            Assert.IsFalse(_inventory.TryRemove("seed_carrot", 5));
            Assert.AreEqual(2, _inventory.GetCount("seed_carrot")); // unchanged
        }

        [Test]
        public void Quality_StacksDoNotMerge()
        {
            _inventory.Add("seed_carrot", 3, QualityGrade.Normal);
            _inventory.Add("seed_carrot", 3, QualityGrade.Excellent);
            Assert.AreEqual(2, _inventory.Slots.Count);
            Assert.AreEqual(6, _inventory.GetCount("seed_carrot"));
        }

        [Test]
        public void Save_RoundTrips()
        {
            _inventory.Add("seed_carrot", 7, QualityGrade.Good);
            var saved = _inventory.CaptureState();
            var fresh = new InventoryService(null);
            // null database is fine for restore-only usage in this test
            fresh.RestoreState(saved);
            Assert.AreEqual(7, fresh.GetCount("seed_carrot"));
        }
    }
}
