using System.Collections;
using FarmQuest.Core.Config;
using FarmQuest.Core.Save;
using FarmQuest.Core.Time;
using FarmQuest.Data;
using FarmQuest.Systems.Ads;
using FarmQuest.Systems.Analytics;
using FarmQuest.Systems.CloudSave;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Events;
using FarmQuest.Systems.Farming;
using FarmQuest.Systems.Inventory;
using FarmQuest.Systems.Animals;
using FarmQuest.Systems.Buildings;
using FarmQuest.Systems.Decor;
using FarmQuest.Systems.Machines;
using FarmQuest.Systems.Missions;
using FarmQuest.Systems.Orchard;
using FarmQuest.Systems.Orders;
using FarmQuest.Systems.Processing;
using FarmQuest.Systems.Village;
using FarmQuest.Systems.World;
using FarmQuest.Systems.Npcs;
using FarmQuest.Systems.ParentMode;
using FarmQuest.Systems.Story;
using System.Collections.Generic;
using UnityEngine;

namespace FarmQuest.Core.Services
{
    /// <summary>
    /// Composition root: builds every service in dependency order, loads or
    /// creates the save, starts autosave. Attach to a persistent GameObject in
    /// the bootstrap scene. Runs before any gameplay scene logic.
    /// Order follows spec §59.
    /// </summary>
    public class Bootstrapper : MonoBehaviour
    {
        [Header("Assign data assets (or generate via FarmQuest/Generate/All Game Data)")]
        public CropDatabase cropDatabase;
        public ProgressionData progressionData;
        public FarmLayoutData farmLayout;
        public BalanceConfig balanceConfig;
        public AnimalDatabase animalDatabase;
        public MachineDatabase machineDatabase;
        public VillageDatabase villageDatabase;

        [Header("Phase 2 data pools (or leave empty to load from Resources/Data)")]
        public List<MissionData> missionPool = new List<MissionData>();
        public List<AchievementData> achievementPool = new List<AchievementData>();
        public List<NpcData> npcPool = new List<NpcData>();
        public List<StoryChapterData> storyChapters = new List<StoryChapterData>();
        public List<GameEventData> gameEventPool = new List<GameEventData>();

        [Header("Tuning")]
        public float autosaveIntervalSeconds = 60f;
        public long startingCoins = 100;
        public int startingCarrotSeeds = 5;
        public int startingTomatoSeeds = 5;

        private SaveCoordinator _coordinator;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            // AddComponent runs Awake synchronously, so TimeTicker registers
            // ITimeService before the line below executes. Single clock instance.
            gameObject.AddComponent<TimeTicker>();
            var time = ServiceLocator.Get<ITimeService>();

            var saveService = new SaveService();
            ServiceLocator.Register(saveService);

            if (cropDatabase == null) cropDatabase = Resources.Load<CropDatabase>("Data/CropDatabase");
            if (progressionData == null) progressionData = Resources.Load<ProgressionData>("Data/Progression");
            if (farmLayout == null) farmLayout = Resources.Load<FarmLayoutData>("Data/FarmLayout");
            if (balanceConfig == null) balanceConfig = Resources.Load<BalanceConfig>("Data/BalanceConfig");
            if (cropDatabase == null) Debug.LogError("[Bootstrapper] CropDatabase missing. Run FarmQuest/Generate/All Game Data.");
            // Fail fast with a clear message instead of a NullReference cascade.
            if (cropDatabase == null || progressionData == null || farmLayout == null || balanceConfig == null)
            {
                Debug.LogError("[Bootstrapper] Boot aborted: required data assets are missing. " +
                    "Run FarmQuest/Generate/All Game Data, then press Play.");
                return;
            }
            cropDatabase?.Initialize();

            ServiceLocator.Register(cropDatabase);
            ServiceLocator.Register(progressionData);
            ServiceLocator.Register(farmLayout);
            IRemoteConfigProvider remoteConfig = new LocalRemoteConfigProvider(balanceConfig);
            // Phase 6: HTTP layer for backend-driven tuning; falls back to local values.
            var httpConfig = new HttpRemoteConfigProvider(remoteConfig);
            ServiceLocator.Register<IRemoteConfigProvider>(httpConfig);
            // Best-effort fetch; no-op when the endpoint isn't configured.
            _ = httpConfig.FetchAsync(
                remoteConfig.GetString("cloud_save_endpoint", ""),
                remoteConfig.GetString("cloud_save_api_key", ""));

            ServiceLocator.Register(new InventoryService(cropDatabase));
            ServiceLocator.Register(new EconomyService());
            ServiceLocator.Register(new ProgressionService(progressionData));
            ServiceLocator.Register(new FarmService(farmLayout, cropDatabase, time));
            ServiceLocator.Register(new CropGrowthService(time));
            ServiceLocator.Register(new TimeAccelerationService());
            ServiceLocator.Register(new MarketService(cropDatabase, new FixedPriceStrategy()));
            ServiceLocator.Register(new RewardedAdService(httpConfig, time,
                new StubAdProvider())); // Phase 6: swap for real provider

            // ---- Phase 2: meta progression ----
            if (missionPool.Count == 0)
                missionPool.AddRange(Resources.LoadAll<MissionData>("Data"));
            if (achievementPool.Count == 0)
                achievementPool.AddRange(Resources.LoadAll<AchievementData>("Data"));
            if (npcPool.Count == 0)
                npcPool.AddRange(Resources.LoadAll<NpcData>("Data"));
            if (storyChapters.Count == 0)
                storyChapters.AddRange(Resources.LoadAll<StoryChapterData>("Data"));

