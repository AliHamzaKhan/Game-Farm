using System.Collections.Generic;
using System.IO;
using FarmQuest.Core.Config;
using FarmQuest.Data;
using FarmQuest.Systems.Animals;
using FarmQuest.Systems.Buildings;
using FarmQuest.Systems.Decor;
using FarmQuest.Systems.Machines;
using FarmQuest.Systems.Missions;
using FarmQuest.Systems.Fishing;
using FarmQuest.Systems.Orchard;
using FarmQuest.Systems.Events;
using FarmQuest.Systems.Processing;
using FarmQuest.Systems.Village;
using FarmQuest.Systems.Npcs;
using FarmQuest.Systems.Story;
using UnityEditor;
using UnityEngine;

namespace FarmQuest.Editor
{
    /// <summary>
    /// Generates all data assets from the GDD/spec tuning tables.
    /// Menu: FarmQuest → Generate → All Game Data.
    /// Re-running overwrites generated assets (safe: data lives here, not in scenes).
    /// </summary>
    public static class GameDataGenerator
    {
        // Under Resources/ so Bootstrapper's Resources.Load fallbacks work in builds.
        private const string Root = "Assets/Resources/Data";

        [MenuItem("FarmQuest/Generate/All Game Data")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(Root);
            var crops = GenerateCrops();
            var items = GenerateItems(crops);
            items.AddRange(GenerateAnimalProducts());
            var database = GenerateDatabase(crops, items);
            GenerateProgression();
            GenerateBalance();
            GenerateFarmLayout();
            GenerateMissions();
            GenerateAchievements();
            GenerateNpcs();
            GenerateStory();
            GenerateAnimalDatabase();
            GenerateMachines();
            GenerateProduction(crops);
            GenerateVillage(crops);
            GenerateGameEvents();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[FarmQuest] Generated {crops.Count} crops, {items.Count} items, game events, " +
                      "database/progression/balance/layout + missions, achievements, NPCs, story chapters. " +
                      "Assign pools on the Bootstrapper or move Generated/ under Resources/Data.");
        }

        // ---- crops: 8 MVP vegetables (§55), durations from spec §8 ----
        private struct CropSpec
        {
            public string id, name; public int level, seed, sell, min, max, xp, water, stages;
            public float seconds; public Season season;
            public CropCategory category;
        }

        private static List<CropData> GenerateCrops()
        {
            var specs = new CropSpec[]
            {
                new CropSpec { id="carrot",  name="Carrot",  level=2,  seed=10, sell=6,  min=4, max=6,  xp=8,  water=1, stages=6, seconds=120, season=Season.Spring },
                new CropSpec { id="lettuce", name="Lettuce", level=4,  seed=12, sell=7,  min=4, max=6,  xp=8,  water=1, stages=6, seconds=180, season=Season.Spring },
                new CropSpec { id="tomato",  name="Tomato",  level=3,  seed=15, sell=12, min=5, max=8,  xp=10, water=2, stages=6, seconds=360, season=Season.Summer },
                new CropSpec { id="onion",   name="Onion",   level=5,  seed=14, sell=8,  min=5, max=7,  xp=9,  water=2, stages=6, seconds=300, season=Season.None },
                new CropSpec { id="peas",    name="Peas",    level=6,  seed=16, sell=9,  min=5, max=8,  xp=10, water=2, stages=6, seconds=300, season=Season.Spring },
                new CropSpec { id="potato",  name="Potato",  level=7,  seed=12, sell=8,  min=6, max=9,  xp=11, water=2, stages=6, seconds=240, season=Season.Autumn },
                new CropSpec { id="corn",    name="Corn",    level=13, seed=30, sell=14, min=6, max=10, xp=14, water=3, stages=6, seconds=600, season=Season.Summer },
                new CropSpec { id="pumpkin", name="Pumpkin", level=9,  seed=25, sell=22, min=3, max=5,  xp=16, water=2, stages=6, seconds=900, season=Season.Autumn },
                // ---- Phase 2 crops ----
                new CropSpec { id="strawberry", name="Strawberry", level=12, seed=45, sell=16, min=6, max=10, xp=18, water=3, stages=6, seconds=720,  season=Season.Spring, category=CropCategory.Fruit },
                new CropSpec { id="watermelon", name="Watermelon", level=14, seed=60, sell=45, min=3, max=5,  xp=22, water=4, stages=6, seconds=1200, season=Season.Summer, category=CropCategory.Fruit },
                new CropSpec { id="apple",   name="Apple",   level=17, seed=80, sell=18, min=8, max=12, xp=24, water=3, stages=6, seconds=1800, season=Season.Autumn, category=CropCategory.Fruit },
                new CropSpec { id="orange",  name="Orange",  level=19, seed=90, sell=20, min=8, max=12, xp=26, water=3, stages=6, seconds=2100, season=Season.None,   category=CropCategory.Fruit },
                new CropSpec { id="wheat",   name="Wheat",   level=20, seed=50, sell=8,  min=10, max=15, xp=20, water=2, stages=6, seconds=1500, season=Season.Autumn, category=CropCategory.Special },
                new CropSpec { id="sunflower", name="Sunflower", level=21, seed=70, sell=30, min=4, max=6, xp=22, water=2, stages=6, seconds=1200, season=Season.None, category=CropCategory.Special },
                new CropSpec { id="mango", name="Mango", level=26, seed=150, sell=40, min=6, max=10, xp=30, water=4, stages=6, seconds=7200, season=Season.Summer, category=CropCategory.Fruit },
                new CropSpec { id="peach", name="Peach", level=30, seed=180, sell=45, min=6, max=10, xp=32, water=4, stages=6, seconds=10800, season=Season.None, category=CropCategory.Fruit },
            };

            var list = new List<CropData>();
            foreach (var s in specs)
            {
                var crop = ScriptableObject.CreateInstance<CropData>();
                crop.cropId = s.id;
                crop.displayName = s.name;
                crop.category = s.category;
                crop.unlockLevel = s.level;
                crop.seedPrice = s.seed;
                crop.sellPrice = s.sell;
                crop.harvestMin = s.min;
                crop.harvestMax = s.max;
                crop.xpReward = s.xp;
                crop.growthDurationSeconds = s.seconds;
                crop.growthStages = s.stages;
                crop.waterRequired = s.water;
                crop.preferredSeason = s.season;
                SaveAsset(crop, $"{Root}/Crop_{s.id}.asset");
                list.Add(crop);
            }
            return list;
        }

