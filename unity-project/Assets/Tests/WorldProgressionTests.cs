using System.Collections.Generic;
using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Farming;
using FarmQuest.Systems.Inventory;
using FarmQuest.Systems.World;
using NUnit.Framework;
using UnityEngine;

namespace FarmQuest.Tests
{
    /// <summary>Countries → cities → objectives: start, progress, unlocks, endless laps, save.</summary>
    public class WorldProgressionTests
    {
        private WorldData _world;
        private CropDatabase _db;
        private EconomyService _economy;
        private InventoryService _inventory;
        private WorldProgressionService _service;
        private FakeTimeService _time;

        private static CityDef City(string id, string introduced, params CityObjective[] objectives)
        {
            var c = new CityDef { cityId = id, displayName = id, rewardCoins = 100, rewardXp = 10 };
            if (!string.IsNullOrEmpty(introduced)) c.introducedCropIds.Add(introduced);
            c.objectives.AddRange(objectives);
            return c;
        }

        private static CityObjective Harvest(string crop, long n) =>
            new CityObjective { type = CityObjectiveType.HarvestCrop, itemId = crop, target = n };

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            _time = new FakeTimeService();
            _world = ScriptableObject.CreateInstance<WorldData>();
            _world.countries = new List<CountryDef>
            {
                new CountryDef { countryId = "pk", displayName = "Pakistan", completionBonusCoins = 1000, cities = new List<CityDef>
                {
                    City("karachi", "tomato", Harvest("tomato", 5),
                        new CityObjective { type = CityObjectiveType.DeliverItem, itemId = "crop_wheat", target = 3 },
                        new CityObjective { type = CityObjectiveType.EarnFromSales, target = 50 }),
                    City("lahore", "onion", Harvest("onion", 4)),
                }},
                new CountryDef { countryId = "in", displayName = "India", completionBonusCoins = 2000, cities = new List<CityDef>
                {
                    City("mumbai", "", Harvest("tomato", 10)),
                }},
            };
            _world.lapTargetGrowth = 0.5f;
            _world.lapRewardGrowth = 1f;

            _db = ScriptableObject.CreateInstance<CropDatabase>();
            _db.Initialize();
            var prog = ScriptableObject.CreateInstance<ProgressionData>();
            prog.levels.Add(new LevelEntry { level = 1, title = "New Farmer", xpRequiredCumulative = 0 });
            _economy = new EconomyService();
            _inventory = new InventoryService(_db);
            ServiceLocator.Register(_economy);
            ServiceLocator.Register(new ProgressionService(prog));
            ServiceLocator.Register(_inventory);
            _service = new WorldProgressionService(_world);
            ServiceLocator.Register(_service);
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Clear();
            Object.DestroyImmediate(_world);
            Object.DestroyImmediate(_db);
        }

        private void RaiseHarvest(string cropId, int qty)
        {
            var crop = ScriptableObject.CreateInstance<CropData>();
            crop.cropId = cropId;
            GameEvents.RaiseCropHarvested(new CropInstance(crop, _time, _time.Now.Ticks), qty);
        }

        private void CompleteKarachi()
        {
            RaiseHarvest("tomato", 5);
            _inventory.Add("crop_wheat", 3);
            _service.Deliver(1);
            GameEvents.RaiseSaleCompleted("crop_tomato", 5, 50);
        }

        [Test]
        public void Starts_InFirstCityOfFirstCountry()
        {
            Assert.AreEqual(0, _service.CurrentCityIndex);
            Assert.AreEqual("karachi", _service.Current.City.cityId);
            Assert.AreEqual("Pakistan", _service.Current.CountryName);
            Assert.AreEqual(CityStatus.Current, _service.GetStatus(0));
            Assert.AreEqual(CityStatus.Locked, _service.GetStatus(1));
        }

        [Test]
        public void Objectives_TrackHarvestsDeliveriesAndSales()
        {
            RaiseHarvest("tomato", 3);
            RaiseHarvest("carrot", 9); // not wanted here
            Assert.AreEqual(3, _service.ObjectiveProgress[0]);

            _inventory.Add("crop_wheat", 10);
            Assert.AreEqual(3, _service.Deliver(1), "only delivers what the contract still needs");
            Assert.AreEqual(7, _inventory.GetCount("crop_wheat"));
            Assert.IsTrue(_service.IsObjectiveDone(1));

            GameEvents.RaiseSaleCompleted("crop_tomato", 2, 30);
            Assert.AreEqual(30, _service.ObjectiveProgress[2]);
            Assert.IsFalse(_service.IsCityComplete);
        }

