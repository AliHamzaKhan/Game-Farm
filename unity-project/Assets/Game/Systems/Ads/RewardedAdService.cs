using System;
using FarmQuest.Core.Config;
using FarmQuest.Core.Time;
using UnityEngine;

namespace FarmQuest.Systems.Ads
{
    /// <summary>
    /// Optional rewarded ads ONLY (§10, §46): never required, never forced,
    /// player-initiated, rewards granted only on provider-confirmed completion.
    /// Conservative kid-safe limits from BalanceConfig (§12).
    /// Phase 6 wires a real provider (Unity Ads / AdMob) behind IAdProvider.
    /// </summary>
    public interface IAdProvider
    {
        bool IsReady { get; }
        void Show(string placement, Action<bool> onComplete);
    }

    /// <summary>Editor/stub provider: simulates a successful ad in the editor.</summary>
    public class StubAdProvider : IAdProvider
    {
        public bool IsReady => true;
        public void Show(string placement, Action<bool> onComplete)
        {
            Debug.Log($"[Ads] (stub) Showing rewarded ad: {placement}");
            onComplete?.Invoke(true);
        }
    }

    public class RewardedAdService
    {
        private readonly IRemoteConfigProvider _config;
        private readonly ITimeService _time;
        private readonly IAdProvider _provider;

        private int _adsWatchedToday;
        private string _dayStamp = "";
        private float _lastAdTime = -99999f;
        private float _reductionUsedToday;
        private int _totalAdsWatched;
        private double _totalSecondsAccelerated;
        private bool _showing;

        /// <summary>Crop yield multiplier granted by the "bigger harvest" ad.</summary>
        public const float YieldBoostMultiplier = 1.5f;

        public int AdsWatchedToday { get { RollDayIfNeeded(); return _adsWatchedToday; } }
        public int AdsLeftToday => Math.Max(0, _config.MaxRewardedAdsPerDay - AdsWatchedToday);
        public int TotalAdsWatched => _totalAdsWatched;

        /// <summary>"Finish now" is only offered when this little growth time is left (config: ad_finish_max_remaining_seconds).</summary>
        public float FinishMaxRemainingSeconds => _config.GetFloat("ad_finish_max_remaining_seconds", 600f);

        public RewardedAdService(IRemoteConfigProvider config, ITimeService time, IAdProvider provider)
        {
            _config = config;
            _time = time;
            _provider = provider;
            _dayStamp = DayStamp();
        }

        private string DayStamp() => DateTime.UtcNow.ToString("yyyy-MM-dd");

        private void RollDayIfNeeded()
        {
            if (DayStamp() == _dayStamp) return;
            _dayStamp = DayStamp();
            _adsWatchedToday = 0;
            _reductionUsedToday = 0f;
        }

        public bool IsRewardedAdAvailable()
        {
            RollDayIfNeeded();
            if (_showing || _provider == null || !_provider.IsReady) return false;
            if (_adsWatchedToday >= _config.MaxRewardedAdsPerDay) return false;
            if (Time.realtimeSinceStartup - _lastAdTime < _config.RewardedAdCooldownSeconds) return false;
            return true;
        }

        /// <summary>Generic entry point; each Show*Xxx* helper defines its reward.</summary>
        public void ShowAd(string placement, Action<bool> onComplete)
        {
            if (!IsRewardedAdAvailable()) { onComplete?.Invoke(false); return; }
            _showing = true;
            try
            {
                _provider.Show(placement, completed =>
                {
                    _showing = false;
                    if (completed)
                    {
                        _adsWatchedToday++;
                        _totalAdsWatched++;
                        _lastAdTime = Time.realtimeSinceStartup;
                    }
                    onComplete?.Invoke(completed); // reward ONLY on confirmed completion
                });
            }
            catch (Exception e)
            {
                // A crashing SDK must never block gameplay or grant a reward.
                _showing = false;
                Debug.LogWarning($"[Ads] Provider failed: {e.Message}");
                onComplete?.Invoke(false);
            }
        }

        public bool CanReduceTime(float requestedSeconds)
        {
            RollDayIfNeeded();
            return _reductionUsedToday + requestedSeconds <= _config.MaxTimeReductionPerDaySeconds;
        }

        public void ShowCropSpeedupAd(Farming.CropInstance crop, Action<float> onReduced) =>
            ShowCropTimeAd(crop, SpeedupSeconds(crop), "crop_speedup", onReduced);

        /// <summary>Completes the crop's current growth stage.</summary>
        public void ShowCropStageSkipAd(Farming.CropInstance crop, Action<float> onReduced) =>
            ShowCropTimeAd(crop, crop != null ? crop.SecondsToNextStage : 0f, "crop_stage_skip", onReduced);