        private static List<ItemData> GenerateItems(List<CropData> crops)
        {
            var list = new List<ItemData>();
            foreach (var crop in crops)
            {
                var seed = ScriptableObject.CreateInstance<ItemData>();
                seed.itemId = "seed_" + crop.cropId;
                seed.displayName = crop.displayName + " Seeds";
                seed.category = ItemCategory.Seed;
                seed.maxStack = 99;
                seed.buyPrice = crop.seedPrice;
                seed.cropId = crop.cropId;
                SaveAsset(seed, $"{Root}/Item_seed_{crop.cropId}.asset");
                list.Add(seed);

                var harvested = ScriptableObject.CreateInstance<ItemData>();
                harvested.itemId = "crop_" + crop.cropId;
                harvested.displayName = crop.displayName;
                harvested.category = ItemCategory.Crop;
                harvested.maxStack = 99;
                harvested.sellPrice = crop.sellPrice;
                SaveAsset(harvested, $"{Root}/Item_crop_{crop.cropId}.asset");
                list.Add(harvested);
            }

            var compost = ScriptableObject.CreateInstance<ItemData>();
            compost.itemId = "compost";
            compost.displayName = "Compost";
            compost.category = ItemCategory.Compost;
            compost.maxStack = 99;
            compost.buyPrice = 8;
            SaveAsset(compost, $"{Root}/Item_compost.asset");
            list.Add(compost);

            var fertilizer = ScriptableObject.CreateInstance<ItemData>();
            fertilizer.itemId = "fertilizer";
            fertilizer.displayName = "Fertilizer";
            fertilizer.category = ItemCategory.Fertilizer;
            fertilizer.maxStack = 99;
            fertilizer.buyPrice = 15;
            SaveAsset(fertilizer, $"{Root}/Item_fertilizer.asset");
            list.Add(fertilizer);

            return list;
        }

        private static CropDatabase GenerateDatabase(List<CropData> crops, List<ItemData> items)
        {
            var db = ScriptableObject.CreateInstance<CropDatabase>();
            db.crops = crops;
            db.items = items;
            SaveAsset(db, $"{Root}/CropDatabase.asset");
            return db;
        }

        private static void GenerateProgression()
        {
            // Levels 1–30 titles from the spec §56 + GDD.
            string[] titles =
            {
                "New Farmer","Garden Farmer","Skilled Farmer","Compost Crafter","Market Friend",
                "Storage Keeper","Potato Pro","Water Worker","Land Owner","Chicken Friend",
                "Egg Collector","Meadow Owner","Corn Grower","Barn Builder","Tractor Driver",
                "Orchard Keeper","Apple Picker","Dairy Farmer","Milk Maker","Processor",
                "Cheese Maker","Courier","Estate Owner","Horse Whisperer","Machine Master",
                "Greenhouse Grower","Crop Royalty","Decorator","Deal Maker","Master Farmer"
            };
            var prog = ScriptableObject.CreateInstance<ProgressionData>();
            int cumulative = 0;
            for (int n = 1; n <= 30; n++)
            {
                if (n > 1) cumulative += Mathf.RoundToInt(100 * Mathf.Pow(n - 1, 1.5f));
                prog.levels.Add(new LevelEntry
                {
                    level = n,
                    title = titles[n - 1],
                    xpRequiredCumulative = cumulative
                });
            }
            SaveAsset(prog, $"{Root}/Progression.asset");
        }

        private static void GenerateBalance()
        {
            var balance = ScriptableObject.CreateInstance<BalanceConfig>();
            SaveAsset(balance, $"{Root}/BalanceConfig.asset");
        }

