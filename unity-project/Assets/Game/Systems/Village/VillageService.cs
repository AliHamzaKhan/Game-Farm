using FarmQuest.Core.Save;
using FarmQuest.Core.Services;
using FarmQuest.Core.Time;
using FarmQuest.Systems.Economy;

namespace FarmQuest.Systems.Village
{
    /// <summary>
    /// LOGIC: the village (§20). Tracks day count for the weekly Market Day
    /// (+20% sell prices — set on the market pricing strategy) and exposes
    /// the NPC-run shops.
    /// </summary>
    public class VillageService
    {
        private readonly VillageDatabase _database;
        private int _dayCount;

        public bool IsMarketDay => _dayCount % 7 == 6;

        public VillageService(VillageDatabase database, ITimeService time)
        {
            _database = database;
            time.DayChanged += OnDayChanged;
        }

        public System.Collections.Generic.IReadOnlyList<ShopData> Shops => _database.shops;

        private void OnDayChanged()
        {
            _dayCount++;
            var market = ServiceLocator.Get<MarketService>();
            if (IsMarketDay)
            {
                market.SetMarketDayMultiplier(1.2f);
                GameEvents.RaiseToast("🎉 Market Day! Sell prices +20% today!");
            }
            else
            {
                market.SetMarketDayMultiplier(1f);
            }
        }

        /// <summary>Applies the current multiplier (e.g. after loading a save).</summary>
        public void RefreshMarketDay()
        {
            var market = ServiceLocator.Get<MarketService>();
            market.SetMarketDayMultiplier(IsMarketDay ? 1.2f : 1f);
        }

        // ---------- save ----------
        public VillageSaveData CaptureState()
        {
            return new VillageSaveData { dayCount = _dayCount };
        }

        public void RestoreState(VillageSaveData data)
        {
            _dayCount = data != null ? data.dayCount : 0;
            RefreshMarketDay();
        }
    }
}
