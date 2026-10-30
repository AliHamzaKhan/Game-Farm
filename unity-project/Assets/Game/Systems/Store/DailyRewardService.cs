using System;
using System.Collections.Generic;
using System.Globalization;
using FarmQuest.Core.Save;
using FarmQuest.Core.Services;
using FarmQuest.Core.Time;
using FarmQuest.Data;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Farming;
using FarmQuest.Systems.Inventory;
using FarmQuest.Systems.World;

namespace FarmQuest.Systems.Store
{
    /// <summary>
    /// LOGIC: reasons to come back, all free.
    /// - Daily login calendar (7-day cycle). A missed day pauses the streak; it never resets.
    /// - Daily free seeds with every claim.
    /// - Weekly challenges with coin/gem rewards.
    /// - Safety net: a player with no seeds, nothing growing, nothing to sell and
    ///   too few coins for the cheapest seed gets free seeds (once per day) — so
    ///   nobody ever needs an ad or a purchase to keep farming.
    /// </summary>
    public class DailyRewardService
    {
        private readonly StoreCatalog _catalog;
        private readonly ITimeService _time;

        private int _calendarIndex;
        private string _lastClaimDay = "";
        private string _lastRescueDay = "";
        private string _weekStamp = "";
        private readonly List<long> _weeklyProgress = new List<long>();
        private readonly List<bool> _weeklyClaimed = new List<bool>();
        private float _rescueCheckTimer;

        public DailyRewardService(StoreCatalog catalog, ITimeService time)
        {
            _catalog = catalog;
            _time = time;
            ResetWeekIfNeeded();
            GameEvents.CropHarvested += (crop, qty) => AddWeekly(ChallengeType.HarvestCrops, qty);
            GameEvents.OrderFulfilled += coins => AddWeekly(ChallengeType.FulfillOrders, 1);
            GameEvents.SaleCompleted += (id, qty, coins) => AddWeekly(ChallengeType.EarnFromSales, coins);
            GameEvents.CropCared += action => AddWeekly(ChallengeType.CareActions, 1);
            GameEvents.CityCompleted += (index, name) => AddWeekly(ChallengeType.CompleteCities, 1);
            _time.SecondTick += OnSecondTick;
        }