        private static void GenerateFarmLayout()
        {
            var layout = ScriptableObject.CreateInstance<FarmLayoutData>();
            layout.gridWidth = 16;
            layout.gridHeight = 16;
            layout.tileSize = 2f;
            layout.regions = new List<FarmRegion>
            {
                new FarmRegion { regionId=0, regionName="Home Farm",   area=new RectInt(2, 2, 4, 4),  unlockLevel=1,  unlockCost=0,     unlockedByDefault=true },
                new FarmRegion { regionId=1, regionName="Sunny Field", area=new RectInt(8, 2, 8, 4),  unlockLevel=9,  unlockCost=1000 },
                new FarmRegion { regionId=2, regionName="Animal Meadow", area=new RectInt(2, 8, 6, 4), unlockLevel=12, unlockCost=4000 },
                new FarmRegion { regionId=3, regionName="Orchard Hill", area=new RectInt(10, 8, 4, 4), unlockLevel=16, unlockCost=3500 },
                new FarmRegion { regionId=4, regionName="Big Field",   area=new RectInt(2, 12, 8, 6), unlockLevel=18, unlockCost=6000 },
            };
            SaveAsset(layout, $"{Root}/FarmLayout.asset");
        }

        // ---------------- Phase 2 ----------------

        private static void GenerateMissions()
        {
            var defs = new (string id, MissionType type, string cropId, string cropName, int count, int coins, int xp, int level)[]
            {
                ("harvest_5",        MissionType.HarvestCrops, "",       "",        5,  50, 30, 1),
                ("water_10",         MissionType.WaterCrops,   "",       "",       10,  60, 35, 1),
                ("plant_8",          MissionType.PlantSeeds,   "",       "",        8,  60, 35, 1),
                ("harvest_carrot_10",MissionType.HarvestCrops, "carrot", "Carrot", 10,  80, 40, 2),
                ("harvest_tomato_12",MissionType.HarvestCrops, "tomato", "Tomato", 12, 120, 60, 3),
                ("sell_10",          MissionType.SellCrops,    "",       "",       10, 100, 50, 5),
                ("earn_500",         MissionType.EarnCoins,    "",       "",      500, 120, 60, 5),
                ("water_20",         MissionType.WaterCrops,   "",       "",       20, 100, 55, 8),
            };
            foreach (var d in defs)
            {
                var m = ScriptableObject.CreateInstance<MissionData>();
                m.missionId = d.id;
                m.type = d.type;
                m.targetCropId = d.cropId;
                m.targetCropName = d.cropName;
                m.targetCount = d.count;
                m.rewardCoins = d.coins;
                m.rewardXp = d.xp;
                m.minLevel = d.level;
                SaveAsset(m, $"{Root}/Mission_{d.id}.asset");
            }
        }

        private static void GenerateAchievements()
        {
            var defs = new (string id, string title, string desc, AchievementType type, int threshold, int coins, int xp)[]
            {
                ("first_harvest",  "First Harvest",  "Harvest your first crop.",        AchievementType.TotalHarvests,      1,     100,  20),
                ("harvest_100",    "Century Harvest","Harvest 100 crops.",              AchievementType.TotalHarvests,      100,   500, 100),
                ("harvest_1000",   "Harvest Master", "Harvest 1,000 crops.",            AchievementType.TotalHarvests,      1000, 2000, 400),
                ("rich_10000",     "10,000 Coins",   "Earn 10,000 coins in total.",     AchievementType.TotalCoinsEarned,   10000,  500, 200),
                ("seller_100",     "Market Regular", "Sell 100 items.",                 AchievementType.TotalItemsSold,     100,   400, 100),
                ("level_10",       "Skilled Farmer", "Reach level 10.",                 AchievementType.ReachLevel,         10,    600, 150),
                ("level_20",       "Farm Expert",    "Reach level 20.",                 AchievementType.ReachLevel,         20,   1500, 300),
                ("land_2",         "Expanding",      "Unlock 2 farm regions.",          AchievementType.UnlockLand,         2,     400, 100),
                ("missions_10",    "Reliable",       "Complete 10 daily missions.",     AchievementType.CompleteMissions,   10,    500, 150),
                ("story_3",        "Story So Far",   "Finish story chapter 3.",         AchievementType.CompleteStoryChapter, 3,   800, 200),
            };
            foreach (var d in defs)
            {
                var a = ScriptableObject.CreateInstance<AchievementData>();
                a.achievementId = d.id;
                a.title = d.title;
                a.description = d.desc;
                a.type = d.type;
                a.threshold = d.threshold;
                a.rewardCoins = d.coins;
                a.rewardXp = d.xp;
                SaveAsset(a, $"{Root}/Achievement_{d.id}.asset");
            }
        }

