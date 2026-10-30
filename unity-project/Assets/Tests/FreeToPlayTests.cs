using System;
using System.Collections.Generic;
using FarmQuest.Core.Config;
using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Ads;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Farming;
using FarmQuest.Systems.Inventory;
using FarmQuest.Systems.ParentMode;
using FarmQuest.Systems.Store;
using NUnit.Framework;
using UnityEngine;

namespace FarmQuest.Tests
{
    /// <summary>Gems, IAP grants, daily rewards, safety net, ad offers, consumables, parent switches.</summary>
    public class FreeToPlayTests
    {
        private class ScriptedIap : IIapProvider
        {
            public bool Available = true;
            public PurchaseResult Next;
            public bool IsAvailable => Available;
            public string GetPriceLabel(string productId) => null;
            public void Purchase(string productId, Action<PurchaseResult> onResult) { Next.productId = productId; onResult(Next); }
        }

        private class AlwaysAd : IAdProvider
        {
            public bool Complete = true;
            public bool IsReady => true;
            public void Show(string placement, Action<bool> onComplete) => onComplete(Complete);
        }

        private FakeTimeService _time;
        private CropDatabase _db;
        private StoreCatalog _catalog;
        private EconomyService _economy;
        private InventoryService _inventory;
        private ParentModeService _parents;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            _time = new FakeTimeService();
            _db = ScriptableObject.CreateInstance<CropDatabase>();
            var carrot = ScriptableObject.CreateInstance<CropData>();
            carrot.cropId = "carrot"; carrot.displayName = "Carrot"; carrot.seedPrice = 10; carrot.unlockLevel = 1;
            var golden = ScriptableObject.CreateInstance<CropData>();
            golden.cropId = "golden_tomato"; golden.displayName = "Golden Tomato"; golden.isPremium = true; golden.gemSeedPrice = 20; golden.premiumSeedPack = 3;
            _db.crops = new List<CropData> { carrot, golden };
            var booster = ScriptableObject.CreateInstance<ItemData>();
            booster.itemId = "growth_booster"; booster.displayName = "Growth Booster"; booster.gemPrice = 10;
            booster.effect = ConsumableEffect.GrowthBooster; booster.effectValue = 0.25f; booster.tier = ItemTier.Premium;
            var crop = ScriptableObject.CreateInstance<ItemData>();
            crop.itemId = "crop_carrot"; crop.sellPrice = 6; crop.category = ItemCategory.Crop;
            _db.items = new List<ItemData> { booster, crop };
            _db.Initialize();
            ServiceLocator.Register(_db);

            var prog = ScriptableObject.CreateInstance<ProgressionData>();
            prog.levels.Add(new LevelEntry { level = 1, xpRequiredCumulative = 0 });
            ServiceLocator.Register(new ProgressionService(prog));
            _economy = new EconomyService();
            ServiceLocator.Register(_economy);
            _inventory = new InventoryService(_db);
            ServiceLocator.Register(_inventory);
            _parents = new ParentModeService();
            ServiceLocator.Register(_parents);

            _catalog = ScriptableObject.CreateInstance<StoreCatalog>();
            _catalog.bundles.Add(new StoreBundle { productId = "gems_100", title = "Gem Pack", contents = new RewardContents { gems = 100 } });
            _catalog.bundles.Add(new StoreBundle { productId = "pack_once", title = "Once", consumable = false, contents = new RewardContents { coins = 500 } });
            _catalog.loginCalendar.Add(new RewardContents { coins = 100 });
            _catalog.loginCalendar.Add(new RewardContents { gems = 5 });
            _catalog.dailyFreeSeeds = 3;
            _catalog.rescueSeeds = 5;
            _catalog.weeklyChallenges.Add(new WeeklyChallengeDef { title = "Harvest 10", type = ChallengeType.HarvestCrops, target = 10, reward = new RewardContents { gems = 10 } });
        }

        [TearDown]
        public void TearDown() => ServiceLocator.Clear();

        // ---------- purchases ----------

        [Test]
        public void Purchase_GrantsOnce_PerTransaction()
        {
            var iap = new ScriptedIap { Next = new PurchaseResult { success = true, transactionId = "t1" } };
            var store = new StoreService(_catalog, iap);
            bool granted = false;
            store.BuyBundle("gems_100", ok => granted = ok);
            Assert.IsTrue(granted);
            Assert.AreEqual(100, _economy.Gems);
            // Duplicate callback / store restore of the same transaction: no second grant.
            Assert.IsFalse(store.ProcessPurchase(new PurchaseResult { success = true, productId = "gems_100", transactionId = "t1" }));
            Assert.AreEqual(100, _economy.Gems);
        }

