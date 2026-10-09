using UnityEngine;

namespace FarmQuest.Core.Save
{
    /// <summary>
    /// Migrates old saves forward step by step (§41). When the save format
    /// changes, bump SaveService.CurrentVersion and add a MigrateV(n) step here.
    /// </summary>
    public static class SaveMigrator
    {
        public static SaveData Migrate(SaveData data)
        {
            if (data == null) return new SaveData();

            // Defensive: never trust missing sub-objects from old/hand-edited saves.
            if (data.player == null) data.player = new PlayerSaveData();
            if (data.farm == null) data.farm = new FarmSaveData();
            if (data.inventory == null) data.inventory = new InventorySaveData();
            if (data.economy == null) data.economy = new EconomySaveData();
            if (data.missions == null) data.missions = new MissionSaveData();
            if (data.achievements == null) data.achievements = new AchievementSaveData();
            if (data.story == null) data.story = new StorySaveData();
            if (data.animals == null) data.animals = new AnimalSaveData();
            if (data.pets == null) data.pets = new PetSaveData();
            if (data.buildings == null) data.buildings = new BuildingSaveData();
            if (data.decor == null) data.decor = new DecorSaveData();
            if (data.equipment == null) data.equipment = new EquipmentSaveData();
            if (data.trees == null) data.trees = new TreeSaveData();
            if (data.production == null) data.production = new ProductionSaveData();
            if (data.orders == null) data.orders = new OrderSaveData();
            if (data.bank == null) data.bank = new BankSaveData();
            if (data.village == null) data.village = new VillageSaveData();
            if (data.season == null) data.season = new SeasonSaveData();
            if (data.weather == null) data.weather = new WeatherSaveData();
            if (data.parent == null) data.parent = new ParentSettingsSave();
            if (data.playtime == null) data.playtime = new PlaytimeSaveData();

            while (data.version < SaveService.CurrentVersion)
            {
                switch (data.version)
                {
                    case 0:
                        MigrateV0ToV1(data);
                        break;
                    default:
                        Debug.LogWarning($"[SaveMigrator] Unknown save version {data.version}, stamping current.");
                        data.version = SaveService.CurrentVersion;
                        break;
                }
            }

            return data;
        }

        private static void MigrateV0ToV1(SaveData data)
        {
            // v0 (pre-release dev): economy had no ad tracking fields.
            if (string.IsNullOrEmpty(data.economy.adDayStamp))
                data.economy.adDayStamp = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
            data.version = 1;
        }
    }
}