        private static void GenerateNpcs()
        {
            var defs = new (string id, string name, string role, string[] lines)[]
            {
                ("grandpa", "Grandpa Hamza", "Mentor", new[]
                {
                    "This farm was mine once. Let's wake it up together!",
                    "Remember: plants drink water like you do.",
                    "A patient farmer grows the sweetest tomatoes."
                }),
                ("seed_seller", "Amara", "Seed Seller", new[]
                {
                    "Fresh seeds! What shall we plant today?",
                    "Carrots love spring, tomatoes love summer."
                }),
                ("equipment_dealer", "Rafiq", "Equipment Dealer", new[]
                {
                    "Good tools, honest prices! That tractor will change your life.",
                    "Come back when your arms get tired of digging!"
                }),
                ("market_owner", "Uncle Bashir", "Market Owner", new[]
                {
                    "What a harvest! I'll give you a fair price.",
                    "Sell when the price forecast looks sunny!"
                }),
                ("restaurant_owner", "Lina", "Restaurant Owner", new[]
                {
                    "My kitchen needs your freshest vegetables!",
                    "One day I'll serve YOUR pumpkin soup."
                }),
                ("teacher", "Ms. Sana", "Farming Teacher", new[]
                {
                    "Did you know? Compost is old food turned into plant food!",
                    "Healthy soil grows healthy plants. Science!"
                }),
                ("animal_doctor", "Dr. Tariq", "Animal Doctor", new[]
                {
                    "Happy animals give the best milk and eggs.",
                    "A little brushing goes a long way!"
                }),
                ("builder", "Bilal", "Builder", new[]
                {
                    "I can raise barns, houses, anything you dream!",
                    "Every great farm starts with one strong wall."
                }),
            };
            foreach (var d in defs)
            {
                var npc = ScriptableObject.CreateInstance<NpcData>();
                npc.npcId = d.id;
                npc.displayName = d.name;
                npc.role = d.role;
                npc.dialogueLines = new List<string>(d.lines);
                SaveAsset(npc, $"{Root}/Npc_{d.id}.asset");
            }
        }

        private static void GenerateStory()
        {
            var chapters = new (int index, string title, string desc, ObjectiveData[] objectives, int coins, int xp)[]
            {
                (1, "Wake Up, Farm!", "Restore the old farm and make your first sale.", new[]
                {
                    new ObjectiveData { type = ObjectiveType.HarvestAny, targetCount = 3, label = "Harvest 3 crops" },
                    new ObjectiveData { type = ObjectiveType.EarnCoins,  targetCount = 200, label = "Earn 200 coins" },
                }, 300, 50),
                (2, "Green Fields", "Grow your first successful fields.", new[]
                {
                    new ObjectiveData { type = ObjectiveType.HarvestAny, targetCount = 15, label = "Harvest 15 crops" },
                    new ObjectiveData { type = ObjectiveType.ReachLevel, targetCount = 5,  label = "Reach level 5" },
                }, 800, 100),
                (3, "Market Days", "Become a familiar face at the market.", new[]
                {
                    new ObjectiveData { type = ObjectiveType.SellAny,   targetCount = 20,   label = "Sell 20 items" },
                    new ObjectiveData { type = ObjectiveType.EarnCoins, targetCount = 1000, label = "Earn 1,000 coins" },
                }, 1000, 150),
                (4, "Feathered Friends", "Chickens are waiting!", new[]
                {
                    new ObjectiveData { type = ObjectiveType.ReachLevel, targetCount = 10, label = "Reach level 10" },
                    new ObjectiveData { type = ObjectiveType.UnlockLand, targetCount = 2,  label = "Unlock 2 regions" },
                }, 1500, 200),
                (5, "Horsepower", "The tractor changes everything.", new[]
                {
                    new ObjectiveData { type = ObjectiveType.ReachLevel, targetCount = 15,   label = "Reach level 15" },
                    new ObjectiveData { type = ObjectiveType.EarnCoins,  targetCount = 5000, label = "Earn 5,000 coins" },
                }, 2000, 250),
                (6, "Growing Big", "Expand across the valley.", new[]
                {
                    new ObjectiveData { type = ObjectiveType.UnlockLand, targetCount = 3,   label = "Unlock 3 regions" },
                    new ObjectiveData { type = ObjectiveType.HarvestAny, targetCount = 100, label = "Harvest 100 crops" },
                }, 3000, 400),
                (7, "The Mill", "Turn harvests into artisan goods.", new[]
                {
                    new ObjectiveData { type = ObjectiveType.ReachLevel, targetCount = 20,    label = "Reach level 20" },
                    new ObjectiveData { type = ObjectiveType.EarnCoins,  targetCount = 15000, label = "Earn 15,000 coins" },
                }, 4000, 500),
                (8, "Farm Business", "Feed the whole valley.", new[]
                {
                    new ObjectiveData { type = ObjectiveType.SellAny,          targetCount = 200, label = "Sell 200 items" },
                    new ObjectiveData { type = ObjectiveType.CompleteMissions, targetCount = 10,  label = "Complete 10 missions" },
                }, 5000, 600),
                (9, "Dream Farmhouse", "Build the home you always wanted.", new[]
                {
                    new ObjectiveData { type = ObjectiveType.ReachLevel, targetCount = 25,    label = "Reach level 25" },
                    new ObjectiveData { type = ObjectiveType.EarnCoins,  targetCount = 40000, label = "Earn 40,000 coins" },
                }, 8000, 800),
                (10, "Master Farmer", "Become the valley's master farmer.", new[]
                {
                    new ObjectiveData { type = ObjectiveType.ReachLevel, targetCount = 30,  label = "Reach level 30" },
                    new ObjectiveData { type = ObjectiveType.HarvestAny, targetCount = 500, label = "Harvest 500 crops" },
                }, 15000, 1500),
            };
            foreach (var c in chapters)
            {
                var chapter = ScriptableObject.CreateInstance<StoryChapterData>();
                chapter.chapterIndex = c.index;
                chapter.title = c.title;
                chapter.description = c.desc;
                chapter.objectives = new List<ObjectiveData>(c.objectives);
                chapter.rewardCoins = c.coins;
                chapter.rewardXp = c.xp;
                SaveAsset(chapter, $"{Root}/Story_Chapter{c.index}.asset");
            }
        }

