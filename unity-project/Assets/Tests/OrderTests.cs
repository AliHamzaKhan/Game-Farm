using System.Collections.Generic;
using FarmQuest.Core.Services;
using FarmQuest.Core.Time;
using FarmQuest.Data;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Inventory;
using FarmQuest.Systems.Orders;
using NUnit.Framework;
using UnityEngine;

namespace FarmQuest.Tests
{
    /// <summary>Order board: generation, fulfilment, expiry, save (§58).</summary>
    public class OrderTests
    {
        private FakeTimeService _time;
        private OrderService _orders;
        private InventoryService _inventory;
        private EconomyService _economy;

        private ItemData MakeItem(string id, ItemCategory cat, int sell)
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.itemId = id;
            item.displayName = id;
            item.category = cat;
            item.maxStack = 999;
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
                MakeItem("crop_corn", ItemCategory.Crop, 15),
                MakeItem("egg", ItemCategory.AnimalProduct, 15),
                MakeItem("flour", ItemCategory.Food, 25),
            };
            cropDb.Initialize();

            var prog = ScriptableObject.CreateInstance<ProgressionData>();
            prog.levels.Add(new LevelEntry { level = 1, title = "N", xpRequiredCumulative = 0 });

            _economy = new EconomyService();
            ServiceLocator.Register(_economy);
            ServiceLocator.Register(new ProgressionService(prog));
            _inventory = new InventoryService(cropDb);
            ServiceLocator.Register(_inventory);

            _orders = new OrderService(cropDb, _time);
            ServiceLocator.Register(_orders);
        }

        [TearDown]
        public void TearDown() => ServiceLocator.Clear();

        [Test]
        public void EnsureSeeded_CreatesOrders()
        {
            Assert.AreEqual(0, _orders.Orders.Count);
            _orders.EnsureSeeded();
            Assert.Greater(_orders.Orders.Count, 0);
        }

        [Test]
        public void Fulfill_RemovesItems_AndGrantsRewards()
        {
            _orders.EnsureSeeded();
            var order = _orders.Orders[0];
            // Stock every possible item so the order is fulfillable.
            foreach (var item in ServiceLocator.Get<CropDatabase>().items)
                _inventory.Add(item.itemId, 99);

            int countBefore = _orders.Orders.Count;
            int coinsBefore = _economy.Coins;
            Assert.IsTrue(_orders.CanFulfill(order));
            Assert.IsTrue(_orders.Fulfill(order));
            Assert.AreEqual(countBefore - 1, _orders.Orders.Count);
            Assert.Greater(_economy.Coins, coinsBefore);
        }

        [Test]
        public void Fulfill_FailsWithoutItems()
        {
            _orders.EnsureSeeded();
            var order = _orders.Orders[0];
            Assert.IsFalse(_orders.CanFulfill(order));
            Assert.IsFalse(_orders.Fulfill(order));
            Assert.IsTrue(_orders.Orders.Contains(order));
        }

        [Test]
        public void ExpiredOrders_RemovedOnTick()
        {
            _orders.EnsureSeeded();
            var oldIds = new HashSet<string>();
            foreach (var o in _orders.Orders) oldIds.Add(o.OrderId);
            Assert.Greater(oldIds.Count, 0);
            _time.AdvanceSeconds(25 * 3600); // past the 24h expiry
            _time.RaiseTick(); // triggers OrderService.Poll
            foreach (var o in _orders.Orders)
                Assert.IsFalse(oldIds.Contains(o.OrderId), "expired order should be gone");
        }

        [Test]
        public void Restore_FiltersExpiredOrders()
        {
            _orders.EnsureSeeded();
            var saved = _orders.CaptureState();
            // Expire the saved orders manually.
            foreach (var o in saved.orders)
                o.expiresAtTicks = _time.UtcNow.Ticks - 1;

            var fresh = new OrderService(ServiceLocator.Get<CropDatabase>(), _time);
            fresh.RestoreState(saved);
            Assert.AreEqual(0, fresh.Orders.Count);
        }
    }
}