        [Test]
        public void FailedOrCancelledPurchase_GrantsNothing()
        {
            var iap = new ScriptedIap { Next = new PurchaseResult { success = false, error = "cancelled" } };
            var store = new StoreService(_catalog, iap);
            store.BuyBundle("gems_100", ok => Assert.IsFalse(ok));
            Assert.AreEqual(0, _economy.Gems);
        }

        [Test]
        public void ParentSwitch_BlocksPurchases()
        {
            _parents.PurchasesAllowed = false;
            var iap = new ScriptedIap { Next = new PurchaseResult { success = true, transactionId = "t2" } };
            var store = new StoreService(_catalog, iap);
            Assert.IsFalse(store.IsStoreAvailable);
            store.BuyBundle("gems_100", ok => Assert.IsFalse(ok));
            Assert.AreEqual(0, _economy.Gems);
        }

        [Test]
        public void StubStoreInBuilds_IsUnavailable()
        {
            var store = new StoreService(_catalog, new StubIapProvider(false));
            Assert.IsFalse(store.IsStoreAvailable);
        }

        [Test]
        public void NonConsumable_HiddenAfterPurchase_AndSurvivesSave()
        {
            var iap = new ScriptedIap { Next = new PurchaseResult { success = true, transactionId = "t3" } };
            var store = new StoreService(_catalog, iap);
            store.BuyBundle("pack_once", _ => { });
            CollectionAssert.DoesNotContain(new List<StoreBundle>(store.VisibleBundles()), _catalog.GetBundle("pack_once"));
            var restored = new StoreService(_catalog, iap);
            restored.RestoreState(store.CaptureState());
            Assert.IsFalse(restored.ProcessPurchase(new PurchaseResult { success = true, productId = "pack_once", transactionId = "t3" }));
            CollectionAssert.DoesNotContain(new List<StoreBundle>(restored.VisibleBundles()), _catalog.GetBundle("pack_once"));
        }

        [Test]
        public void PremiumSeeds_CostGems_NotCoins()
        {
            var store = new StoreService(_catalog, new ScriptedIap());
            _economy.AddCoins(100000);
            Assert.IsFalse(store.BuyPremiumSeeds("golden_tomato"), "coins can't buy premium seeds");
            _economy.AddGems(20);
            Assert.IsTrue(store.BuyPremiumSeeds("golden_tomato"));
            Assert.AreEqual(3, _inventory.GetCount("seed_golden_tomato"));
            Assert.AreEqual(0, _economy.Gems);
            var market = new MarketService(_db, new FixedPriceStrategy());
            Assert.IsFalse(market.BuySeed("golden_tomato", 1), "market never sells premium seeds for coins");
        }

        // ---------- daily rewards ----------

        [Test]
        public void DailyReward_OncePerDay_StreakPausesInsteadOfResetting()
        {
            var daily = new DailyRewardService(_catalog, _time);
            Assert.IsTrue(daily.ClaimDaily());
            Assert.AreEqual(100, _economy.Coins);
            Assert.AreEqual(3, _inventory.GetCount("seed_carrot"), "daily free seeds");
            Assert.IsFalse(daily.ClaimDaily(), "once per day");
            _time.AdvanceSeconds(5 * 24 * 3600); // away for days
            Assert.AreEqual(1, daily.CalendarIndex, "missed days don't reset the streak");
            Assert.IsTrue(daily.ClaimDaily());
            Assert.AreEqual(5, _economy.Gems);
        }

        [Test]
        public void DailyReward_AdDouble_OnlyWhenAdCompletes()
        {
            var config = ScriptableObject.CreateInstance<BalanceConfig>();
            config.rewardedAdCooldownSeconds = 0;
            var provider = new AlwaysAd { Complete = false };
            ServiceLocator.Register(new RewardedAdService(config, _time, provider));
            var daily = new DailyRewardService(_catalog, _time);
            daily.ClaimDailyWithAd(ok => Assert.IsFalse(ok));
            Assert.AreEqual(0, _economy.Coins);
            Assert.IsTrue(daily.CanClaimToday, "a skipped ad never costs the normal reward");
            provider.Complete = true;
            daily.ClaimDailyWithAd(ok => Assert.IsTrue(ok));
            Assert.AreEqual(200, _economy.Coins);
            Assert.AreEqual(6, _inventory.GetCount("seed_carrot"));
        }