        // ---------------- Phase 3 ----------------

        private static List<ItemData> GenerateAnimalProducts()
        {
            var defs = new (string id, string name, int sell)[]
            {
                ("egg", "Egg", 15),
                ("milk", "Milk", 40),
                ("wool", "Wool", 60),
                ("goat_milk", "Goat Milk", 45),
                ("duck_egg", "Duck Egg", 25),
                ("honey", "Honey", 70),
            };
            var list = new List<ItemData>();
            foreach (var d in defs)
            {
                var item = ScriptableObject.CreateInstance<ItemData>();
                item.itemId = d.id;
                item.displayName = d.name;
                item.category = ItemCategory.AnimalProduct;
                item.maxStack = 99;
                item.sellPrice = d.sell;
                SaveAsset(item, $"{Root}/Item_{d.id}.asset");
                list.Add(item);
            }
            var feed = ScriptableObject.CreateInstance<ItemData>();
            feed.itemId = "feed";
            feed.displayName = "Animal Feed";
            feed.category = ItemCategory.Food;
            feed.maxStack = 99;
            feed.buyPrice = 10;
            SaveAsset(feed, $"{Root}/Item_feed.asset");
            list.Add(feed);
            return list;
        }

        private static void GenerateAnimalDatabase()
        {
            var db = ScriptableObject.CreateInstance<AnimalDatabase>();

            var animalDefs = new (string id, string name, int level, int price, AnimalHousing housing,
                string product, float interval, int maxStored, int region)[]
            {
                ("chicken", "Chicken", 10,  150,  AnimalHousing.Coop,    "egg",       600f, 4, 2),
                ("cow",     "Cow",     18,  800,  AnimalHousing.Barn,    "milk",     1800f, 3, 2),
                ("sheep",   "Sheep",   20,  700,  AnimalHousing.Pasture, "wool",     2700f, 3, 2),
                ("goat",    "Goat",    22,  900,  AnimalHousing.Pasture, "goat_milk",1800f, 3, 2),
                ("duck",    "Duck",    24,  500,  AnimalHousing.Pond,    "duck_egg", 1200f, 3, 2),
                ("horse",   "Horse",   24, 3000,  AnimalHousing.Stable,  "",            0f, 0, 2),
                ("bees",    "Bees",    26, 1200,  AnimalHousing.Hive,   "honey",    3600f, 4, 3),
            };
            foreach (var d in animalDefs)
            {
                var animal = ScriptableObject.CreateInstance<AnimalData>();
                animal.animalId = d.id;
                animal.displayName = d.name;
                animal.unlockLevel = d.level;
                animal.buyPrice = d.price;
                animal.housing = d.housing;
                animal.productItemId = d.product;
                animal.productionIntervalSeconds = d.interval;
                animal.maxStoredProducts = d.maxStored;
                animal.requiredRegionId = d.region;
                SaveAsset(animal, $"{Root}/Animal_{d.id}.asset");
                db.animals.Add(animal);
            }

            var dog = ScriptableObject.CreateInstance<PetData>();
            dog.petId = "dog";
            dog.displayName = "Dog";
            dog.unlockLevel = 0;
            dog.unlockDescription = "A gift from Grandpa!";
            SaveAsset(dog, $"{Root}/Pet_dog.asset");
            db.pets.Add(dog);

            var cat = ScriptableObject.CreateInstance<PetData>();
            cat.petId = "cat";
            cat.displayName = "Cat";
            cat.unlockLevel = 14;
            cat.unlockDescription = "Unlocks at level 14.";
            SaveAsset(cat, $"{Root}/Pet_cat.asset");
            db.pets.Add(cat);

            var house = ScriptableObject.CreateInstance<BuildingData>();
            house.buildingId = "house";
            house.displayName = "Farmhouse";
            house.levels = new List<BuildingLevel>
            {
                new BuildingLevel { levelName = "Small Farmhouse", cost = 0,     description = "A cozy start." },
                new BuildingLevel { levelName = "Larger House",    cost = 3000,  description = "Two rooms and a porch." },
                new BuildingLevel { levelName = "Farmhouse",       cost = 12000, description = "Kitchen, fireplace, garden." },
                new BuildingLevel { levelName = "Dream Farmhouse", cost = 40000, description = "The dream, built." },
            };
            SaveAsset(house, $"{Root}/Building_house.asset");
            db.buildings.Add(house);

            var barn = ScriptableObject.CreateInstance<BuildingData>();
            barn.buildingId = "barn";
            barn.displayName = "Barn";
            barn.levels = new List<BuildingLevel>
            {
                new BuildingLevel { levelName = "Small Barn", cost = 2000,  description = "+4 barn & pasture slots." },
                new BuildingLevel { levelName = "Big Barn",   cost = 6000,  description = "+4 barn & pasture slots." },
                new BuildingLevel { levelName = "Grand Barn", cost = 15000, description = "+4 barn & pasture slots." },
            };
            SaveAsset(barn, $"{Root}/Building_barn.asset");
            db.buildings.Add(barn);

            var decorDefs = new (string id, string name, string category, int level, int cost)[]
            {
                ("fence",      "Wooden Fence",  "Fences",   8,  50),
                ("flower_bed", "Flower Bed",    "Flowers",  8,  80),
                ("tree",       "Apple Tree",    "Trees",    10, 120),
                ("lamp",       "Garden Lamp",   "Lights",   12, 100),
                ("path_stone", "Stone Path",    "Paths",    8,  30),
                ("sign",       "Farm Sign",     "Signs",    8,  40),
                ("bench",      "Wooden Bench",  "Furniture",14, 150),
                ("birdhouse",  "Birdhouse",     "Wildlife", 16, 200),
            };
            foreach (var d in decorDefs)
            {
                var decor = ScriptableObject.CreateInstance<DecorData>();
                decor.decorId = d.id;
                decor.displayName = d.name;
                decor.category = d.category;
                decor.unlockLevel = d.level;
                decor.cost = d.cost;
                SaveAsset(decor, $"{Root}/Decor_{d.id}.asset");
                db.decorations.Add(decor);
            }

            SaveAsset(db, $"{Root}/AnimalDatabase.asset");
        }

