using System;
using System.Collections.Generic;
using FarmQuest.Core.Config;
using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Ads;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Farming;
using FarmQuest.Systems.Inventory;
using FarmQuest.Systems.World;
using NUnit.Framework;
using UnityEngine;

namespace FarmQuest.Tests
{
    /// <summary>
    /// Free-to-play guarantee: a new player who never watches an ad (or buys
    /// anything) can finish the first city in reasonable time, using only the
    /// starting resources and coins earned by farming. Plays the real farm,
    /// market, care and world-tour services with Karachi's crops and contracts.
    /// </summary>
    public class NoAdsProgressionTests
    {
        private class NeverShowAds : IAdProvider
        {
            public int Shown;
            public bool IsReady => true;
            public void Show(string placement, Action<bool> onComplete) { Shown++; onComplete(false); }
        }

        private const int MaxMinutes = 30; // generous: about 17 min in practice
        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();

        private T Make<T>() where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            _created.Add(o);
            return o;
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Clear();
            foreach (var o in _created) UnityEngine.Object.DestroyImmediate(o);
            _created.Clear();
        }

        // Values mirror GameDataGenerator (Karachi crops) so the test tracks real balance.
        private CropData Crop(string id, int seed, int sell, int min, int max, float secs, float[] weights,
            float waterInterval, bool fertilizer, NutrientType nutrient, bool prune, float pest)
        {
            var c = Make<CropData>();
            c.cropId = id; c.displayName = id; c.seedPrice = seed; c.sellPrice = sell;
            c.harvestMin = min; c.harvestMax = max; c.growthDurationSeconds = secs; c.stageWeights = weights;
            c.waterIntervalSeconds = waterInterval; c.waterRequired = 2; c.needsFertilizer = fertilizer;
            c.requiredNutrient = nutrient; c.needsPruning = prune; c.pestChance = pest; c.xpReward = 10;
            return c;
        }

        private ItemData Item(string id, int buy, int sell)
        {
            var i = Make<ItemData>();
            i.itemId = id; i.displayName = id; i.buyPrice = buy; i.sellPrice = sell; i.maxStack = 99;
            return i;
        }

        private static CityObjective Obj(CityObjectiveType type, string item, long target) =>
            new CityObjective { type = type, itemId = item, target = target };

