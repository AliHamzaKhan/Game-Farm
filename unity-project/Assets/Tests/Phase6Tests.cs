using System.Collections.Generic;
using System.Threading.Tasks;
using FarmQuest.Core.Config;
using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.CloudSave;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Events;
using FarmQuest.Systems.ParentMode;
using NUnit.Framework;
using UnityEngine;

namespace FarmQuest.Tests
{
    /// <summary>Phase 6: parent gate, live events, cloud save, remote config (§58).</summary>
    public class Phase6Tests
    {
        [SetUp]
        public void SetUp() => ServiceLocator.Clear();

        [TearDown]
        public void TearDown() => ServiceLocator.Clear();

        // ---------- parent gate ----------

        [Test]
        public void ParentGate_ChallengeHasCorrectAnswer()
        {
            var parents = new ParentModeService();
            for (int i = 0; i < 20; i++)
            {
                var c = parents.GenerateChallenge();
                Assert.IsNotNull(c.Question);
                Assert.AreEqual(3, c.Options.Length);
                // The correct slot must hold the answer parsed from the question.
                // Question format: "To continue, solve: A + B = ?"
                var parts = c.Question.Split(' ');
                int a = int.Parse(parts[3]);
                int b = int.Parse(parts[5]);
                Assert.AreEqual(a + b, c.Options[c.CorrectIndex]);
                Assert.IsTrue(parents.VerifyAnswer(c, c.CorrectIndex));
                Assert.IsFalse(parents.VerifyAnswer(c, (c.CorrectIndex + 1) % 3));
            }
        }

        [Test]
        public void ParentMode_Settings_SaveRoundTrip()
        {
            var parents = new ParentModeService
            {
                AdsAllowed = false,
                AnalyticsAllowed = false,
                MaxDailyPlayMinutes = 60
            };
            var saved = parents.CaptureState();
            var fresh = new ParentModeService();
            fresh.RestoreState(saved);
            Assert.IsFalse(fresh.AdsAllowed);
            Assert.IsFalse(fresh.AnalyticsAllowed);
            Assert.AreEqual(60, fresh.MaxDailyPlayMinutes);
        }

        // ---------- live events ----------

        [Test]
        public void LiveEvent_ActiveEvent_AppliesBonuses()
        {
            var cropDb = ScriptableObject.CreateInstance<CropDatabase>();
            cropDb.Initialize();
            var market = new MarketService(cropDb, new FixedPriceStrategy());
            var economy = new EconomyService();
            ServiceLocator.Register(market);
            ServiceLocator.Register(economy);

            var now = System.DateTime.Now;
            var e = ScriptableObject.CreateInstance<GameEventData>();
            e.month = now.Month;
            e.startDay = 1;
            e.endDay = 28;
            e.sellPriceBonus = 1.1f;
            e.xpMultiplier = 2f;

            var service = new LiveEventService(new List<GameEventData> { e });
            Assert.AreEqual(1, service.ActiveEvents().Count);
            service.RefreshBonuses();
            Assert.AreEqual(2f, economy.XpMultiplier);

            // Inactive event: no bonuses.
            e.month = now.Month == 12 ? 1 : 12;
            service.RefreshBonuses();
            Assert.AreEqual(1f, economy.XpMultiplier);
        }

        // ---------- cloud save ----------

        private class FakeCloudProvider : ICloudSaveProvider
        {
            public long CloudTicks;
            public string CloudJson = "{}";
            public int Uploads;

            public Task<CloudUploadResult> UploadAsync(string userId, string json, long updatedAtTicks)
            {
                Uploads++;
                CloudJson = json;
                CloudTicks = updatedAtTicks;
                return Task.FromResult(new CloudUploadResult { Success = true });
            }

            public Task<CloudDownloadResult> DownloadAsync(string userId) =>
                Task.FromResult(new CloudDownloadResult
                {
                    Success = true,
                    Found = true,
                    Json = CloudJson,
                    UpdatedAtTicks = CloudTicks
                });
        }

        [Test]
        public async Task CloudSave_NewerCloud_Wins()
        {
            var provider = new FakeCloudProvider { CloudTicks = 2000, CloudJson = "{\"v\":2}" };
            var service = new CloudSaveService(provider);
            string json = await service.DownloadNewerSaveAsync("u1", 1000);
            Assert.AreEqual("{\"v\":2}", json);
        }

        [Test]
        public async Task CloudSave_OlderCloud_Ignored()
        {
            var provider = new FakeCloudProvider { CloudTicks = 500, CloudJson = "{\"v\":1}" };
            var service = new CloudSaveService(provider);
            string json = await service.DownloadNewerSaveAsync("u1", 1000);
            Assert.IsNull(json);
        }

        [Test]
        public async Task CloudSave_Upload_ForwardsToProvider()
        {
            var provider = new FakeCloudProvider();
            var service = new CloudSaveService(provider);
            await service.UploadSaveAsync("u1", "{\"v\":3}", 3000);
            Assert.AreEqual(1, provider.Uploads);
            Assert.AreEqual(3000, provider.CloudTicks);
        }

        // ---------- remote config ----------

        [Test]
        public void RemoteConfig_ParseFlatJson()
        {
            var dict = HttpRemoteConfigProvider.ParseFlatJson(
                "{\"max_rewarded_ads_per_day\": 3, \"name\": \"hi\", \"on\": true}");
            Assert.AreEqual("3", dict["max_rewarded_ads_per_day"]);
            Assert.AreEqual("hi", dict["name"]);
            Assert.AreEqual("true", dict["on"]);
            Assert.AreEqual(0, HttpRemoteConfigProvider.ParseFlatJson("not json").Count);
        }
    }
}