        // ---------------- Phase 4 ----------------

        private static void GenerateMachines()
        {
            var db = ScriptableObject.CreateInstance<MachineDatabase>();

            var equipmentDefs = new (string id, string name, EquipmentType type, int level, int cost,
                string desc, int radius, float speed, float waterInterval, int waterCount)[]
            {
                ("small_tractor",  "Small Tractor",      EquipmentType.Tractor,    15, 2500,  "Drive it! Plows a 3x3 area.",      1, 1.0f, 0,   0),
                ("seeder",         "Seeder",             EquipmentType.Seeder,     15, 1200,  "Unlocks tractor Plant mode.",      0, 0,    0,   0),
                ("sprayer",        "Sprayer",            EquipmentType.Sprayer,    16, 900,   "Unlocks tractor Fertilize mode.",  0, 0,    0,   0),
                ("large_tractor",  "Large Tractor",      EquipmentType.Tractor,    23, 8000,  "Plows a 5x5 area, faster.",        2, 1.2f, 0,   0),
                ("auto_planter",   "Automated Planter",  EquipmentType.Planter,    24, 6000,  "Plant mode covers 7x7.",           0, 0,    0,   0),
                ("harvester",      "Harvester",          EquipmentType.Harvester,  25, 10000, "Unlocks tractor Harvest mode.",    0, 0,    0,   0),
                ("advanced_tractor","Advanced Tractor", EquipmentType.Tractor,    28, 20000, "5x5 plowing at high speed.",      2, 1.6f, 0,   0),
                ("auto_harvester", "Automatic Harvester",EquipmentType.Harvester, 30, 25000, "Harvest mode covers 7x7.",         0, 0,    0,   0),
                ("sprinkler",      "Sprinkler",          EquipmentType.Irrigation, 14, 1500,  "Waters 8 crops every 90s.",       0, 0,    90f, 8),
                ("irrigation",     "Irrigation System",  EquipmentType.Irrigation, 20, 5000,  "Waters 20 crops every 60s.",      0, 0,    60f, 20),
                ("auto_irrigation","Automatic Irrigation",EquipmentType.Irrigation,30, 15000, "Waters everything every 30s.",    0, 0,    30f, 999),
            };
            foreach (var d in equipmentDefs)
            {
                var eq = ScriptableObject.CreateInstance<EquipmentData>();
                eq.equipmentId = d.id;
                eq.displayName = d.name;
                eq.type = d.type;
                eq.unlockLevel = d.level;
                eq.cost = d.cost;
                eq.description = d.desc;
                eq.workRadius = d.radius;
                eq.speedMultiplier = d.speed;
                eq.waterIntervalSeconds = d.waterInterval;
                eq.waterCount = d.waterCount;
                SaveAsset(eq, $"{Root}/Equipment_{d.id}.asset");
                db.equipment.Add(eq);
            }

            var treeDefs = new (string id, string name, int level, int cost, string fruit, float regrow)[]
            {
                ("apple_tree",  "Apple Tree",  17, 250, "apple",  2700f),
                ("orange_tree", "Orange Tree", 19, 300, "orange", 3600f),
                ("mango_tree",  "Mango Tree",  26, 600, "mango",  5400f),
                ("peach_tree",  "Peach Tree",  30, 750, "peach",  7200f),
            };
            foreach (var d in treeDefs)
            {
                var tree = ScriptableObject.CreateInstance<TreeData>();
                tree.treeId = d.id;
                tree.displayName = d.name;
                tree.unlockLevel = d.level;
                tree.saplingCost = d.cost;
                tree.fruitCropId = d.fruit;
                tree.regrowIntervalSeconds = d.regrow;
                SaveAsset(tree, $"{Root}/Tree_{d.id}.asset");
                db.trees.Add(tree);
            }

            SaveAsset(db, $"{Root}/MachineDatabase.asset");
        }

