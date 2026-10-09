using System.Collections.Generic;
using FarmQuest.Core.Save;
using FarmQuest.Core.Services;
using FarmQuest.Core.Time;
using FarmQuest.Data;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Inventory;
using FarmQuest.Systems.Machines;

namespace FarmQuest.Systems.Orchard
{
    /// <summary>
    /// LOGIC: orchard trees (§16). Saplings planted once into fixed spots;
    /// fruit regrows forever. Harvests flow through inventory + XP like crops.
    /// </summary>
    public class OrchardService
    {
        public const int MaxSpots = 12;

        private readonly MachineDatabase _database;
        private readonly CropDatabase _cropDatabase;
        private readonly ITimeService _time;
        private readonly List<TreeInstance> _trees = new List<TreeInstance>();
        private readonly System.Random _rng = new System.Random();

        public IReadOnlyList<TreeInstance> Trees => _trees;

        public OrchardService(MachineDatabase database, CropDatabase cropDatabase, ITimeService time)
        {
            _database = database;
            _cropDatabase = cropDatabase;
            _time = time;
        }

        public int FreeSpots()
        {
            var used = new HashSet<int>();
            foreach (var t in _trees) used.Add(t.SpotIndex);
            int free = 0;
            for (int i = 0; i < MaxSpots; i++)
                if (!used.Contains(i)) free++;
            return free;
        }

        private int NextFreeSpot()
        {
            var used = new HashSet<int>();
            foreach (var t in _trees) used.Add(t.SpotIndex);
            for (int i = 0; i < MaxSpots; i++)
                if (!used.Contains(i)) return i;
            return -1;
        }

        public bool PlantTree(string treeId)
        {
            var data = _database.GetTree(treeId);
            if (data == null) return false;
            var progression = ServiceLocator.Get<ProgressionService>();
            if (!progression.IsLevelUnlocked(data.unlockLevel))
            {
                GameEvents.RaiseToast($"Reach level {data.unlockLevel} for {data.displayName}.");
                return false;
            }
            int spot = NextFreeSpot();
            if (spot < 0)
            {
                GameEvents.RaiseToast("Orchard is full!");
                return false;
            }
            var economy = ServiceLocator.Get<EconomyService>();
            if (!economy.TrySpend(data.saplingCost))
            {
                GameEvents.RaiseToast($"Need {data.saplingCost} coins.");
                return false;
            }
            _trees.Add(new TreeInstance(data, spot, _time));
            economy.AddXp(15, "plant_tree");
            GameEvents.RaiseOrchardChanged();
            GameEvents.RaiseToast($"🌳 {data.displayName} planted!");
            return true;
        }

        /// <summary>Harvests a ready tree. Returns fruit count.</summary>
        public int HarvestTree(TreeInstance tree)
        {
            if (tree == null) return 0;
            var crop = _cropDatabase.GetCrop(tree.Data.fruitCropId);
            int qty = tree.Harvest(() => crop != null
                ? _rng.Next(crop.harvestMin, crop.harvestMax + 1)
                : 6);
            if (qty <= 0) return 0;
            ServiceLocator.Get<InventoryService>().Add("crop_" + tree.Data.fruitCropId, qty);
            var economy = ServiceLocator.Get<EconomyService>();
            economy.AddXp(tree.Data.xpReward, "harvest_tree");
            GameEvents.RaiseCropHarvested(null, qty); // missions/achievements count tree harvests
            GameEvents.RaiseToast($"🧺 +{qty} {tree.Data.displayName.Replace(" Tree", "")}!");
            return qty;
        }

        // ---------- save ----------
        public TreeSaveData CaptureState()
        {
            var data = new TreeSaveData();
            foreach (var t in _trees) data.trees.Add(t.CaptureState());
            return data;
        }

        public void RestoreState(TreeSaveData data)
        {
            _trees.Clear();
            if (data == null) return;
            foreach (var entry in data.trees)
            {
                var treeData = _database.GetTree(entry.treeId);
                if (treeData == null) continue;
                var tree = new TreeInstance(treeData, entry.spotIndex, _time);
                tree.RestoreState(entry);
                _trees.Add(tree);
            }
        }
    }
}
