using System;
using System.Collections.Generic;

namespace FarmQuest.Core.Save
{
    // Versioned save containers (§41). JsonUtility-friendly: no Dictionaries,
    // only Lists of serializable entries. Timestamps stored as UTC ticks.

    [Serializable]
    public class SaveData
    {
        public int version = SaveService.CurrentVersion;
        public long savedAtTicks;
        public PlayerSaveData player = new PlayerSaveData();
        public FarmSaveData farm = new FarmSaveData();
        public InventorySaveData inventory = new InventorySaveData();
        public EconomySaveData economy = new EconomySaveData();
        public MissionSaveData missions = new MissionSaveData();
        public AchievementSaveData achievements = new AchievementSaveData();
        public StorySaveData story = new StorySaveData();
        public AnimalSaveData animals = new AnimalSaveData();
        public PetSaveData pets = new PetSaveData();
        public BuildingSaveData buildings = new BuildingSaveData();
        public DecorSaveData decor = new DecorSaveData();
        public EquipmentSaveData equipment = new EquipmentSaveData();
        public TreeSaveData trees = new TreeSaveData();
        public ProductionSaveData production = new ProductionSaveData();
        public OrderSaveData orders = new OrderSaveData();
        public BankSaveData bank = new BankSaveData();
        public VillageSaveData village = new VillageSaveData();
        public SeasonSaveData season = new SeasonSaveData();
        public WeatherSaveData weather = new WeatherSaveData();
        public ParentSettingsSave parent = new ParentSettingsSave();
        public PlaytimeSaveData playtime = new PlaytimeSaveData();
        /// <summary>Last write time (cloud conflict resolution). Phase 6.</summary>
        public long updatedAtTicks;
        /// <summary>Random per-install id for cloud save. Not PII. Phase 6.</summary>
        public string installId = "";
    }

    [Serializable]
    public class PlayerSaveData
    {
        public string playerName = "Farmer";
        public int bodyType; public int skinTone; public int hairStyle; public int hairColor;
        public int shirt; public int pants; public int shoes; public int hat;
        public float posX; public float posZ;
        public int tutorialStep; // TutorialService step index
    }

    [Serializable]
    public class FarmTileSaveData
    {
        public int x; public int y;
        public int regionId;
        public bool unlocked;
        public int soilState;
        public string cropId;            // null/empty when no crop
        public long plantedAtTicks;
        public int wateringsDone;
        public bool compostApplied;
        public bool nutrientApplied;
        public float growthTimeMultiplier = 1f;
        public bool compostPrepared;
        public bool nutrientPrepared;
    }

    [Serializable]
    public class FarmSaveData
    {
        public List<FarmTileSaveData> tiles = new List<FarmTileSaveData>();
        public List<int> unlockedRegions = new List<int>();
    }

    [Serializable]
    public class InventorySlotSaveData
    {
        public string itemId;
        public int count;
        public int quality; // QualityGrade as int
    }

    [Serializable]
    public class InventorySaveData
    {
        public List<InventorySlotSaveData> slots = new List<InventorySlotSaveData>();
    }

    [Serializable]
    public class EconomySaveData
    {
        public long coins;
        public int xp;
        public int level = 1;
        public int rewardedAdsWatchedToday;
        public string adDayStamp; // yyyy-MM-dd UTC, for daily reset
    }

    // ---------- Phase 2: meta progression ----------

    [Serializable]
    public class MissionEntrySave
    {
        public string missionId;
        public int progress;
        public bool completed;
        public bool claimed;
    }

    [Serializable]
    public class MissionSaveData
    {
        public string dayStamp;
        public List<MissionEntrySave> entries = new List<MissionEntrySave>();
    }

    [Serializable]
    public class AchievementSaveData
    {
        public List<string> unlocked = new List<string>();
        public int totalHarvests;
        public int totalCoinsEarned;
        public int totalItemsSold;
        public int missionsCompleted;
        public int regionsUnlocked;
        public int highestStoryChapter;
    }

    [Serializable]
    public class StorySaveData
    {
        public int chapterIndex;
        public List<int> objectiveProgress = new List<int>();
    }

    // ---------- Phase 3: animals, pets, buildings, decor ----------

    [Serializable]
    public class AnimalSaveEntry
    {
        public string animalId;
        public string instanceId;
        public long lastFedAtTicks;
        public long lastCollectedAtTicks;
        public float happiness;
        public long happinessAtTicks;
    }

    [Serializable]
    public class AnimalSaveData
    {
        public List<AnimalSaveEntry> animals = new List<AnimalSaveEntry>();
    }

    [Serializable]
    public class PetSaveData
    {
        public List<string> unlocked = new List<string>();
        public string activePetId;
        public string petName = "";
    }

    [Serializable]
    public class BuildingEntrySave
    {
        public string buildingId;
        public int level;
    }

    [Serializable]
    public class BuildingSaveData
    {
        public List<BuildingEntrySave> entries = new List<BuildingEntrySave>();
    }

    [Serializable]
    public class DecorOwnedSave
    {
        public string decorId;
        public int count;
    }

    [Serializable]
    public class DecorPlacedSave
    {
        public string decorId;
        public float x; public float z; public float rotY;
    }

    [Serializable]
    public class DecorSaveData
    {
        public List<DecorOwnedSave> owned = new List<DecorOwnedSave>();
        public List<DecorPlacedSave> placed = new List<DecorPlacedSave>();
    }

    // ---------- Phase 4: machines & orchard ----------

    [Serializable]
    public class EquipmentSaveData
    {
        public List<string> owned = new List<string>();
    }

    [Serializable]
    public class TreeSaveEntry
    {
        public string treeId;
        public int spotIndex;
        public long lastHarvestedAtTicks;
    }

    [Serializable]
    public class TreeSaveData
    {
        public List<TreeSaveEntry> trees = new List<TreeSaveEntry>();
    }

    // ---------- Phase 5: business & world ----------

    [Serializable]
    public class ProductionSaveEntry
    {
        public string recipeId;
        public long finishAtTicks;
    }

    [Serializable]
    public class ProductionBuildingSave
    {
        public string buildingId;
        public List<ProductionSaveEntry> queue = new List<ProductionSaveEntry>();
    }

    [Serializable]
    public class ProductionSaveData
    {
        public List<ProductionBuildingSave> buildings = new List<ProductionBuildingSave>();
    }

    [Serializable]
    public class OrderItemSave
    {
        public string itemId;
        public int count;
    }

    [Serializable]
    public class OrderSaveEntry
    {
        public string orderId;
        public List<OrderItemSave> items = new List<OrderItemSave>();
        public int rewardCoins;
        public int rewardXp;
        public long expiresAtTicks;
        public bool isPremium;
    }

    [Serializable]
    public class OrderSaveData
    {
        public List<OrderSaveEntry> orders = new List<OrderSaveEntry>();
        public long lastGenerationTicks;
    }

    [Serializable]
    public class BankSaveData
    {
        public long balance;
    }

    [Serializable]
    public class VillageSaveData
    {
        public int dayCount;
    }

    [Serializable]
    public class SeasonSaveData
    {
        public int daysElapsed;
    }

    [Serializable]
    public class WeatherSaveData
    {
        public int current;
        public long nextChangeTicks;
    }

    // ---------- Phase 6: launch readiness ----------

    [Serializable]
    public class ParentSettingsSave
    {
        public bool adsAllowed = true;
        public bool analyticsAllowed = true;
        public int maxDailyPlayMinutes;
    }

    [Serializable]
    public class PlaytimeSaveData
    {
        public float dailySeconds;
        public string dayStamp = "";
    }
}
