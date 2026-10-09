using System.Collections.Generic;
using FarmQuest.Core.Save;
using FarmQuest.Core.Services;
using FarmQuest.Systems.Animals;
using FarmQuest.Systems.Economy;

namespace FarmQuest.Systems.Buildings
{
    /// <summary>
    /// LOGIC: building ownership + upgrades (§24). Every upgrade must change
    /// something visible (spec §60) — views listen to BuildingUpgraded.
    /// Level 0 = not built; level 1+ = built/upgraded.
    /// </summary>
    public class BuildingService
    {
        private readonly AnimalDatabase _database;
        private readonly Dictionary<string, int> _levels = new Dictionary<string, int>();

        public BuildingService(AnimalDatabase database) { _database = database; }

        public int GetLevel(string buildingId)
        {
            _levels.TryGetValue(buildingId, out int level);
            return level;
        }

        public bool IsBuilt(string buildingId) => GetLevel(buildingId) >= 1;

        public BuildingData GetData(string buildingId) => _database.GetBuilding(buildingId);

        public int MaxLevel(string buildingId)
        {
            var data = GetData(buildingId);
            return data != null ? data.levels.Count : 0;
        }

        /// <summary>Builds (0→1) or upgrades. Returns false if maxed or unaffordable.</summary>
        public bool BuyOrUpgrade(string buildingId)
        {
            var data = GetData(buildingId);
            if (data == null) return false;
            int current = GetLevel(buildingId);
            if (current >= data.levels.Count)
            {
                GameEvents.RaiseToast("Already at max level!");
                return false;
            }
            int cost = data.levels[current].cost; // levels[0] is L1 state; cost to reach it
            var economy = ServiceLocator.Get<EconomyService>();
            if (!economy.TrySpend(cost))
            {
                GameEvents.RaiseToast($"Need {cost} coins.");
                return false;
            }
            int next = current + 1;
            _levels[buildingId] = next;
            economy.AddXp(50 * next, "building");
            GameEvents.RaiseBuildingUpgraded(buildingId, next);
            GameEvents.RaiseToast($"🏠 {data.displayName} → {data.levels[next - 1].levelName}!");
            return true;
        }

        /// <summary>Grants a starting level without cost (e.g. house L1 at game start).</summary>
        public void GrantLevel(string buildingId, int level)
        {
            _levels[buildingId] = level;
            GameEvents.RaiseBuildingUpgraded(buildingId, level);
        }

        // ---------- save ----------
        public BuildingSaveData CaptureState()
        {
            var data = new BuildingSaveData();
            foreach (var kvp in _levels)
                data.entries.Add(new BuildingEntrySave { buildingId = kvp.Key, level = kvp.Value });
            return data;
        }

        public void RestoreState(BuildingSaveData data)
        {
            _levels.Clear();
            if (data == null) return;
            foreach (var e in data.entries)
                _levels[e.buildingId] = e.level;
        }
    }
}