        [Test]
        public void CompletingCity_RewardsAndUnlocksNextCity()
        {
            int completedIndex = -1;
            GameEvents.CityCompleted += (i, name) => completedIndex = i;
            CompleteKarachi();
            Assert.AreEqual(0, completedIndex);
            Assert.AreEqual(1, _service.CurrentCityIndex);
            Assert.AreEqual("lahore", _service.Current.City.cityId);
            Assert.AreEqual(CityStatus.Completed, _service.GetStatus(0));
            Assert.AreEqual(100, _economy.Coins);
            Assert.AreEqual(WorldProgressionService.NewCropStarterSeeds, _inventory.GetCount("seed_onion"),
                "the next city's new crop comes with starter seeds");
            Assert.AreEqual(0, _service.ObjectiveProgress[0], "fresh objectives for the new city");
        }

        [Test]
        public void CompletingLastCityInCountry_UnlocksNextCountryWithBonus()
        {
            string completedCountry = null;
            GameEvents.CountryCompleted += name => completedCountry = name;
            CompleteKarachi();
            RaiseHarvest("onion", 4);
            Assert.AreEqual("Pakistan", completedCountry);
            Assert.AreEqual("India", _service.Current.CountryName);
            Assert.AreEqual(100 + 100 + 1000, _economy.Coins);
            Assert.AreEqual(1, _service.CountriesCompleted);
            Assert.AreEqual(1f, _service.CountryCompletion(0), 0.001f);
        }

        [Test]
        public void AfterLastAuthoredCity_TourContinuesWithHarderTargets()
        {
            CompleteKarachi();
            RaiseHarvest("onion", 4);
            RaiseHarvest("tomato", 10); // Mumbai, last authored city
            Assert.AreEqual(3, _service.CurrentCityIndex);
            var lap2 = _service.Current;
            Assert.AreEqual(1, lap2.Lap);
            Assert.AreEqual("karachi", _world.countries[0].cities[0].cityId, "authored data untouched");
            Assert.AreEqual(8, lap2.City.objectives[0].target, "5 x 1.5 rounded");
            Assert.AreEqual(200, lap2.City.rewardCoins, "rewards scale too");
            StringAssert.Contains("★2", lap2.CityName);
        }

        [Test]
        public void CropAvailability_FollowsTheTour()
        {
            Assert.IsTrue(_service.IsCropIntroduced("tomato"));
            Assert.IsFalse(_service.IsCropIntroduced("onion"));
            CompleteKarachi();
            Assert.IsTrue(_service.IsCropIntroduced("onion"));
        }

        [Test]
        public void Save_RoundTripsCityAndProgress()
        {
            CompleteKarachi();
            RaiseHarvest("onion", 2);
            var saved = _service.CaptureState();

            var fresh = new WorldProgressionService(_world);
            fresh.RestoreState(saved);
            Assert.AreEqual(1, fresh.CurrentCityIndex);
            Assert.AreEqual(2, fresh.ObjectiveProgress[0]);
            Assert.AreEqual(1, fresh.CitiesCompleted);
        }

        [Test]
        public void Restore_DropsProgressWhenCityDataChanged()
        {
            RaiseHarvest("tomato", 4);
            var saved = _service.CaptureState();
            saved.currentCityId = "renamed_city";
            var fresh = new WorldProgressionService(_world);
            fresh.RestoreState(saved);
            Assert.AreEqual(0, fresh.ObjectiveProgress[0]);
        }

        [Test]
        public void CityPrices_ApplyToMatchingItems()
        {
            _world.countries[0].cities[0].prices.Add(new CityPriceModifier { itemId = "crop_tomato", sellMultiplier = 1.5f });
            Assert.AreEqual(1.5f, _service.GetSellMultiplier("crop_tomato"), 0.001f);
            Assert.AreEqual(1f, _service.GetSellMultiplier("crop_onion"), 0.001f);
            CollectionAssert.Contains(_service.DemandedItemIds(), "crop_tomato");
        }
    }
}
