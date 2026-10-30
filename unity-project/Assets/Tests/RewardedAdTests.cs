using System;
using FarmQuest.Core.Config;
using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Ads;
using FarmQuest.Systems.Farming;
using FarmQuest.Systems.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace FarmQuest.Tests
{
    /// <summary>Rewarded ads: reward only on confirmed completion, limits, crop acceleration.</summary>
    public class RewardedAdTests
    {
        private class ScriptedAdProvider : IAdProvider
        {
            public bool Ready = true;
            public bool Complete = true;
            public bool Throw;
            public int Shown;
            public bool IsReady => Ready;
            public void Show(string placement, Action<bool> onComplete)
            {
                Shown++;
                if (Throw) throw new InvalidOperationException("sdk crashed");
                onComplete(Complete);
            }
        }

        private FakeTimeService _time;
        private BalanceConfig _config;
        private ScriptedAdProvider _provider;
        private RewardedAdService _ads;
        private CropData _crop;
        private InventoryService _inventory;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            _time = new FakeTimeService();
            _config = ScriptableObject.CreateInstance<BalanceConfig>();
            _config.rewardedAdCooldownSeconds = 0f;
            _config.maxRewardedAdsPerDay = 3;
            _config.maxTimeReductionPerDaySeconds = 10000f;
            _config.cropAdReductionSeconds = 60f;
            _provider = new ScriptedAdProvider();
            _ads = new RewardedAdService(_config, _time, _provider);
            ServiceLocator.Register(new TimeAccelerationService());
            var db = ScriptableObject.CreateInstance<CropDatabase>();
            _inventory = new InventoryService(db);
            ServiceLocator.Register(_inventory);

            _crop = ScriptableObject.CreateInstance<CropData>();
            _crop.growthDurationSeconds = 300f;
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Clear();
            UnityEngine.Object.DestroyImmediate(_config);
            UnityEngine.Object.DestroyImmediate(_crop);
        }

        private CropInstance Plant() => new CropInstance(_crop, _time, _time.Now.Ticks);

        [Test]
        public void SkippedAd_GrantsNothing()
        {
            _provider.Complete = false;
            var crop = Plant();
            float reduced = -1f;
            _ads.ShowCropFinishAd(crop, r => reduced = r);
            Assert.AreEqual(0f, reduced);
            Assert.IsFalse(crop.IsReady);
            Assert.AreEqual(0, _ads.AdsWatchedToday);
        }

        [Test]
        public void CrashingProvider_GrantsNothing_AndDoesNotThrow()
        {
            _provider.Throw = true;
            bool? rewarded = null;
            Assert.DoesNotThrow(() => _ads.ShowItemRewardAd("fertilizer", 2, ok => rewarded = ok));
            Assert.AreEqual(false, rewarded);
            Assert.AreEqual(0, _inventory.GetCount("fertilizer"));
        }

        [Test]
        public void UnavailableAd_IsNotOffered_AndNeverShown()
        {
            _provider.Ready = false;
            var crop = Plant();
            Assert.IsFalse(_ads.CanOfferSpeedup(crop));
            _ads.ShowCropSpeedupAd(crop, r => { });
            Assert.AreEqual(0, _provider.Shown);
        }

        [Test]
        public void CompletedAds_AccelerateCrop()
        {
            var crop = Plant();
            _ads.ShowCropSpeedupAd(crop, r => Assert.AreEqual(60f, r, 0.1f));
            Assert.AreEqual(240f, crop.RemainingSeconds, 0.1f);

            var stageBefore = crop.Stage;
            _ads.ShowCropStageSkipAd(crop, r => { });
            Assert.Greater((int)crop.Stage, (int)stageBefore);

            _ads.ShowCropFinishAd(crop, r => { });
            Assert.IsTrue(crop.IsReady);
        }

        [Test]
        public void FinishAd_OnlyOfferedNearTheEnd()
        {
            var longCrop = ScriptableObject.CreateInstance<CropData>();
            longCrop.growthDurationSeconds = 7200f;
            var crop = new CropInstance(longCrop, _time, _time.Now.Ticks);
            Assert.IsFalse(_ads.CanOfferFinish(crop), "2h left is above the configured limit");
            Assert.IsTrue(_ads.CanOfferSpeedup(crop));
            UnityEngine.Object.DestroyImmediate(longCrop);
        }

        [Test]
        public void DailyLimit_IsRespected()
        {
            for (int i = 0; i < 3; i++) _ads.ShowItemRewardAd("seed_tomato", 1, ok => { });
            Assert.IsFalse(_ads.IsRewardedAdAvailable());
            _ads.ShowItemRewardAd("seed_tomato", 1, ok => Assert.IsFalse(ok));
            Assert.AreEqual(3, _inventory.GetCount("seed_tomato"));
        }

        [Test]
        public void History_SurvivesRestart()
        {
            _ads.ShowItemRewardAd("seed_tomato", 1, ok => { });
            _ads.ShowItemRewardAd("seed_tomato", 1, ok => { });
            var saved = _ads.CaptureState();
            var restarted = new RewardedAdService(_config, _time, _provider);
            restarted.RestoreState(saved);
            Assert.AreEqual(2, restarted.AdsWatchedToday);
            Assert.AreEqual(2, restarted.TotalAdsWatched);
        }
    }
}