        // ---------------- Phase 5 ----------------

        private static void GenerateProduction(List<CropData> crops)
        {
            var db = AssetDatabase.LoadAssetAtPath<MachineDatabase>($"{Root}/MachineDatabase.asset");
            if (db == null) return;

            // Product items
            var products = new (string id, string name, ItemCategory cat, int sell)[]
            {
                ("flour", "Flour", ItemCategory.Food, 25),
                ("cheese", "Cheese", ItemCategory.Food, 60),
                ("butter", "Butter", ItemCategory.Food, 45),
                ("jam", "Strawberry Jam", ItemCategory.Food, 70),
                ("bread", "Bread", ItemCategory.Food, 50),
                ("cake", "Cake", ItemCategory.Food, 90),
                ("cloth", "Cloth", ItemCategory.Material, 80),
                ("oil", "Sunflower Oil", ItemCategory.Food, 75),
                ("aged_cheese", "Aged Cheese", ItemCategory.Food, 110),
            };
            var productItems = new List<ItemData>();
            foreach (var p in products)
            {
                var item = ScriptableObject.CreateInstance<ItemData>();
                item.itemId = p.id;
                item.displayName = p.name;
                item.category = p.cat;
                item.maxStack = 99;
                item.sellPrice = p.sell;
                SaveAsset(item, $"{Root}/Item_{p.id}.asset");
                productItems.Add(item);
            }
            // Fish items
            var fishDefs = new (string id, string name, FishRarity rarity, int sell, int xp)[]
            {
                ("carp", "Carp", FishRarity.Common, 20, 8),
                ("bass", "Bass", FishRarity.Uncommon, 35, 12),
                ("catfish", "Catfish", FishRarity.Rare, 50, 18),
                ("koi", "Golden Koi", FishRarity.Legendary, 150, 40),
            };
            foreach (var f in fishDefs)
            {
                var item = ScriptableObject.CreateInstance<ItemData>();
                item.itemId = "fish_" + f.id;
                item.displayName = f.name;
                item.category = ItemCategory.Food;
                item.maxStack = 99;
                item.sellPrice = f.sell;
                SaveAsset(item, $"{Root}/Item_fish_{f.id}.asset");
                productItems.Add(item);

                var fish = ScriptableObject.CreateInstance<FishData>();
                fish.fishId = f.id;
                fish.displayName = f.name;
                fish.rarity = f.rarity;
                fish.sellPrice = f.sell;
                fish.xpReward = f.xp;
                SaveAsset(fish, $"{Root}/Fish_{f.id}.asset");
            }

            // Append new items to the main database's item list.
            // Idempotent: remove previously generated entries first so
            // re-running the generator never duplicates items.
            var cropDb = AssetDatabase.LoadAssetAtPath<CropDatabase>($"{Root}/CropDatabase.asset");
            if (cropDb != null)
            {
                var generatedIds = new HashSet<string>();
                foreach (var p in products) generatedIds.Add(p.id);
                foreach (var f in fishDefs) generatedIds.Add("fish_" + f.id);
                cropDb.items.RemoveAll(i => i != null && generatedIds.Contains(i.itemId));
                cropDb.items.AddRange(productItems);
                EditorUtility.SetDirty(cropDb);
            }

            // Recipes
            var recipes = new List<RecipeData>();
            void AddRecipe(string id, string name, string building, int level,
                string[] inputIds, int[] inputCounts,
                string outputId, int outputCount, float seconds, int xp)
            {
                var r = ScriptableObject.CreateInstance<RecipeData>();
                r.recipeId = id;
                r.displayName = name;
                r.buildingId = building;
                r.unlockLevel = level;
                for (int i = 0; i < inputIds.Length; i++)
                    r.inputs.Add(new RecipeIngredient { itemId = inputIds[i], count = inputCounts[i] });
                r.outputItemId = outputId;
                r.outputCount = outputCount;
                r.durationSeconds = seconds;
                r.xpReward = xp;
                SaveAsset(r, $"{Root}/Recipe_{id}.asset");
                recipes.Add(r);
            }

            AddRecipe("flour", "Flour", "mill", 18,
                new[] { "crop_wheat" }, new[] { 3 }, "flour", 2, 300f, 10);
            AddRecipe("feed", "Animal Feed", "feed_mill", 18,
                new[] { "crop_corn" }, new[] { 2 }, "feed", 4, 240f, 8);
            AddRecipe("cheese", "Cheese", "dairy", 21,
                new[] { "milk" }, new[] { 2 }, "cheese", 1, 900f, 15);
            AddRecipe("butter", "Butter", "dairy", 21,
                new[] { "milk" }, new[] { 2 }, "butter", 1, 600f, 12);
            AddRecipe("jam", "Strawberry Jam", "jam_kitchen", 22,
                new[] { "crop_strawberry" }, new[] { 3 }, "jam", 2, 600f, 12);
            AddRecipe("bread", "Bread", "bakery", 23,
                new[] { "flour" }, new[] { 2 }, "bread", 2, 300f, 10);
            AddRecipe("cake", "Cake", "bakery", 25,
                new[] { "flour", "egg" }, new[] { 2, 2 }, "cake", 1, 900f, 20);
            AddRecipe("cloth", "Cloth", "textile", 24,
                new[] { "wool" }, new[] { 2 }, "cloth", 1, 900f, 15);
            AddRecipe("oil", "Sunflower Oil", "oil_press", 26,
                new[] { "crop_sunflower" }, new[] { 3 }, "oil", 1, 600f, 12);
            AddRecipe("aged_cheese", "Aged Cheese", "cheese_cellar", 27,
                new[] { "cheese" }, new[] { 1 }, "aged_cheese", 1, 1800f, 25);

            // Idempotent: rebuild from scratch so re-runs never duplicate.
            db.recipes.Clear();
            db.recipes.AddRange(recipes);
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
        }

