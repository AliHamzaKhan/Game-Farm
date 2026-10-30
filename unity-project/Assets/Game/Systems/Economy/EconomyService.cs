using FarmQuest.Core.Save;
using FarmQuest.Core.Services;

namespace FarmQuest.Systems.Economy
{
    /// <summary>
    /// LOGIC: wallet (§28). Farm Coins are earned by farming and buy everything
    /// a player needs. Farm Gems are the premium currency: earned for free
    /// (cities, login calendar, weekly challenges) or bought; they buy
    /// convenience, premium seeds and cosmetics — never required to progress.
    /// </summary>
    public class EconomyService
    {
        public long Coins { get; private set; }
        public long Gems { get; private set; }

        public void AddGems(long amount, string reason = null)
        {
            if (amount <= 0) return;
            Gems += amount;
            GameEvents.RaiseGemsChanged(Gems);
        }

        public bool TrySpendGems(long amount)
        {
            if (amount <= 0) return true;
            if (Gems < amount) return false;
            Gems -= amount;
            GameEvents.RaiseGemsChanged(Gems);
            return true;
        }

        public void AddCoins(long amount, string reason = null)
        {
            if (amount <= 0) return;
            Coins += amount;
            GameEvents.RaiseCoinsChanged(Coins);
            GameEvents.RaiseCoinsEarned(amount);
        }

        public bool TrySpend(long amount)
        {
            if (amount <= 0) return true;
            if (Coins < amount) return false;
            Coins -= amount;
            GameEvents.RaiseCoinsChanged(Coins);
            return true;
        }

        /// <summary>XP multiplier from live events (Phase 6). 1 = normal.</summary>
        public float XpMultiplier { get; set; } = 1f;

        public void AddXp(int amount, string reason = null)
        {
            int adjusted = amount > 0
                ? System.Math.Max(1, UnityEngine.Mathf.RoundToInt(amount * XpMultiplier))
                : amount;
            if (ServiceLocator.TryGet(out ProgressionService progression))
                progression.AddXp(adjusted);
        }

        // ---------- save ----------
        public EconomySaveData CaptureState()
        {
            var progression = ServiceLocator.TryGet(out ProgressionService p) ? p : null;
            return new EconomySaveData
            {
                coins = Coins,
                gems = Gems,
                xp = progression != null ? progression.Xp : 0,
                level = progression != null ? progression.Level : 1
            };
        }

        public void RestoreState(EconomySaveData data)
        {
            Coins = data != null ? data.coins : 0;
            Gems = data != null ? System.Math.Max(0, data.gems) : 0;
            GameEvents.RaiseCoinsChanged(Coins);
            GameEvents.RaiseGemsChanged(Gems);
            if (data != null && ServiceLocator.TryGet(out ProgressionService p))
                p.Restore(data.xp, data.level);
        }
    }
}