        /// <summary>Plays Karachi with no ads. Returns minutes taken, or -1 if not finished in time.</summary>
        private double PlayKarachi(bool careful, out long coinsAfter, out int adsShown)
        {
            ServiceLocator.Clear();
            var time = new FakeTimeService();
            ServiceLocator.Register<FarmQuest.Core.Time.ITimeService>(time);

            var db = Make<CropDatabase>();
            db.crops = new List<CropData>
            {
                Crop("tomato", 15, 12, 5, 8, 300, new[] { 0.8f, 1f, 1.6f, 1.2f, 2.4f }, 120, true, NutrientType.Calcium, true, 0.35f),
                Crop("potato", 12, 8, 6, 9, 480, new[] { 1f, 1.4f, 2.2f, 0.8f, 2.6f }, 160, true, NutrientType.Potassium, false, 0.30f),
                Crop("wheat", 20, 8, 10, 15, 240, new[] { 0.8f, 1.5f, 2f, 0.7f, 1.2f }, 0, false, NutrientType.Nitrogen, false, 0.20f),
            };
            db.items = new List<ItemData>
            {
                Item("seed_tomato", 15, 0), Item("seed_potato", 12, 0), Item("seed_wheat", 20, 0),
                Item("crop_tomato", 0, 12), Item("crop_potato", 0, 8), Item("crop_wheat", 0, 8),
                Item("fertilizer", 15, 0), Item("pesticide", 12, 0), Item("compost", 8, 0),
                Item("nutrient_nitrogen", 10, 0), Item("nutrient_potassium", 12, 0), Item("nutrient_calcium", 14, 0),
            };
            db.Initialize();

            var prog = Make<ProgressionData>();
            prog.levels.Add(new LevelEntry { level = 1, xpRequiredCumulative = 0 });
            var layout = Make<FarmLayoutData>();
            layout.gridWidth = 16; layout.gridHeight = 16; layout.tileSize = 2;
            layout.regions = new List<FarmRegion>
            {
                new FarmRegion { regionId = 0, area = new RectInt(2, 2, 4, 4), unlockedByDefault = true } // 16 starting plots
            };
            var world = Make<WorldData>();
            world.countries = new List<CountryDef>
            {
                new CountryDef { displayName = "Pakistan", cities = new List<CityDef>
                {
                    new CityDef
                    {
                        cityId = "karachi", displayName = "Karachi", rewardCoins = 300,
                        objectives = new List<CityObjective>
                        {
                            Obj(CityObjectiveType.HarvestCrop, "tomato", 20),
                            Obj(CityObjectiveType.HarvestCrop, "potato", 15),
                            Obj(CityObjectiveType.DeliverItem, "crop_wheat", 15),
                            Obj(CityObjectiveType.EarnFromSales, "", 400),
                        },
                        prices = new List<CityPriceModifier> { new CityPriceModifier { itemId = "crop_tomato", sellMultiplier = 1.2f } }
                    },
                    new CityDef { cityId = "lahore", displayName = "Lahore",
                        objectives = new List<CityObjective> { Obj(CityObjectiveType.EarnFromSales, "", 999999) } },
                }}
            };

            var economy = new EconomyService(); ServiceLocator.Register(economy);
            ServiceLocator.Register(new ProgressionService(prog));
            var inventory = new InventoryService(db); ServiceLocator.Register(inventory);
            ServiceLocator.Register(db);
            ServiceLocator.Register(new CropGrowthService(time));
            var farm = new FarmService(layout, db, time); ServiceLocator.Register(farm);
            var care = new CropCareService(); ServiceLocator.Register(care);
            var pricing = new FixedPriceStrategy();
            var market = new MarketService(db, pricing); ServiceLocator.Register(market);
            var tour = new WorldProgressionService(world); ServiceLocator.Register(tour);
            pricing.ItemSellMultiplier = tour.GetSellMultiplier;

            // Ads exist but the player never accepts any.
            var config = Make<BalanceConfig>();
            var provider = new NeverShowAds();
            ServiceLocator.Register(new RewardedAdService(config, time, provider));

            // New-game grant, as in Bootstrapper.GrantStartingResources.
            economy.AddCoins(100);
            inventory.Add("seed_tomato", 5); inventory.Add("seed_potato", 5); inventory.Add("seed_wheat", 5);
            inventory.Add("fertilizer", 2); inventory.Add("pesticide", 2); inventory.Add("compost", 2);

            long Need(int i) => i < tour.ObjectiveProgress.Count ? tour.GetTarget(i) - tour.ObjectiveProgress[i] : 0;
            for (int step = 1; step <= MaxMinutes * 6; step++) // the player checks the farm every 10 s
            {
                time.AdvanceSeconds(10);
                foreach (var t in farm.AllTiles())
                {
                    if (!t.IsUnlocked) continue;
                    farm.TryClear(t); farm.TryDig(t); farm.TryPrepare(t);
                    if (t.CanPlant())
                    {
                        string id = Need(2) > inventory.GetCount("crop_wheat") ? "wheat"
                            : Need(0) > 0 ? "tomato" : Need(1) > 0 ? "potato" : "tomato";
                        if (inventory.GetCount("seed_" + id) == 0) market.BuySeed(id, 1);
                        farm.TryPlant(t, id);
                    }
                    if (!t.HasCrop) continue;
                    var c = t.Crop;
                    if (c.NeedsWater) farm.TryWater(t);
                    if (careful)
                    {
                        if (c.HasPests) { if (inventory.GetCount("pesticide") == 0) market.BuyItem("pesticide", 1); care.TrySpray(t); }
                        if (c.NeedsFertilizer) { if (inventory.GetCount("fertilizer") == 0) market.BuyItem("fertilizer", 1); farm.TryApplyNutrient(t); }
                        if (c.HasNutrientDeficiency)
                        {
                            string nid = CropInstance.NutrientItemId(c.Data.requiredNutrient);
                            if (inventory.GetCount(nid) == 0) market.BuyItem(nid, 1);
                            care.TryApplyNutrient(t);
                        }
                        if (c.CanPrune) care.TryPrune(t);
                    }
                    if (t.CanHarvest()) farm.TryHarvest(t);
                }
                tour.Deliver(2);
                // Sell whatever the contracts don't need.
                int tomatoes = inventory.GetCount("crop_tomato"), potatoes = inventory.GetCount("crop_potato");
                if (tomatoes > 0) market.Sell("crop_tomato", tomatoes);
                if (potatoes > 0) market.Sell("crop_potato", potatoes);
                int spareWheat = inventory.GetCount("crop_wheat") - (int)Need(2);
                if (spareWheat > 0) market.Sell("crop_wheat", spareWheat);

                if (tour.CurrentCityIndex > 0)
                {
                    coinsAfter = economy.Coins;
                    adsShown = provider.Shown;
                    return step * 10 / 60.0;
                }
            }
            coinsAfter = economy.Coins;
            adsShown = provider.Shown;
            return -1;
        }

        [Test]
        public void FirstCity_CanBeFinishedWithoutAds_WithFullCare([Values(0, 1, 2)] int run)
        {
            double minutes = PlayKarachi(careful: true, out long coins, out int ads);
            TestContext.WriteLine($"full care: {minutes:F1} min, {coins} coins left");
            Assert.Greater(minutes, 0, $"Karachi not finished within {MaxMinutes} minutes without ads");
            Assert.AreEqual(0, ads, "no ad was needed or shown");
            Assert.Greater(coins, 100, "farming should leave the player richer than they started");
        }

        [Test]
        public void FirstCity_CanBeFinishedWithoutAds_WithMinimalCare([Values(0, 1, 2)] int run)
        {
            // A player who only waters and harvests (no fertilizer, sprays, nutrients or pruning).
            double minutes = PlayKarachi(careful: false, out long coins, out int ads);
            TestContext.WriteLine($"water+harvest only: {minutes:F1} min, {coins} coins left");
            Assert.Greater(minutes, 0, $"Karachi not finished within {MaxMinutes} minutes without ads");
            Assert.AreEqual(0, ads);
            Assert.Greater(coins, 100);
        }
    }
}