        private static void GenerateVillage(List<CropData> crops)
        {
            var db = ScriptableObject.CreateInstance<VillageDatabase>();

            var seedStore = ScriptableObject.CreateInstance<ShopData>();
            seedStore.shopId = "seed_store";
            seedStore.displayName = "Rafiq's Seed Store";
            seedStore.shopkeeperName = "Rafiq";
            seedStore.openHour = 9;
            seedStore.closeHour = 18;
            foreach (var crop in crops)
                seedStore.items.Add(new ShopEntry { itemId = "seed_" + crop.cropId });
            seedStore.items.Add(new ShopEntry { itemId = "feed" });
            seedStore.items.Add(new ShopEntry { itemId = "fertilizer" });
            seedStore.items.Add(new ShopEntry { itemId = "compost" });
            SaveAsset(seedStore, $"{Root}/Shop_seed_store.asset");
            db.shops.Add(seedStore);

            var gardenShop = ScriptableObject.CreateInstance<ShopData>();
            gardenShop.shopId = "garden_shop";
            gardenShop.displayName = "Lina's Garden Shop";
            gardenShop.shopkeeperName = "Lina";
            gardenShop.openHour = 10;
            gardenShop.closeHour = 17;
            foreach (var decorId in new[] { "fence", "flower_bed", "tree", "lamp", "path_stone", "sign", "bench", "birdhouse" })
                gardenShop.items.Add(new ShopEntry { itemId = decorId });
            SaveAsset(gardenShop, $"{Root}/Shop_garden_shop.asset");
            db.shops.Add(gardenShop);

            SaveAsset(db, $"{Root}/VillageDatabase.asset");
        }

        // ---------------- Phase 6 ----------------

        private static void GenerateGameEvents()
        {
            var defs = new (string id, string name, int month, int start, int end,
                string desc, float sell, float xp)[]
            {
                ("new_year", "New Year Fair", 1, 1, 7,
                    "A fresh start for every farm!", 1.1f, 1.5f),
                ("spring_festival", "Spring Festival", 3, 20, 27,
                    "Celebrate the new season!", 1.1f, 1.5f),
                ("spring_planting", "Planting Week", 4, 15, 22,
                    "Double XP on everything!", 1.0f, 2.0f),
                ("midsummer", "Midsummer Fair", 6, 21, 28,
                    "The longest days, the biggest harvests.", 1.1f, 1.5f),
                ("summer_harvest", "Summer Harvest", 7, 10, 17,
                    "Sell your summer crops for more!", 1.15f, 1.25f),
                ("harvest_moon", "Harvest Moon", 9, 20, 27,
                    "Work by moonlight, earn by daylight.", 1.15f, 1.5f),
                ("autumn_fair", "Autumn Fair", 10, 5, 12,
                    "The village celebrates the fall harvest!", 1.2f, 1.25f),
                ("winter_market", "Winter Market", 12, 18, 25,
                    "Holiday shoppers pay top coin!", 1.25f, 1.0f),
            };
            foreach (var d in defs)
            {
                var e = ScriptableObject.CreateInstance<GameEventData>();
                e.eventId = d.id;
                e.displayName = d.name;
                e.month = d.month;
                e.startDay = d.start;
                e.endDay = d.end;
                e.description = d.desc;
                e.sellPriceBonus = d.sell;
                e.xpMultiplier = d.xp;
                SaveAsset(e, $"{Root}/GameEvent_{d.id}.asset");
            }
        }

        private static void SaveAsset(Object asset, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(asset, existing);
            }
            else
            {
                AssetDatabase.CreateAsset(asset, path);
            }
        }
    }
}
