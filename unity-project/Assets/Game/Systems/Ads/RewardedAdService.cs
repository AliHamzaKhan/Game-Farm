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
            if (!_provider.IsReady) return false;
            if (_adsWatchedToday >= _config.MaxRewardedAdsPerDay) return false;
            if (Time.realtimeSinceStartup - _lastAdTime < _config.RewardedAdCooldownSeconds) return false;
            return true;
        }

        /// <summary>Generic entry point; each Show*Xxx* helper defines its reward.</summary>
        public void ShowAd(string placement, Action<bool> onComplete)
        {
            if (!IsRewardedAdAvailable()) { onComplete?.Invoke(false); return; }
            _provider.Show(placement, completed =>
            {
                if (completed)
                {
                    _adsWatchedToday++;
                    _lastAdTime = Time.realtimeSinceStartup;
                }
                onComplete?.Invoke(completed); // reward ONLY on confirmed completion
            });
        }

        public bool CanReduceTime(float requestedSeconds)
        {
            RollDayIfNeeded();
            return _reductionUsedToday + requestedSeconds <= _config.MaxTimeReductionPerDaySeconds;
        }

        public void ShowCropSpeedupAd(Farming.CropInstance crop, Action<float> onReduced)
        {
            float amount = Mathf.Min(_config.CropAdReductionSeconds, crop.RemainingSeconds);
            if (!CanReduceTime(amount)) { onReduced?.Invoke(0f); return; }
            ShowAd("crop_speedup", completed =>
            {
                if (!completed) { onReduced?.Invoke(0f); return; }
                _reductionUsedToday += amount;
                var accel = Core.Services.ServiceLocator.Get<Farming.TimeAccelerationService>();
                onReduced?.Invoke(accel.AccelerateCrop(crop, amount));
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
    }
}