        private string Today => _time.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        private string WeekStamp
        {
            get
            {
                var now = _time.UtcNow;
                int week = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(now, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                return $"{now.Year}-W{week:00}";
            }
        }

        // ---------- daily login ----------

        public int CalendarLength => _catalog != null ? _catalog.loginCalendar.Count : 0;
        public int CalendarIndex => CalendarLength > 0 ? _calendarIndex % CalendarLength : 0;
        public bool CanClaimToday => CalendarLength > 0 && _lastClaimDay != Today;

        public int DailySeedCount() => _catalog != null ? _catalog.dailyFreeSeeds : 0;

        public StoreCatalog Catalog => _catalog;

        public RewardContents TodaysReward => CalendarLength > 0 ? _catalog.loginCalendar[CalendarIndex] : null;

        /// <summary>Claims today's reward (doubled = the optional rewarded-ad bonus). Returns false if already claimed.</summary>
        public bool ClaimDaily(bool doubled = false)
        {
            if (!CanClaimToday) return false;
            int mult = doubled ? 2 : 1;
            Rewards.Grant(TodaysReward, "daily_login", mult);
            string seedCrop = DailySeedCropId();
            if (seedCrop != null && _catalog.dailyFreeSeeds > 0)
                ServiceLocator.Get<InventoryService>().Add("seed_" + seedCrop, _catalog.dailyFreeSeeds * mult);
            _lastClaimDay = Today;
            _calendarIndex = (_calendarIndex + 1) % CalendarLength;
            GameEvents.RaiseToast(doubled ? "Daily reward doubled!" : "Daily reward collected!");
            GameEvents.RaiseStoreChanged();
            return true;
        }

        /// <summary>Optional: watch an ad to double today's reward. If the ad isn't finished nothing is lost: the normal claim stays available.</summary>
        public void ClaimDailyWithAd(Action<bool> onDone)
        {
            if (!CanClaimToday || !ServiceLocator.TryGet(out Ads.RewardedAdService ads)) { onDone?.Invoke(false); return; }
            ads.ShowAd("daily_double", completed =>
            {
                // Not finished = no bonus, but today's normal reward stays claimable.
                onDone?.Invoke(completed && ClaimDaily(true));
            });
        }

        /// <summary>Crop for daily/rescue seeds: one the current city wants, else the cheapest available.</summary>
        public string DailySeedCropId()
        {
            var db = ServiceLocator.Get<CropDatabase>();
            if (ServiceLocator.TryGet(out WorldProgressionService world) && world.Current != null)
                foreach (var o in world.Current.City.objectives)
                    if (o.type == CityObjectiveType.HarvestCrop && db.GetCrop(o.itemId) != null) return o.itemId;
            var cheapest = CheapestAvailableCrop(db);
            return cheapest?.cropId;
        }

        private static CropData CheapestAvailableCrop(CropDatabase db)
        {
            CropData best = null;
            foreach (var c in db.crops)
            {
                if (c == null || c.isPremium || !WorldProgressionService.IsCropAvailable(c)) continue;
                if (best == null || c.seedPrice < best.seedPrice) best = c;
            }
            return best;
        }

        // ---------- safety net ----------

        private void OnSecondTick()
        {
            _rescueCheckTimer += 1f;
            if (_rescueCheckTimer < 5f) return;
            _rescueCheckTimer = 0f;
            ResetWeekIfNeeded();
            TryRescue();
        }

        /// <summary>True when the player cannot continue farming without help.</summary>
        public bool IsStuck()
        {
            if (!ServiceLocator.TryGet(out InventoryService inventory) ||
                !ServiceLocator.TryGet(out EconomyService economy) ||
                !ServiceLocator.TryGet(out CropDatabase db)) return false;
            foreach (var slot in inventory.Slots)
            {
                if (slot.Count <= 0) continue;
                if (slot.ItemId.StartsWith("seed_")) return false;          // can plant
                var item = db.GetItem(slot.ItemId);
                if (item != null && item.sellPrice > 0) return false;        // can sell
            }
            if (ServiceLocator.TryGet(out FarmService farm))
                foreach (var tile in farm.AllTiles())
                    if (tile.HasCrop) return false;                          // something is growing
            var cheapest = CheapestAvailableCrop(db);
            return cheapest != null && economy.Coins < cheapest.seedPrice;
        }

        /// <summary>Grants free seeds when stuck, at most once per day. Returns true if granted.</summary>
        public bool TryRescue()
        {
            if (_catalog == null || _catalog.rescueSeeds <= 0 || _lastRescueDay == Today || !IsStuck()) return false;
            var crop = CheapestAvailableCrop(ServiceLocator.Get<CropDatabase>());
            if (crop == null) return false;
            ServiceLocator.Get<InventoryService>().Add("seed_" + crop.cropId, _catalog.rescueSeeds);
            _lastRescueDay = Today;
            GameEvents.RaiseToast($"Grandpa sent you {_catalog.rescueSeeds} free {crop.displayName} seeds!");
            return true;
        }

        // ---------- weekly challenges ----------

        public IReadOnlyList<WeeklyChallengeDef> WeeklyChallenges =>
            _catalog != null ? _catalog.weeklyChallenges : (IReadOnlyList<WeeklyChallengeDef>)Array.Empty<WeeklyChallengeDef>();

        public long WeeklyProgress(int i) => i >= 0 && i < _weeklyProgress.Count ? _weeklyProgress[i] : 0;
        public bool IsWeeklyClaimed(int i) => i >= 0 && i < _weeklyClaimed.Count && _weeklyClaimed[i];
        public bool IsWeeklyDone(int i) => i >= 0 && i < WeeklyChallenges.Count && WeeklyProgress(i) >= WeeklyChallenges[i].target;

        private void ResetWeekIfNeeded()
        {
            int count = _catalog != null ? _catalog.weeklyChallenges.Count : 0;
            string week = WeekStamp;
            if (_weekStamp == week && _weeklyProgress.Count == count) return;
            if (_weekStamp != week)
            {
                _weekStamp = week;
                _weeklyProgress.Clear();
                _weeklyClaimed.Clear();
            }
            while (_weeklyProgress.Count < count) _weeklyProgress.Add(0);
            while (_weeklyClaimed.Count < count) _weeklyClaimed.Add(false);
        }

        private void AddWeekly(ChallengeType type, long amount)
        {
            if (_catalog == null || amount <= 0) return;
            ResetWeekIfNeeded();
            bool changed = false;
            for (int i = 0; i < _catalog.weeklyChallenges.Count; i++)
            {
                var c = _catalog.weeklyChallenges[i];
                if (c.type != type || _weeklyProgress[i] >= c.target) continue;
                _weeklyProgress[i] = Math.Min(c.target, _weeklyProgress[i] + amount);
                changed = true;
            }
            if (changed) GameEvents.RaiseStoreChanged();
        }

        public bool ClaimWeekly(int i)
        {
            ResetWeekIfNeeded();
            if (!IsWeeklyDone(i) || IsWeeklyClaimed(i)) return false;
            _weeklyClaimed[i] = true;
            Rewards.Grant(WeeklyChallenges[i].reward, "weekly_challenge");
            GameEvents.RaiseToast($"Challenge complete: {WeeklyChallenges[i].title}!");
            GameEvents.RaiseStoreChanged();
            return true;
        }

        // ---------- save ----------
        public DailySaveData CaptureState() => new DailySaveData
        {
            calendarIndex = _calendarIndex,
            lastClaimDay = _lastClaimDay,
            lastRescueDay = _lastRescueDay,
            weekStamp = _weekStamp,
            weeklyProgress = new List<long>(_weeklyProgress),
            weeklyClaimed = new List<bool>(_weeklyClaimed)
        };

        public void RestoreState(DailySaveData data)
        {
            if (data == null) return;
            _calendarIndex = Math.Max(0, data.calendarIndex);
            _lastClaimDay = data.lastClaimDay ?? "";
            _lastRescueDay = data.lastRescueDay ?? "";
            _weekStamp = data.weekStamp ?? "";
            _weeklyProgress.Clear();
            _weeklyClaimed.Clear();
            if (data.weeklyProgress != null) _weeklyProgress.AddRange(data.weeklyProgress);
            if (data.weeklyClaimed != null) _weeklyClaimed.AddRange(data.weeklyClaimed);
            ResetWeekIfNeeded();
        }
    }
}
