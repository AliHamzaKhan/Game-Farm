using System.Collections.Generic;
using FarmQuest.Core.Save;
using FarmQuest.Core.Services;
using FarmQuest.Core.Time;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Inventory;
using FarmQuest.Systems.Machines;

namespace FarmQuest.Systems.Processing
{
    /// <summary>
    /// LOGIC: machine buildings with production queues (§20). Start a recipe →
    /// inputs consumed → real-time batch → collect output. Offline-safe via
    /// timestamps; cancel refunds inputs (kid-friendly).
    /// </summary>
    public class ProductionService
    {
        public const int QueueCapacity = 2;

        public static readonly Dictionary<string, string> BuildingNames =
            new Dictionary<string, string>
            {
                { "mill", "Grain Mill" },
                { "feed_mill", "Feed Mill" },
                { "dairy", "Dairy" },
                { "jam_kitchen", "Jam Kitchen" },
                { "bakery", "Bakery" },
                { "textile", "Textile Mill" },
                { "oil_press", "Oil Press" },
                { "cheese_cellar", "Cheese Cellar" },
            };

        private readonly MachineDatabase _database;
        private readonly ITimeService _time;
        private readonly Dictionary<string, List<ProductionInstance>> _queues =
            new Dictionary<string, List<ProductionInstance>>();

        public ProductionService(MachineDatabase database, ITimeService time)
        {
            _database = database;
            _time = time;
            _time.SecondTick += PollReady;
        }

        public IReadOnlyList<ProductionInstance> QueueFor(string buildingId)
        {
            if (_queues.TryGetValue(buildingId, out var q)) return q;
            return System.Array.Empty<ProductionInstance>();
        }

        public int ReadyCount(string buildingId)
        {
            int n = 0;
            foreach (var p in QueueFor(buildingId))
                if (p.IsReady) n++;
            return n;
        }

        public bool StartProduction(string recipeId)
        {
            var recipe = _database.GetRecipe(recipeId);
            if (recipe == null) return false;
            var progression = ServiceLocator.Get<ProgressionService>();
            if (!progression.IsLevelUnlocked(recipe.unlockLevel))
            {
                GameEvents.RaiseToast($"Reach level {recipe.unlockLevel} for {recipe.displayName}.");
                return false;
            }
            if (!_queues.TryGetValue(recipe.buildingId, out var queue))
            {
                queue = new List<ProductionInstance>();
                _queues[recipe.buildingId] = queue;
            }
            if (queue.Count >= QueueCapacity)
            {
                GameEvents.RaiseToast("Queue is full — collect finished goods first!");
                return false;
            }
            var inventory = ServiceLocator.Get<InventoryService>();
            foreach (var input in recipe.inputs)
                if (inventory.GetCount(input.itemId) < input.count)
                {
                    GameEvents.RaiseToast($"Need {input.count}× {input.itemId}!");
                    return false;
                }
            foreach (var input in recipe.inputs)
                inventory.TryRemove(input.itemId, input.count);

            queue.Add(new ProductionInstance(recipe, _time));
            GameEvents.RaiseProductionChanged();
            GameEvents.RaiseToast($"⚙️ {recipe.displayName} started!");
            return true;
        }

        /// <summary>Collects all finished batches in a building.</summary>
        public int CollectBuilding(string buildingId)
        {
            if (!_queues.TryGetValue(buildingId, out var queue)) return 0;
            var inventory = ServiceLocator.Get<InventoryService>();
            var economy = ServiceLocator.Get<EconomyService>();
            int collected = 0;
            for (int i = queue.Count - 1; i >= 0; i--)
            {
                if (!queue[i].IsReady) continue;
                var recipe = queue[i].Recipe;
                inventory.Add(recipe.outputItemId, recipe.outputCount);
                economy.AddXp(recipe.xpReward, "production");
                queue.RemoveAt(i);
                collected++;
            }
            if (collected > 0)
            {
                GameEvents.RaiseProductionChanged();
                GameEvents.RaiseToast($"📦 Collected {collected} batch(es)!");
            }
            return collected;
        }

        /// <summary>Cancels a batch, refunding inputs. Only before collection.</summary>
        public bool CancelProduction(ProductionInstance instance)
        {
            if (instance == null) return false;
            if (!_queues.TryGetValue(instance.BuildingId, out var queue)) return false;
            if (!queue.Remove(instance)) return false;
            var inventory = ServiceLocator.Get<InventoryService>();
            foreach (var input in instance.Recipe.inputs)
                inventory.Add(input.itemId, input.count);
            GameEvents.RaiseProductionChanged();
            GameEvents.RaiseToast("Batch cancelled — ingredients returned. ↩️");
            return true;
        }

        private void PollReady()
        {
            bool changed = false;
            foreach (var kvp in _queues)
                foreach (var p in kvp.Value)
                    if (p.IsReady && !p.ReadyNotified)
                    {
                        p.ReadyNotified = true;
                        GameEvents.RaiseProductionReady(kvp.Key);
                        changed = true;
                    }
            if (changed) GameEvents.RaiseProductionChanged();
        }

        // ---------- save ----------
        public ProductionSaveData CaptureState()
        {
            var data = new ProductionSaveData();
            foreach (var kvp in _queues)
            {
                var building = new ProductionBuildingSave
                {
                    buildingId = kvp.Key,
                    queue = new List<ProductionSaveEntry>()
                };
                foreach (var p in kvp.Value) building.queue.Add(p.CaptureState());
                data.buildings.Add(building);
            }
            return data;
        }

        public void RestoreState(ProductionSaveData data)
        {
            _queues.Clear();
            if (data == null) return;
            foreach (var b in data.buildings)
            {
                var queue = new List<ProductionInstance>();
                foreach (var entry in b.queue)
                {
                    var recipe = _database.GetRecipe(entry.recipeId);
                    if (recipe == null) continue;
                    var instance = new ProductionInstance(recipe, _time);
                    instance.RestoreState(entry);
                    queue.Add(instance);
                }
                _queues[b.buildingId] = queue;
            }
        }
    }
}
