using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace FarmQuest.Tests
{
    /// <summary>Coins, XP/levels, market buy/sell (§58).</summary>
    public class EconomyTests
    {
        private EconomyService _economy;
        private ProgressionService _progression;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            var prog = ScriptableObject.CreateInstance<ProgressionData>();
            prog.levels.Add(new LevelEntry { level = 1, title = "New Farmer", xpRequiredCumulative = 0 });
            prog.levels.Add(new LevelEntry { level = 2, title = "Garden Farmer", xpRequiredCumulative = 100 });
            prog.levels.Add(new LevelEntry { level = 3, title = "Skilled Farmer", xpRequiredCumulative = 300 });

            _economy = new EconomyService();
            _progression = new ProgressionService(prog);
            ServiceLocator.Register(_economy);
            ServiceLocator.Register(_progression);
        }

        [TearDown]
        public void TearDown() => ServiceLocator.Clear();

        [Test]
        public void AddCoins_IncreasesBalance()
        {
            _economy.AddCoins(100, "test");
            Assert.AreEqual(100, _economy.Coins);
        }

        [Test]
        public void TrySpend_Succeeds_WhenAffordable()
        {
            _economy.AddCoins(100, "test");
            Assert.IsTrue(_economy.TrySpend(40));
            Assert.AreEqual(60, _economy.Coins);
        }

        [Test]
        public void TrySpend_Fails_WhenTooExpensive()
        {
            _economy.AddCoins(10, "test");
            Assert.IsFalse(_economy.TrySpend(40));
            Assert.AreEqual(10, _economy.Coins); // unchanged
        }

        [Test]
        public void AddXp_LevelsUp_AtThreshold()
        {
            int levelUps = 0;
            GameEvents.LevelUp += _ => levelUps++;
            _economy.AddXp(100, "test");
            Assert.AreEqual(2, _progression.Level);
            Assert.AreEqual(1, levelUps);
            _economy.AddXp(200, "test");
            Assert.AreEqual(3, _progression.Level);
        }

        [Test]
        public void Market_BuySeed_SpendsCoinsAndGrantsSeeds()
        {
            var db = ScriptableObject.CreateInstance<CropDatabase>();
            var carrot = ScriptableObject.CreateInstance<CropData>();
            carrot.cropId = "carrot"; carrot.seedPrice = 10;
            var seedItem = ScriptableObject.CreateInstance<ItemData>();
            seedItem.itemId = "seed_carrot"; seedItem.maxStack = 99; seedItem.category = ItemCategory.Seed;
            db.crops = new System.Collections.Generic.List<CropData> { carrot };
            db.items = new System.Collections.Generic.List<ItemData> { seedItem };
            db.Initialize();

            ServiceLocator.Register(new InventoryService(db));
            var market = new MarketService(db, new FixedPriceStrategy());
            _economy.AddCoins(100, "test");

            Assert.IsTrue(market.BuySeed("carrot", 3));
            Assert.AreEqual(70, _economy.Coins);
            Assert.AreEqual(3, ServiceLocator.Get<InventoryService>().GetCount("seed_carrot"));
        }

        [Test]
        public void Market_BuySeed_Fails_WhenBroke()
        {
            var db = ScriptableObject.CreateInstance<CropDatabase>();
            var carrot = ScriptableObject.CreateInstance<CropData>();
            carrot.cropId = "carrot"; carrot.seedPrice = 10;
            db.crops = new System.Collections.Generic.List<CropData> { carrot };
            db.items = new System.Collections.Generic.List<ItemData>();
            db.Initialize();
            ServiceLocator.Register(new InventoryService(db));
            var market = new MarketService(db, new FixedPriceStrategy());

            Assert.IsFalse(market.BuySeed("carrot", 3)); // 0 coins
            Assert.AreEqual(0, ServiceLocator.Get<InventoryService>().GetCount("seed_carrot"));
        }
    }
}
