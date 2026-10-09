using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Inventory;

namespace FarmQuest.Systems.Economy
{
    /// <summary>
    /// Pricing strategy seam (§19, §47): fixed prices for MVP; a dynamic
    /// strategy plugs in later without touching market code.
    /// </summary>
    public interface IPriceStrategy
    {
        int GetBuyPrice(ItemData item);
        int GetSellPrice(ItemData item, QualityGrade quality);
    }

    public class FixedPriceStrategy : IPriceStrategy
    {
        /// <summary>Global sell-price multiplier (e.g. Market Day +20%). Phase 5.</summary>
        public float SellPriceMultiplier { get; set; } = 1f;

        public int GetBuyPrice(ItemData item) => item.buyPrice;
        public int GetSellPrice(ItemData item, QualityGrade quality)
        {
            float mult = Farming.CropInstance.QualityPriceMultiplier(quality) * SellPriceMultiplier;
            return UnityEngine.Mathf.RoundToInt(item.sellPrice * mult);
        }
    }

    /// <summary>LOGIC: village market (§19). Buy seeds/supplies, sell harvest.</summary>
    public class MarketService
    {
        private readonly CropDatabase _database;
        private readonly IPriceStrategy _pricing;

        public MarketService(CropDatabase database, IPriceStrategy pricing)
        {
            _database = database;
            _pricing = pricing;
        }

        private float _marketDayMultiplier = 1f;
        private float _eventMultiplier = 1f;

        /// <summary>Sets the Market Day component of the sell-price multiplier.</summary>
        public void SetMarketDayMultiplier(float multiplier)
        {
            _marketDayMultiplier = multiplier;
            ApplyMultiplier();
        }

        /// <summary>Sets the live-event component of the sell-price multiplier.</summary>
        public void SetEventMultiplier(float multiplier)
        {
            _eventMultiplier = multiplier;
            ApplyMultiplier();
        }

        private void ApplyMultiplier()
        {
            if (_pricing is FixedPriceStrategy fixed_)
                fixed_.SellPriceMultiplier = _marketDayMultiplier * _eventMultiplier;
        }

        public bool BuySeed(string cropId, int quantity)
        {
            var crop = _database.GetCrop(cropId);
            if (crop == null || quantity <= 0) return false;
            var economy = ServiceLocator.Get<EconomyService>();
            int total = crop.seedPrice * quantity;
            if (!economy.TrySpend(total))
            {
                GameEvents.RaiseToast("Not enough coins!");
                return false;
            }
            ServiceLocator.Get<InventoryService>().Add("seed_" + cropId, quantity);
            economy.AddXp(1, "buy_seed");
            return true;
        }

        public bool BuyItem(string itemId, int quantity)
        {
            var item = _database.GetItem(itemId);
            if (item == null || quantity <= 0) return false;
            var economy = ServiceLocator.Get<EconomyService>();
            int total = _pricing.GetBuyPrice(item) * quantity;
            if (!economy.TrySpend(total))
            {
                GameEvents.RaiseToast("Not enough coins!");
                return false;
            }
            ServiceLocator.Get<InventoryService>().Add(itemId, quantity);
            return true;
        }

        /// <summary>Sells up to <paramref name="quantity"/> of an item (best price per quality stack).</summary>
        public int Sell(string itemId, int quantity)
        {
            var item = _database.GetItem(itemId);
            if (item == null || quantity <= 0) return 0;
            var inventory = ServiceLocator.Get<InventoryService>();
            var economy = ServiceLocator.Get<EconomyService>();

            int earned = 0;
            int remaining = quantity;
            // Sell highest quality first (best price).
            var slots = new System.Collections.Generic.List<InventorySlot>(inventory.Slots);
            slots.Sort((a, b) => b.Quality.CompareTo(a.Quality));
            foreach (var slot in slots)
            {
                if (remaining <= 0) break;
                if (slot.ItemId != itemId) continue;
                int take = System.Math.Min(slot.Count, remaining);
                if (inventory.TryRemove(itemId, take, slot.Quality))
                {
                    earned += _pricing.GetSellPrice(item, slot.Quality) * take;
                    remaining -= take;
                }
            }
            if (earned > 0)
            {
                economy.AddCoins(earned, "sell");
                economy.AddXp(System.Math.Max(1, earned / 10), "sell");
                GameEvents.RaiseItemsSold(itemId, quantity - remaining);
            }
            return earned;
        }
    }
}