        /// <summary>Completes the whole growth timer (only offered near the end, see FinishMaxRemainingSeconds).</summary>
        public void ShowCropFinishAd(Farming.CropInstance crop, Action<float> onReduced) =>
            ShowCropTimeAd(crop, crop != null ? crop.RemainingSeconds : 0f, "crop_finish", onReduced);

        public float SpeedupSeconds(Farming.CropInstance crop) =>
            crop == null ? 0f : Mathf.Min(_config.CropAdReductionSeconds, crop.RemainingSeconds);

        public bool CanOfferSpeedup(Farming.CropInstance crop) => CanOfferTime(crop, SpeedupSeconds(crop));
        public bool CanOfferStageSkip(Farming.CropInstance crop) =>
            crop != null && crop.Stage < Data.GrowthStage.Fruiting && CanOfferTime(crop, crop.SecondsToNextStage);
        public bool CanOfferFinish(Farming.CropInstance crop) =>
            crop != null && crop.RemainingSeconds <= FinishMaxRemainingSeconds && CanOfferTime(crop, crop.RemainingSeconds);
        public bool CanOfferYieldBoost(Farming.CropInstance crop) =>
            crop != null && !crop.IsReady && crop.BonusYieldMultiplier < YieldBoostMultiplier && IsRewardedAdAvailable();

        private bool CanOfferTime(Farming.CropInstance crop, float seconds) =>
            crop != null && !crop.IsReady && seconds > 0.5f && CanReduceTime(seconds) && IsRewardedAdAvailable();

        private void ShowCropTimeAd(Farming.CropInstance crop, float amount, string placement, Action<float> onReduced)
        {
            if (crop == null || crop.IsReady || amount <= 0f || !CanReduceTime(amount)) { onReduced?.Invoke(0f); return; }
            ShowAd(placement, completed =>
            {
                if (!completed) { onReduced?.Invoke(0f); return; }
                var accel = Core.Services.ServiceLocator.Get<Farming.TimeAccelerationService>();
                float reduced = accel.AccelerateCrop(crop, amount + 0.01f);
                _reductionUsedToday += reduced;
                _totalSecondsAccelerated += reduced;
                onReduced?.Invoke(reduced);
            });
        }

        /// <summary>Bigger harvest: multiplies this crop's yield once.</summary>
        public void ShowYieldBoostAd(Farming.CropInstance crop, Action<bool> onRewarded)
        {
            if (!CanOfferYieldBoost(crop)) { onRewarded?.Invoke(false); return; }
            ShowAd("yield_boost", completed =>
            {
                if (completed) crop.BonusYieldMultiplier *= YieldBoostMultiplier;
                onRewarded?.Invoke(completed);
            });
        }

        /// <summary>Free supplies (seeds, fertilizer, nutrients...).</summary>
        public void ShowItemRewardAd(string itemId, int count, Action<bool> onRewarded)
        {
            ShowAd("item_reward", completed =>
            {
                if (completed && count > 0)
                    Core.Services.ServiceLocator.Get<Inventory.InventoryService>().Add(itemId, count);
                onRewarded?.Invoke(completed);
            });
        }

        public void ShowBonusCoinsAd(int coins, Action<int> onRewarded)
        {
            ShowAd("bonus_coins", completed =>
            {
                if (!completed) { onRewarded?.Invoke(0); return; }
                Core.Services.ServiceLocator.Get<Economy.EconomyService>().AddCoins(coins, "ad_bonus");
                onRewarded?.Invoke(coins);
            });
        }

        public void ShowDoubleRewardAd(int baseCoins, Action<int> onRewarded)
        {
            ShowAd("double_reward", completed =>
            {
                int grant = completed ? baseCoins : 0;
                if (completed)
                    Core.Services.ServiceLocator.Get<Economy.EconomyService>().AddCoins(baseCoins, "ad_double");
                onRewarded?.Invoke(grant);
            });
        }
    
        // ---------- save (acceleration history + daily limits survive restarts) ----------
        public Core.Save.AdSaveData CaptureState() => new Core.Save.AdSaveData
        {
            dayStamp = _dayStamp,
            adsWatchedToday = _adsWatchedToday,
            secondsReducedToday = _reductionUsedToday,
            totalAdsWatched = _totalAdsWatched,
            totalSecondsAccelerated = _totalSecondsAccelerated
        };

        public void RestoreState(Core.Save.AdSaveData data)
        {
            if (data == null) return;
            _totalAdsWatched = Math.Max(0, data.totalAdsWatched);
            _totalSecondsAccelerated = Math.Max(0, data.totalSecondsAccelerated);
            if (data.dayStamp == DayStamp())
            {
                _dayStamp = data.dayStamp;
                _adsWatchedToday = Math.Max(0, data.adsWatchedToday);
                _reductionUsedToday = Mathf.Max(0f, data.secondsReducedToday);
            }
        }
    }
}
