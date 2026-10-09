using System.Collections.Generic;
using FarmQuest.Core.Services;
using FarmQuest.Systems.Economy;

namespace FarmQuest.Systems.Events
{
    /// <summary>
    /// LOGIC: monthly calendar events (§44). Applies sell-price and XP bonuses
    /// from active events. Combines cleanly with Market Day (multiplicative).
    /// </summary>
    public class LiveEventService
    {
        private readonly List<GameEventData> _events;

        public LiveEventService(List<GameEventData> events)
        {
            _events = events ?? new List<GameEventData>();
        }

        public IReadOnlyList<GameEventData> ActiveEvents()
        {
            var active = new List<GameEventData>();
            foreach (var e in _events)
                if (e != null && e.IsActiveNow()) active.Add(e);
            return active;
        }

        /// <summary>Recomputes bonuses from active events. Call on boot + daily.</summary>
        public void RefreshBonuses()
        {
            float sellBonus = 1f;
            float xpMult = 1f;
            foreach (var e in ActiveEvents())
            {
                sellBonus *= e.sellPriceBonus;
                xpMult *= e.xpMultiplier;
            }
            if (ServiceLocator.TryGet(out MarketService market))
                market.SetEventMultiplier(sellBonus);
            if (ServiceLocator.TryGet(out EconomyService economy))
                economy.XpMultiplier = xpMult;

            foreach (var e in ActiveEvents())
                GameEvents.RaiseToast($"🎪 {e.displayName} is on! {e.description}");
        }
    }
}
