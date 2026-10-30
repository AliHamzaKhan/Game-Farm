using System.Collections.Generic;
using FarmQuest.Core.Save;
using FarmQuest.Core.Services;
using FarmQuest.Systems.Economy;

namespace FarmQuest.Systems.Machines
{
    public enum TractorMode { Plow, Plant, Water, Fertilize, Harvest }

    /// <summary>
    /// LOGIC: equipment ownership (§23). Tractors define work radius/speed;
    /// attachments unlock tractor work modes; irrigation runs on timers.
    /// </summary>
    public class EquipmentService
    {
        private readonly MachineDatabase _database;
        private readonly HashSet<string> _owned = new HashSet<string>();

        public EquipmentService(MachineDatabase database) { _database = database; }

        public bool Has(string equipmentId) => _owned.Contains(equipmentId);
        public IEnumerable<EquipmentData> OwnedEquipment()
        {
            foreach (var id in _owned)
            {
                var data = _database.GetEquipment(id);
                if (data != null) yield return data;
            }
        }

        /// <summary>Grants equipment without payment (bundles, rewards). No-op if owned.</summary>
        public bool Grant(string equipmentId)
        {
            if (_database.GetEquipment(equipmentId) == null || !_owned.Add(equipmentId)) return false;
            GameEvents.RaiseEquipmentBought(equipmentId);
            GameEvents.RaiseEquipmentChanged();
            return true;
        }

        public IEnumerable<EquipmentData> AllEquipment() => _database.equipment;

        public bool HasType(EquipmentType type)
        {
            foreach (var e in OwnedEquipment()) if (e.type == type) return true;
            return false;
        }

        /// <summary>Greenhouse growth-time multiplier (1 = none). Owned greenhouses don't stack.</summary>
        public float GreenhouseGrowthMultiplier()
        {
            float best = 1f;
            foreach (var e in OwnedEquipment())
                if (e.type == EquipmentType.Greenhouse && e.speedMultiplier > 0f)
                    best = UnityEngine.Mathf.Min(best, 1f / e.speedMultiplier);
            return best;
        }

        public bool BuyEquipment(string equipmentId) => BuyEquipment(equipmentId, false);

        /// <summary>Buys with coins, or with gems when the item has a gem price.</summary>
        public bool BuyEquipment(string equipmentId, bool withGems)
        {
            var data = _database.GetEquipment(equipmentId);
            if (data == null || _owned.Contains(equipmentId)) return false;
            var progression = ServiceLocator.Get<ProgressionService>();
            if (!progression.IsLevelUnlocked(data.unlockLevel))
            {
                GameEvents.RaiseToast($"Reach level {data.unlockLevel} for {data.displayName}.");
                return false;
            }
            var economy = ServiceLocator.Get<EconomyService>();
            if (withGems)
            {
                if (data.gemPrice <= 0 || !economy.TrySpendGems(data.gemPrice))
                {
                    GameEvents.RaiseToast(data.gemPrice <= 0 ? "Not sold for gems." : $"Need {data.gemPrice} gems.");
                    return false;
                }
            }
            else if (!economy.TrySpend(data.cost))
            {
                GameEvents.RaiseToast($"Need {data.cost} coins.");
                return false;
            }
            _owned.Add(equipmentId);
            economy.AddXp(40, "buy_equipment");
            GameEvents.RaiseEquipmentBought(equipmentId);
            GameEvents.RaiseEquipmentChanged();
            GameEvents.RaiseToast($"🚜 {data.displayName} acquired!");
            return true;
        }

        /// <summary>Best tractor = highest workRadius, then highest speed.</summary>
        public EquipmentData GetBestTractor()
        {
            EquipmentData best = null;
            foreach (var id in _owned)
            {
                var data = _database.GetEquipment(id);
                if (data == null || data.type != EquipmentType.Tractor) continue;
                if (best == null || data.workRadius > best.workRadius ||
                    (data.workRadius == best.workRadius && data.speedMultiplier > best.speedMultiplier))
                    best = data;
            }
            return best;
        }

        public bool HasTractor() => GetBestTractor() != null;

        public bool IsModeUnlocked(TractorMode mode)
        {
            switch (mode)
            {
                case TractorMode.Plow: return HasTractor();
                case TractorMode.Plant: return HasTractor() && Has("seeder");
                case TractorMode.Fertilize: return HasTractor() && Has("sprayer");
                case TractorMode.Harvest: return HasTractor() && (Has("harvester") || Has("auto_harvester"));
                default: return false;
            }
        }

        public int ModeRadius(TractorMode mode)
        {
            var tractor = GetBestTractor();
            int radius = tractor != null ? tractor.workRadius : 1;
            if (mode == TractorMode.Plant && Has("auto_planter"))
                radius = System.Math.Max(radius, 3);
            if (mode == TractorMode.Harvest && Has("auto_harvester"))
                radius = System.Math.Max(radius, 3);
            return radius;
        }

        /// <summary>Best irrigation = shortest interval among owned.</summary>
        public EquipmentData BestIrrigation()
        {
            EquipmentData best = null;
            foreach (var id in _owned)
            {
                var data = _database.GetEquipment(id);
                if (data == null || data.type != EquipmentType.Irrigation) continue;
                if (best == null || data.waterIntervalSeconds < best.waterIntervalSeconds)
                    best = data;
            }
            return best;
        }

        // ---------- save ----------
        public EquipmentSaveData CaptureState()
        {
            return new EquipmentSaveData { owned = new List<string>(_owned) };
        }

        public void RestoreState(EquipmentSaveData data)
        {
            _owned.Clear();
            if (data == null) return;
            foreach (var id in data.owned) _owned.Add(id);
        }
    }
}
