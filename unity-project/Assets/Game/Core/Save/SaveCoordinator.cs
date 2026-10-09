using FarmQuest.Core.Services;
using FarmQuest.Systems.Economy;
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
using FarmQuest.Systems.Story;
using FarmQuest.Systems.ParentMode;
using FarmQuest.Systems.Village;
using FarmQuest.Systems.World;

namespace FarmQuest.Core.Save
{
    /// <summary>
    /// Builds a SaveData snapshot from all services and restores it back.
    /// Services own Capture/Restore; this class only orchestrates.
    /// </summary>
    public class SaveCoordinator
    {
        private readonly SaveService _saveService;

        public SaveCoordinator(SaveService saveService) { _saveService = saveService; }

        public string InstallId { get; private set; }
        public SaveData LastData { get; private set; }
        public bool HadSave { get; private set; }

        public SaveData CaptureAll()
        {
            var data = new SaveData
            {
                farm = ServiceLocator.Get<FarmService>().CaptureState(),
                inventory = ServiceLocator.Get<InventoryService>().CaptureState(),
                economy = ServiceLocator.Get<EconomyService>().CaptureState(),
                player = ServiceLocator.TryGet(out Gameplay.Player.PlayerController playerCtrl)
                    ? playerCtrl.CaptureState()
                    : new PlayerSaveData(),
                missions = ServiceLocator.Get<MissionService>().CaptureState(),
                achievements = ServiceLocator.Get<AchievementService>().CaptureState(),
                story = ServiceLocator.Get<StoryService>().CaptureState(),
                animals = ServiceLocator.Get<AnimalService>().CaptureState(),
                pets = ServiceLocator.Get<PetService>().CaptureState(),
                buildings = ServiceLocator.Get<BuildingService>().CaptureState(),
                decor = ServiceLocator.Get<DecorService>().CaptureState(),
                equipment = ServiceLocator.Get<EquipmentService>().CaptureState(),
                trees = ServiceLocator.Get<OrchardService>().CaptureState(),
                production = ServiceLocator.Get<ProductionService>().CaptureState(),
                orders = ServiceLocator.Get<OrderService>().CaptureState(),
                bank = ServiceLocator.Get<BankService>().CaptureState(),
                village = ServiceLocator.Get<VillageService>().CaptureState(),
                season = ServiceLocator.Get<SeasonService>().CaptureState(),
                weather = ServiceLocator.Get<WeatherService>().CaptureState(),
                parent = ServiceLocator.Get<ParentModeService>().CaptureState(),
                playtime = ServiceLocator.Get<PlaytimeService>().CaptureState(),
                updatedAtTicks = System.DateTime.UtcNow.Ticks,
                installId = InstallId
            };
            return data;
        }

        public void SaveAll()
        {
            var data = CaptureAll();
            LastData = data;
            _saveService.Save(data);
        }

        public bool TryRestoreAll(out bool hadSave)
        {
            hadSave = _saveService.TryLoad(out var data);
            HadSave = hadSave;
            if (!hadSave || data == null)
            {
                data = new SaveData();
            }
            RestoreFromData(data);
            return true;
        }

        /// <summary>Restores from an explicit SaveData (local load or cloud download).</summary>
        public void RestoreFromData(SaveData data)
        {
            data = SaveMigrator.Migrate(data);
            LastData = data;
            InstallId = string.IsNullOrEmpty(data.installId)
                ? System.Guid.NewGuid().ToString("N")
                : data.installId;
            data.installId = InstallId;
            ServiceLocator.Get<FarmService>().RestoreState(data.farm);
            ServiceLocator.Get<InventoryService>().RestoreState(data.inventory);
            ServiceLocator.Get<EconomyService>().RestoreState(data.economy);
            ServiceLocator.Get<MissionService>().RestoreState(data.missions);
            ServiceLocator.Get<AchievementService>().RestoreState(data.achievements);
            ServiceLocator.Get<StoryService>().RestoreState(data.story);
            ServiceLocator.Get<AnimalService>().RestoreState(data.animals);
            ServiceLocator.Get<PetService>().RestoreState(data.pets);
            ServiceLocator.Get<BuildingService>().RestoreState(data.buildings);
            ServiceLocator.Get<DecorService>().RestoreState(data.decor);
            ServiceLocator.Get<EquipmentService>().RestoreState(data.equipment);
            ServiceLocator.Get<OrchardService>().RestoreState(data.trees);
            ServiceLocator.Get<ProductionService>().RestoreState(data.production);
            ServiceLocator.Get<OrderService>().RestoreState(data.orders);
            ServiceLocator.Get<BankService>().RestoreState(data.bank);
            ServiceLocator.Get<VillageService>().RestoreState(data.village);
            ServiceLocator.Get<SeasonService>().RestoreState(data.season);
            ServiceLocator.Get<WeatherService>().RestoreState(data.weather);
            ServiceLocator.Get<ParentModeService>().RestoreState(data.parent);
            ServiceLocator.Get<PlaytimeService>().RestoreState(data.playtime);
            if (ServiceLocator.TryGet(out Gameplay.Player.PlayerController player))
                player.RestoreState(data.player);
            ServiceLocator.Get<TutorialService>().Restore(data.player.tutorialStep);
        }
    }
}
