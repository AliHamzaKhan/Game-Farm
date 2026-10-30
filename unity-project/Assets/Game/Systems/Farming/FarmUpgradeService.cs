using System.Collections.Generic;
using FarmQuest.Core.Save;
using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Economy;
using UnityEngine;

namespace FarmQuest.Systems.Farming
{
    /// <summary>
    /// LOGIC: coin-bought farm upgrades with levels. Effects are read by
    /// FarmService at planting/harvest, so they persist through the crop's save.
    /// More plots come from FarmService.TryUnlockRegion; machines from EquipmentService.
    /// </summary>
    public class FarmUpgradeService
    {
        private readonly FarmUpgradeDatabase _database;
        private readonly Dictionary<string, int> _levels = new Dictionary<string, int>();

        public FarmUpgradeService(FarmUpgradeDatabase database) { _database = database; }

        public IEnumerable<FarmUpgradeDef> All()
        {
            if (_database == null) yield break;
            foreach (var u in _database.upgrades)
                if (u != null) yield return u;
        }

        public int GetLevel(string upgradeId) => _levels.TryGetValue(upgradeId, out var l) ? l : 0;

        public bool IsMaxed(FarmUpgradeDef def) => GetLevel(def.upgradeId) >= def.maxLevel;

        public long NextCost(FarmUpgradeDef def) => def.CostForLevel(GetLevel(def.upgradeId));

        public bool TryBuy(string upgradeId)
        {
            var def = _database != null ? _database.Get(upgradeId) : null;
            if (def == null || IsMaxed(def)) return false;
            var economy = ServiceLocator.Get<EconomyService>();
            long cost = NextCost(def);
            if (!economy.TrySpend(cost))
            {
                GameEvents.RaiseToast($"Need {cost} coins for {def.displayName}.");
                return false;
            }
            _levels[upgradeId] = GetLevel(upgradeId) + 1;
            economy.AddXp(20, "farm_upgrade");
            GameEvents.RaiseToast($"{def.displayName} upgraded to level {GetLevel(upgradeId)}!");
            GameEvents.RaiseFarmUpgradesChanged();
            return true;
        }

        /// <summary>Sum of value*level over every upgrade with this effect.</summary>
        public float Total(FarmUpgradeEffect effect)
        {
            float total = 0f;
            foreach (var u in All())
                if (u.effect == effect) total += u.valuePerLevel * GetLevel(u.upgradeId);
            return total;
        }

        public float GrowthTimeMultiplier => Mathf.Max(0.3f, 1f - Total(FarmUpgradeEffect.GrowthSpeed));
        public float YieldMultiplier => 1f + Total(FarmUpgradeEffect.YieldBonus);
        public float PestChanceMultiplier => Mathf.Clamp01(1f - Total(FarmUpgradeEffect.PestResistance));
        public float SeedSaverChance => Mathf.Clamp(Total(FarmUpgradeEffect.SeedSaver), 0f, 0.5f);

        // ---------- save ----------
        public FarmUpgradeSaveData CaptureState()
        {
            var data = new FarmUpgradeSaveData();
            foreach (var kv in _levels)
                data.levels.Add(new FarmUpgradeLevelSave { upgradeId = kv.Key, level = kv.Value });
            return data;
        }

        public void RestoreState(FarmUpgradeSaveData data)
        {
            _levels.Clear();
            if (data?.levels == null) return;
            foreach (var e in data.levels)
            {
                if (string.IsNullOrEmpty(e.upgradeId) || e.level <= 0) continue;
                var def = _database != null ? _database.Get(e.upgradeId) : null;
                _levels[e.upgradeId] = def != null ? Mathf.Min(e.level, def.maxLevel) : e.level;
            }
        }
    }
}
