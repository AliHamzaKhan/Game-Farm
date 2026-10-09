using System.Collections.Generic;
using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Animals;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace FarmQuest.Tests
{
    /// <summary>Animal production, hunger, happiness, capacity, pets (§58).</summary>
    public class AnimalTests
    {
        private FakeTimeService _time;
        private AnimalData _chicken;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            _time = new FakeTimeService();
            _chicken = ScriptableObject.CreateInstance<AnimalData>();
            _chicken.animalId = "chicken";
            _chicken.displayName = "Chicken";
            _chicken.housing = AnimalHousing.Coop;
            _chicken.productItemId = "egg";
            _chicken.productionIntervalSeconds = 600f;
            _chicken.maxStoredProducts = 4;
            _chicken.feedIntervalSeconds = 7200f;
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Clear();
            Object.DestroyImmediate(_chicken);
        }

        private AnimalInstance NewChicken() => new AnimalInstance(_chicken, _time);

        [Test]
        public void ProductsReady_AccumulatesOverTime()
        {
            var chicken = NewChicken(); // happiness 60 → multiplier 1.0
            Assert.AreEqual(0, chicken.ProductsReady);
            _time.AdvanceSeconds(600);
            Assert.AreEqual(1, chicken.ProductsReady);
            _time.AdvanceSeconds(600);
            Assert.AreEqual(2, chicken.ProductsReady);
        }

        [Test]
        public void ProductsReady_CappedAtMaxStored()
        {
            var chicken = NewChicken();
            _time.AdvanceSeconds(100000);
            Assert.AreEqual(4, chicken.ProductsReady);
        }

        [Test]
        public void IsHungry_AfterFeedInterval()
        {
            var chicken = NewChicken();
            Assert.IsFalse(chicken.IsHungry);
            _time.AdvanceSeconds(7201);
            Assert.IsTrue(chicken.IsHungry);
            Assert.AreEqual(AnimalState.Hungry, chicken.CurrentState);
        }

        [Test]
        public void Feed_ResetsHunger_AndBoostsHappiness()
        {
            var chicken = NewChicken();
            _time.AdvanceSeconds(7201);
            chicken.Feed();
            Assert.IsFalse(chicken.IsHungry);
            Assert.Greater(chicken.Happiness, 60f);
        }

        [Test]
        public void Happiness_DecaysOverTime()
        {
            var chicken = NewChicken();
            chicken.Pet(); // 60 + 25 = 85
            Assert.AreEqual(85f, chicken.Happiness, 0.01f);
            _time.AdvanceSeconds(3600); // -5
            Assert.AreEqual(80f, chicken.Happiness, 0.01f);
        }

        [Test]
        public void HappyAnimal_ProducesFaster()
        {
            var chicken = NewChicken();
            chicken.Pet(); // happiness 85 → multiplier 1.2 → 500s per egg
            _time.AdvanceSeconds(500);
            Assert.AreEqual(1, chicken.ProductsReady);
        }

        [Test]
        public void Collect_ResetsTimer()
        {
            var chicken = NewChicken();
            _time.AdvanceSeconds(1200);
            Assert.AreEqual(2, chicken.Collect());
            Assert.AreEqual(0, chicken.ProductsReady);
        }

        // ---------- service-level ----------

        private AnimalService MakeService(int coins, int level)
        {
            var db = ScriptableObject.CreateInstance<AnimalDatabase>();
            db.animals = new List<AnimalData> { _chicken };
            db.Initialize();

            var prog = ScriptableObject.CreateInstance<ProgressionData>();
            for (int i = 1; i <= level; i++)
                prog.levels.Add(new LevelEntry { level = i, title = "L" + i, xpRequiredCumulative = (i - 1) * 100 });

            var layout = ScriptableObject.CreateInstance<FarmLayoutData>();
            layout.gridWidth = 4; layout.gridHeight = 4; layout.tileSize = 2f;
            layout.regions = new List<FarmRegion>
            {
                new FarmRegion { regionId = 2, regionName = "Meadow",
                    area = new RectInt(0, 0, 2, 2), unlockedByDefault = true }
            };
            var cropDb = ScriptableObject.CreateInstance<CropDatabase>();
            cropDb.Initialize();

            var economy = new EconomyService();
            economy.AddCoins(coins, "test");
            ServiceLocator.Register(economy);
            ServiceLocator.Register(new ProgressionService(prog));
            ServiceLocator.Register(new Systems.Farming.FarmService(layout, cropDb, _time));
            ServiceLocator.Register(new InventoryService(cropDb));

            // ProgressionService level comes from XP; force it:
            ServiceLocator.Get<ProgressionService>().Restore((level - 1) * 100, level);

            var service = new AnimalService(db, _time);
            ServiceLocator.Register(service);
            return service;
        }

        [Test]
        public void BuyAnimal_RespectsCapacity()
        {
            _chicken.unlockLevel = 1;
            var service = MakeService(100000, 10);
            // Coop capacity is 4.
            for (int i = 0; i < 4; i++)
                Assert.IsTrue(service.BuyAnimal("chicken"), $"buy #{i + 1} should succeed");
            Assert.IsFalse(service.BuyAnimal("chicken"), "5th chicken should fail: coop full");
            Assert.AreEqual(0, service.FreeSlots(AnimalHousing.Coop));
        }

        [Test]
        public void BuyAnimal_RequiresCoins()
        {
            _chicken.unlockLevel = 1;
            _chicken.buyPrice = 150;
            var service = MakeService(100, 10); // can't afford
            Assert.IsFalse(service.BuyAnimal("chicken"));
            Assert.AreEqual(0, service.Animals.Count);
        }

        [Test]
        public void CollectProducts_AddsToInventory()
        {
            _chicken.unlockLevel = 1;
            var service = MakeService(100000, 10);
            Assert.IsTrue(service.BuyAnimal("chicken"));
            var animal = service.Animals[0];
            _time.AdvanceSeconds(600);
            int collected = service.CollectProducts(animal);
            Assert.AreEqual(1, collected);
            Assert.AreEqual(1, ServiceLocator.Get<InventoryService>().GetCount("egg"));
        }

        [Test]
        public void PetService_UnlocksDog_AndCatOnLevelUp()
        {
            var db = ScriptableObject.CreateInstance<AnimalDatabase>();
            var dog = ScriptableObject.CreateInstance<PetData>();
            dog.petId = "dog"; dog.unlockLevel = 0;
            var cat = ScriptableObject.CreateInstance<PetData>();
            cat.petId = "cat"; cat.unlockLevel = 14;
            db.pets = new List<PetData> { dog, cat };
            db.Initialize();

            var pets = new PetService(db);
            Assert.IsTrue(pets.UnlockPet("dog"));
            Assert.AreEqual("dog", pets.ActivePetId);

            GameEvents.RaiseLevelUp(14); // cat auto-unlocks
            Assert.IsTrue(pets.IsUnlocked("cat"));

            pets.SetPetName("Biscuit");
            Assert.AreEqual("Biscuit", pets.PetName);
        }
    }
}