            ServiceLocator.Register(new MissionService(missionPool));
            ServiceLocator.Register(new AchievementService(achievementPool));
            ServiceLocator.Register(new NpcService(npcPool));
            ServiceLocator.Register(new StoryService(storyChapters));
            ServiceLocator.Register(new TutorialService());

            // ---- Phase 3: animals, pets, buildings, decor ----
            if (animalDatabase == null) animalDatabase = Resources.Load<AnimalDatabase>("Data/AnimalDatabase");
            if (animalDatabase == null) Debug.LogError("[Bootstrapper] AnimalDatabase missing. Run FarmQuest/Generate/All Game Data.");
            animalDatabase?.Initialize();
            ServiceLocator.Register(animalDatabase);
            ServiceLocator.Register(new AnimalService(animalDatabase, time));
            ServiceLocator.Register(new PetService(animalDatabase));
            ServiceLocator.Register(new BuildingService(animalDatabase));
            ServiceLocator.Register(new DecorService(animalDatabase));

            // ---- Phase 4: machines & orchard ----
            if (machineDatabase == null) machineDatabase = Resources.Load<MachineDatabase>("Data/MachineDatabase");
            if (machineDatabase == null) Debug.LogError("[Bootstrapper] MachineDatabase missing. Run FarmQuest/Generate/All Game Data.");
            machineDatabase?.Initialize();
            ServiceLocator.Register(machineDatabase);
            var cropDb = ServiceLocator.Get<CropDatabase>();
            ServiceLocator.Register(new EquipmentService(machineDatabase));
            ServiceLocator.Register(new IrrigationService(time));
            ServiceLocator.Register(new OrchardService(machineDatabase, cropDb, time));

            // ---- Phase 5: business & world ----
            ServiceLocator.Register(new ProductionService(machineDatabase, time));
            ServiceLocator.Register(new OrderService(cropDb, time));
            if (villageDatabase == null) villageDatabase = Resources.Load<VillageDatabase>("Data/VillageDatabase");
            if (villageDatabase == null) Debug.LogError("[Bootstrapper] VillageDatabase missing. Run FarmQuest/Generate/All Game Data.");
            ServiceLocator.Register(villageDatabase);
            ServiceLocator.Register(new VillageService(villageDatabase, time));
            ServiceLocator.Register(new BankService(time));
            ServiceLocator.Register(new SeasonService(time));
            ServiceLocator.Register(new WeatherService(time));

            // ---- Phase 6: launch readiness ----
            ServiceLocator.Register(new ParentModeService());
            ServiceLocator.Register(new PlaytimeService(time));
            if (gameEventPool.Count == 0)
                gameEventPool.AddRange(Resources.LoadAll<GameEventData>("Data"));
            var liveEvents = new LiveEventService(gameEventPool);
            ServiceLocator.Register(liveEvents);
            // Swap StubAdProvider for UnityAdsProvider once the Ads package is installed (see its docs).
            ServiceLocator.Register(new CloudSaveService(new HttpCloudSaveProvider()));
            var analytics = new AnalyticsService(new HttpAnalyticsProvider());
            ServiceLocator.Register(analytics);

            _coordinator = new SaveCoordinator(saveService);
            ServiceLocator.Register(_coordinator);

            bool hadSave = saveService.HasSave();
            _coordinator.TryRestoreAll(out _);
            if (!hadSave) GrantStartingResources();
            ServiceLocator.Get<MissionService>().EnsureDailyMissions();
            liveEvents.RefreshBonuses();
            analytics.AttachAutoTracking();
            CheckCloudSave();

            StartCoroutine(AutosaveLoop());
        }

        /// <summary>Best-effort: adopt a newer cloud save if one exists.</summary>
        private async void CheckCloudSave()
        {
            var cloud = ServiceLocator.Get<CloudSaveService>();
            var local = _coordinator.LastData;
            long localTicks = local != null ? local.updatedAtTicks : 0;
            string json = await cloud.DownloadNewerSaveAsync(_coordinator.InstallId, localTicks);
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null) return;
                _coordinator.RestoreFromData(data);
                ServiceLocator.Get<LiveEventService>().RefreshBonuses();
                GameEvents.RaiseToast("☁️ Loaded your cloud save!");
            }
            catch (System.Exception e)
            {
                Debug.Log($"[Bootstrapper] Cloud save parse failed: {e.Message}");
            }
        }

        private void SaveAll()
        {
            _coordinator.SaveAll();
            // Fire-and-forget: local save is authoritative; cloud is a backup.
            var cloud = ServiceLocator.Get<CloudSaveService>();
            var data = _coordinator.LastData;
            if (data != null)
            {
                string json = JsonUtility.ToJson(data);
                _ = cloud.UploadSaveAsync(_coordinator.InstallId, json, data.updatedAtTicks);
            }
            ServiceLocator.Get<AnalyticsService>().Flush();
        }

        private void GrantStartingResources()
        {
            var economy = ServiceLocator.Get<EconomyService>();
            var inventory = ServiceLocator.Get<InventoryService>();
            economy.AddCoins(startingCoins, "new_game");
            inventory.Add("seed_carrot", startingCarrotSeeds);
            inventory.Add("seed_tomato", startingTomatoSeeds);
            inventory.Add("compost", 2);
            // Grandpa's gifts: a dog companion and the small farmhouse.
            ServiceLocator.Get<PetService>().UnlockPet("dog");
            ServiceLocator.Get<BuildingService>().GrantLevel("house", 1);
            Debug.Log("[Bootstrapper] New game started with starter resources.");
        }

        private IEnumerator AutosaveLoop()
        {
            var wait = new WaitForSeconds(autosaveIntervalSeconds);
            while (true)
            {
                yield return wait;
                SaveAll();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveAll();
        }

        private void OnApplicationQuit()
        {
            SaveAll();
        }
    }
}
