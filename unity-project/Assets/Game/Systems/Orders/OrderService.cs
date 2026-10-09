using System;
using System.Collections.Generic;
using FarmQuest.Core.Save;
using FarmQuest.Core.Services;
using FarmQuest.Core.Time;
using FarmQuest.Data;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Inventory;

namespace FarmQuest.Systems.Orders
{
    [Serializable]
    public class OrderItem
    {
        public string itemId;
        public int count;
    }

    /// <summary>One delivery order on the order board (§20).</summary>
    public class DeliveryOrder
    {
        public string OrderId;
        public List<OrderItem> Items = new List<OrderItem>();
        public int RewardCoins;
        public int RewardXp;
        public long ExpiresAtTicks;
        public bool IsPremium;

        public bool IsExpired(ITimeService time) => time.UtcNow.Ticks >= ExpiresAtTicks;

        public double HoursLeft(ITimeService time) =>
            Math.Max(0, (ExpiresAtTicks - time.UtcNow.Ticks) / (double)TimeSpan.TicksPerHour);
    }

    /// <summary>
    /// LOGIC: the order board (§20). Generates delivery orders on a timer,
    /// expires old ones, fulfills from inventory. Premium orders pay more
    /// for processed goods — the "sell vs process" decision.
    /// </summary>
    public class OrderService
    {
        public const int MaxActiveOrders = 3;
        private const double GenerationIntervalHours = 4.0;
        private const double ExpiryHours = 24.0;

        private readonly CropDatabase _database;
        private readonly ITimeService _time;
        private readonly List<DeliveryOrder> _orders = new List<DeliveryOrder>();
        private readonly Random _rng = new Random();
        private long _lastGenerationTicks;

        public IReadOnlyList<DeliveryOrder> Orders => _orders;

        public OrderService(CropDatabase database, ITimeService time)
        {
            _database = database;
            _time = time;
            _lastGenerationTicks = _time.UtcNow.Ticks;
            _time.SecondTick += Poll;
        }

        private void Poll()
        {
            bool changed = false;
            for (int i = _orders.Count - 1; i >= 0; i--)
                if (_orders[i].IsExpired(_time))
                {
                    _orders.RemoveAt(i);
                    changed = true;
                }
            double hoursSinceGen = _time.GetElapsedSeconds(_lastGenerationTicks) / 3600.0;
            if (hoursSinceGen >= GenerationIntervalHours && _orders.Count < MaxActiveOrders)
            {
                GenerateOrder();
                _lastGenerationTicks = _time.UtcNow.Ticks;
                changed = true;
            }
            if (changed) GameEvents.RaiseOrdersChanged();
        }

        /// <summary>Ensures the board isn't empty on first open.</summary>
        public void EnsureSeeded()
        {
            if (_orders.Count == 0)
            {
                GenerateOrder();
                GenerateOrder();
                GameEvents.RaiseOrdersChanged();
            }
        }

        private List<ItemData> SellableItems()
        {
            var list = new List<ItemData>();
            foreach (var item in _database.items)
                if (item != null && item.sellPrice > 0 && item.category != ItemCategory.Seed)
                    list.Add(item);
            return list;
        }

        private void GenerateOrder()
        {
            var pool = SellableItems();
            if (pool.Count == 0) return;

            var order = new DeliveryOrder
            {
                OrderId = Guid.NewGuid().ToString("N"),
                IsPremium = _rng.NextDouble() < 0.25,
                ExpiresAtTicks = _time.UtcNow.Ticks + (long)(ExpiryHours * TimeSpan.TicksPerHour)
            };

            int itemCount = _rng.Next(2, 4);
            var used = new HashSet<string>();
            int baseValue = 0;
            for (int i = 0; i < itemCount && used.Count < pool.Count; i++)
            {
                var item = pool[_rng.Next(pool.Count)];
                if (!used.Add(item.itemId)) continue;
                int qty = _rng.Next(3, 9);
                order.Items.Add(new OrderItem { itemId = item.itemId, count = qty });
                baseValue += item.sellPrice * qty;
            }
            if (order.Items.Count == 0) return;

            float mult = order.IsPremium ? 2.2f : 1.5f;
            order.RewardCoins = Math.Max(10, (int)(baseValue * mult));
            order.RewardXp = 10 * order.Items.Count;
            _orders.Add(order);
        }

        public bool CanFulfill(DeliveryOrder order)
        {
            if (order == null) return false;
            var inventory = ServiceLocator.Get<InventoryService>();
            foreach (var item in order.Items)
                if (inventory.GetCount(item.itemId) < item.count) return false;
            return true;
        }

        public bool Fulfill(DeliveryOrder order)
        {
            if (!_orders.Contains(order) || !CanFulfill(order)) return false;
            var inventory = ServiceLocator.Get<InventoryService>();
            foreach (var item in order.Items)
                inventory.TryRemove(item.itemId, item.count);
            var economy = ServiceLocator.Get<EconomyService>();
            economy.AddCoins(order.RewardCoins, "order");
            economy.AddXp(order.RewardXp, "order");
            _orders.Remove(order);
            GameEvents.RaiseOrdersChanged();
            GameEvents.RaiseToast(order.IsPremium
                ? $"⭐ Premium delivery! +{order.RewardCoins} coins!"
                : $"🚚 Delivered! +{order.RewardCoins} coins!");
            return true;
        }

        public bool Cancel(DeliveryOrder order)
        {
            if (order == null || !_orders.Remove(order)) return false;
            GameEvents.RaiseOrdersChanged();
            return true;
        }

        // ---------- save ----------
        public OrderSaveData CaptureState()
        {
            var data = new OrderSaveData { lastGenerationTicks = _lastGenerationTicks };
            foreach (var o in _orders)
            {
                var entry = new OrderSaveEntry
                {
                    orderId = o.OrderId,
                    items = new List<OrderItemSave>(),
                    rewardCoins = o.RewardCoins,
                    rewardXp = o.RewardXp,
                    expiresAtTicks = o.ExpiresAtTicks,
                    isPremium = o.IsPremium
                };
                foreach (var item in o.Items)
                    entry.items.Add(new OrderItemSave { itemId = item.itemId, count = item.count });
                data.orders.Add(entry);
            }
            return data;
        }

        public void RestoreState(OrderSaveData data)
        {
            _orders.Clear();
            if (data == null) return;
            _lastGenerationTicks = data.lastGenerationTicks;
            foreach (var entry in data.orders)
            {
                var order = new DeliveryOrder
                {
                    OrderId = entry.orderId,
                    RewardCoins = entry.rewardCoins,
                    RewardXp = entry.rewardXp,
                    ExpiresAtTicks = entry.expiresAtTicks,
                    IsPremium = entry.isPremium
                };
                foreach (var item in entry.items)
                    order.Items.Add(new OrderItem { itemId = item.itemId, count = item.count });
                if (!order.IsExpired(_time)) _orders.Add(order);
            }
        }
    }
}
