using System;
using FarmQuest.Systems.Farming;

namespace FarmQuest.Core.Services
{
    /// <summary>
    /// Central event bus. Systems communicate through these events instead of
    /// direct references, keeping DATA / LOGIC / VIEW / UI decoupled (§51).
    /// UI and views subscribe; services raise. Call Reset() in tests.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<long> CoinsChanged;
        public static event Action<int, int> LevelChanged;      // level, xpIntoLevel
        public static event Action<int> LevelUp;               // new level
        public static event Action<string, int> InventoryChanged; // itemId, newCount
        public static event Action<FarmTile> TileChanged;
        public static event Action<CropInstance> CropReady;
        public static event Action<CropInstance, int> CropHarvested; // crop, quantity
        public static event Action<string> ToastRequested;     // small UI message

        // ---- Phase 2: granular farming events ----
        public static event Action TileCleared;
        public static event Action TileDug;
        public static event Action TilePrepared;
        public static event Action<string> CropPlanted;        // cropId
        public static event Action CropWatered;
        public static event Action<string, int> ItemsSold;     // itemId, quantity
        public static event Action<long> CoinsEarned;
        public static event Action<int> RegionUnlocked;        // regionId

        // ---- Phase 2: meta progression ----
        public static event Action MissionsChanged;            // progress regenerated/updated
        public static event Action<string> MissionCompleted;    // missionId
        public static event Action<string> MissionClaimed;
        public static event Action<string> AchievementUnlocked; // achievementId
        public static event Action<int> StoryChapterCompleted; // chapterIndex
        public static event Action<string> TutorialHintChanged; // hint text ("" = hide)

        // ---- Phase 3: animals, buildings, pets, decor ----
        public static event Action AnimalsChanged;
        public static event Action<string> AnimalProductReady;  // instanceId
        public static event Action<string> AnimalBought;        // animalId
        public static event Action<string, int> BuildingUpgraded; // buildingId, newLevel
        public static event Action<string> PetUnlocked;          // petId
        public static event Action DecorChanged;

        // ---- Phase 4: machines ----
        public static event Action EquipmentChanged;
        public static event Action<string> EquipmentBought;      // equipmentId
        public static event Action OrchardChanged;

        // ---- Phase 5: business & world ----
        public static event Action ProductionChanged;
        public static event Action<string> ProductionReady;       // buildingId
        public static event Action OrdersChanged;
        public static event Action BankChanged;
        public static event Action WeatherChanged;
        public static event Action SeasonChanged;

        public static void RaiseCoinsChanged(long coins) => CoinsChanged?.Invoke(coins);
        public static void RaiseLevelChanged(int level, int xpIntoLevel) => LevelChanged?.Invoke(level, xpIntoLevel);
        public static void RaiseLevelUp(int newLevel) => LevelUp?.Invoke(newLevel);
        public static void RaiseInventoryChanged(string itemId, int newCount) => InventoryChanged?.Invoke(itemId, newCount);
        public static void RaiseTileChanged(FarmTile tile) => TileChanged?.Invoke(tile);
        public static void RaiseCropReady(CropInstance crop) => CropReady?.Invoke(crop);
        public static void RaiseCropHarvested(CropInstance crop, int quantity) => CropHarvested?.Invoke(crop, quantity);
        public static void RaiseToast(string message) => ToastRequested?.Invoke(message);
        public static void RaiseTileCleared() => TileCleared?.Invoke();
        public static void RaiseTileDug() => TileDug?.Invoke();
        public static void RaiseTilePrepared() => TilePrepared?.Invoke();
        public static void RaiseCropPlanted(string cropId) => CropPlanted?.Invoke(cropId);
        public static void RaiseCropWatered() => CropWatered?.Invoke();
        public static void RaiseItemsSold(string itemId, int quantity) => ItemsSold?.Invoke(itemId, quantity);
        public static void RaiseCoinsEarned(long amount) => CoinsEarned?.Invoke(amount);
        public static void RaiseRegionUnlocked(int regionId) => RegionUnlocked?.Invoke(regionId);
        public static void RaiseMissionsChanged() => MissionsChanged?.Invoke();
        public static void RaiseMissionCompleted(string missionId) => MissionCompleted?.Invoke(missionId);
        public static void RaiseMissionClaimed(string missionId) => MissionClaimed?.Invoke(missionId);
        public static void RaiseAchievementUnlocked(string achievementId) => AchievementUnlocked?.Invoke(achievementId);
        public static void RaiseStoryChapterCompleted(int chapterIndex) => StoryChapterCompleted?.Invoke(chapterIndex);
        public static void RaiseTutorialHintChanged(string hint) => TutorialHintChanged?.Invoke(hint);
        public static void RaiseAnimalsChanged() => AnimalsChanged?.Invoke();
        public static void RaiseAnimalProductReady(string instanceId) => AnimalProductReady?.Invoke(instanceId);
        public static void RaiseAnimalBought(string animalId) => AnimalBought?.Invoke(animalId);
        public static void RaiseBuildingUpgraded(string buildingId, int newLevel) => BuildingUpgraded?.Invoke(buildingId, newLevel);
        public static void RaisePetUnlocked(string petId) => PetUnlocked?.Invoke(petId);
        public static void RaiseDecorChanged() => DecorChanged?.Invoke();
        public static void RaiseEquipmentChanged() => EquipmentChanged?.Invoke();
        public static void RaiseEquipmentBought(string equipmentId) => EquipmentBought?.Invoke(equipmentId);
        public static void RaiseOrchardChanged() => OrchardChanged?.Invoke();
        public static void RaiseProductionChanged() => ProductionChanged?.Invoke();
        public static void RaiseProductionReady(string buildingId) => ProductionReady?.Invoke(buildingId);
        public static void RaiseOrdersChanged() => OrdersChanged?.Invoke();
        public static void RaiseBankChanged() => BankChanged?.Invoke();
        public static void RaiseWeatherChanged() => WeatherChanged?.Invoke();
        public static void RaiseSeasonChanged() => SeasonChanged?.Invoke();

        public static void Reset()
        {
            CoinsChanged = null;
            LevelChanged = null;
            LevelUp = null;
            InventoryChanged = null;
            TileChanged = null;
            CropReady = null;
            CropHarvested = null;
            ToastRequested = null;
            TileCleared = null;
            TileDug = null;
            TilePrepared = null;
            CropPlanted = null;
            CropWatered = null;
            ItemsSold = null;
            CoinsEarned = null;
            RegionUnlocked = null;
            MissionsChanged = null;
            MissionCompleted = null;
            MissionClaimed = null;
            AchievementUnlocked = null;
            StoryChapterCompleted = null;
            TutorialHintChanged = null;
            AnimalsChanged = null;
            AnimalProductReady = null;
            AnimalBought = null;
            BuildingUpgraded = null;
            PetUnlocked = null;
            DecorChanged = null;
            EquipmentChanged = null;
            EquipmentBought = null;
            OrchardChanged = null;
            ProductionChanged = null;
            ProductionReady = null;
            OrdersChanged = null;
            BankChanged = null;
            WeatherChanged = null;
            SeasonChanged = null;
        }
    }
}