        [Test]
        public void WeeklyChallenge_TracksAndClaimsOnce_ThenResetsNextWeek()
        {
            var daily = new DailyRewardService(_catalog, _time);
            var carrot = _db.GetCrop("carrot");
            GameEvents.RaiseCropHarvested(new CropInstance(carrot, _time, _time.Now.Ticks), 10);
            Assert.IsTrue(daily.IsWeeklyDone(0));
            Assert.IsTrue(daily.ClaimWeekly(0));
            Assert.IsFalse(daily.ClaimWeekly(0));
            Assert.AreEqual(10, _economy.Gems);
            _time.AdvanceSeconds(8 * 24 * 3600);
            _time.RaiseTick(); _time.RaiseTick(); _time.RaiseTick(); _time.RaiseTick(); _time.RaiseTick();
            Assert.AreEqual(0, daily.WeeklyProgress(0));
            Assert.IsFalse(daily.IsWeeklyClaimed(0));
        }

        [Test]
        public void SafetyNet_GivesSeeds_OnlyWhenTrulyStuck_OncePerDay()
        {
            var daily = new DailyRewardService(_catalog, _time);
            _economy.AddCoins(5); // less than a carrot seed (10)
            Assert.IsTrue(daily.IsStuck());
            Assert.IsTrue(daily.TryRescue());
            Assert.AreEqual(5, _inventory.GetCount("seed_carrot"));
            Assert.IsFalse(daily.IsStuck(), "has seeds now");

            _inventory.TryRemove("seed_carrot", 5);
            Assert.IsFalse(daily.TryRescue(), "once per day");
            _time.AdvanceSeconds(24 * 3600);
            _inventory.Add("crop_carrot", 2);
            Assert.IsFalse(daily.IsStuck(), "can still sell crops");
            _inventory.TryRemove("crop_carrot", 2);
            Assert.IsTrue(daily.TryRescue());
        }

        [Test]
        public void SafetyNet_NotTriggered_WhenPlayerCanAffordSeeds()
        {
            var daily = new DailyRewardService(_catalog, _time);
            _economy.AddCoins(50);
            Assert.IsFalse(daily.IsStuck());
            Assert.IsFalse(daily.TryRescue());
        }

        // ---------- ad offers & parent ad switch ----------

        [Test]
        public void AdOffers_RespectCooldowns_AndGrantOnlyOnCompletion()
        {
            var config = ScriptableObject.CreateInstance<BalanceConfig>();
            config.rewardedAdCooldownSeconds = 0;
            config.maxRewardedAdsPerDay = 99;
            var provider = new AlwaysAd { Complete = false };
            var ads = new RewardedAdService(config, _time, provider);
            float clock = 1000f;
            var offers = new AdOfferService(ads, config) { Clock = () => clock };
            int shown = 0;
            offers.OfferAvailable += o => shown++;

            GameEvents.RaiseOrderFulfilled(100);
            Assert.AreEqual(1, shown);
            offers.Accept(ok => Assert.IsFalse(ok));
            Assert.AreEqual(0, _economy.Coins, "skipped ad = no bonus");

            GameEvents.RaiseOrderFulfilled(100);
            Assert.AreEqual(1, shown, "global gap: not after every action");
            clock += 400f;
            provider.Complete = true;
            GameEvents.RaiseOrderFulfilled(100);
            Assert.AreEqual(2, shown);
            offers.Accept();
            Assert.AreEqual(50, _economy.Coins, "+50% delivery bonus");
        }

        [Test]
        public void ParentAdSwitch_StopsAllAds()
        {
            var config = ScriptableObject.CreateInstance<BalanceConfig>();
            config.rewardedAdCooldownSeconds = 0;
            var ads = new RewardedAdService(config, _time, new AlwaysAd());
            Assert.IsTrue(ads.IsRewardedAdAvailable());
            _parents.AdsAllowed = false;
            Assert.IsFalse(ads.IsRewardedAdAvailable());
            var offers = new AdOfferService(ads, config);
            int shown = 0;
            offers.OfferAvailable += o => shown++;
            GameEvents.RaiseOrderFulfilled(100);
            Assert.AreEqual(0, shown);
        }

        // ---------- consumables ----------

        [Test]
        public void GrowthBooster_ShortensButDoesNotSkip()
        {
            var data = ScriptableObject.CreateInstance<CropData>();
            data.growthDurationSeconds = 400f;
            var tile = new FarmTile(Vector2Int.zero, 0, true);
            tile.Clear(); tile.Dig(); tile.Prepare();
            var crop = new CropInstance(data, _time, _time.Now.Ticks);
            tile.Plant(crop);
            ServiceLocator.Register(new CropCareService());
            _inventory.Add("growth_booster", 1);
            Assert.IsTrue(ServiceLocator.Get<CropCareService>().TryUseConsumable(tile, "growth_booster"));
            Assert.AreEqual(300f, crop.RemainingSeconds, 0.5f, "25% of the timer, not instant");
            Assert.IsFalse(ServiceLocator.Get<CropCareService>().TryUseConsumable(tile, "growth_booster"), "none left");
        }
    }
}
