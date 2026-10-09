using System;
using System.Collections.Generic;
using FarmQuest.Core.Save;
using FarmQuest.Core.Services;
using FarmQuest.Core.Time;
using FarmQuest.Data;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Inventory;

namespace FarmQuest.Systems.Animals
{
    /// <summary>
    /// LOGIC: animal ownership, care, production (§21). Housing capacity rules;
    /// barn upgrades expand Barn/Pasture. Polls on the 1-second tick for
    /// product-ready notifications — no per-animal Update().
    /// </summary>
    public class AnimalService
    {
        private readonly AnimalDatabase _database;
        private readonly ITimeService _time;
        private readonly List<AnimalInstance> _animals = new List<AnimalInstance>();
        private readonly Dictionary<AnimalHousing, int> _capacity = new Dictionary<AnimalHousing, int>();

        public IReadOnlyList<AnimalInstance> Animals => _animals;

        public AnimalService(AnimalDatabase database, ITimeService time)
        {
            _database = database;
            _time = time;
            ResetCapacity();
            _time.SecondTick += PollReady;
            GameEvents.BuildingUpgraded += OnBuildingUpgraded;
        }

        private void ResetCapacity()
        {
            _capacity[AnimalHousing.Coop] = 4;
            _capacity[AnimalHousing.Barn] = 4;
            _capacity[AnimalHousing.Pasture] = 6;
            _capacity[AnimalHousing.Pond] = 4;
            _capacity[AnimalHousing.Stable] = 2;
            _capacity[AnimalHousing.Hive] = 4;
        }

        public int CapacityFor(AnimalHousing housing) =>
            _capacity.TryGetValue(housing, out int c) ? c : 0;

        public int UsedSlots(AnimalHousing housing)
        {
            int n = 0;
            foreach (var a in _animals)
                if (a.Data.housing == housing) n++;
            return n;
        }

        public int FreeSlots(AnimalHousing housing) =>
            Math.Max(0, CapacityFor(housing) - UsedSlots(housing));

        /// <summary>Barn upgrades add +4 Barn/Pasture slots per level.</summary>
        private void OnBuildingUpgraded(string buildingId, int newLevel)
        {
            if (buildingId != "barn") return;
            ResetCapacity();
            int bonus = 4 * Math.Max(0, newLevel);
            _capacity[AnimalHousing.Barn] += bonus;
            _capacity[AnimalHousing.Pasture] += bonus;
            GameEvents.RaiseAnimalsChanged();
        }

        public bool BuyAnimal(string animalId)
        {
            var data = _database.GetAnimal(animalId);
            if (data == null) return false;

            var progression = ServiceLocator.Get<ProgressionService>();
            if (!progression.IsLevelUnlocked(data.unlockLevel))
            {
                GameEvents.RaiseToast($"Reach level {data.unlockLevel} for {data.displayName}.");
                return false;
            }
            var farm = ServiceLocator.Get<Farming.FarmService>();
            if (!farm.IsRegionUnlocked(data.requiredRegionId))
            {
                GameEvents.RaiseToast("Unlock the animal area first!");
                return false;
            }
            if (FreeSlots(data.housing) <= 0)
            {
                GameEvents.RaiseToast($"No room! Upgrade the barn or free a {data.housing} slot.");
                return false;
            }
            if (!ServiceLocator.Get<EconomyService>().TrySpend(data.buyPrice))
            {
                GameEvents.RaiseToast($"Need {data.buyPrice} coins for {data.displayName}.");
                return false;
            }

            var animal = new AnimalInstance(data, _time);
            _animals.Add(animal);
            ServiceLocator.Get<EconomyService>().AddXp(25, "buy_animal");
            GameEvents.RaiseAnimalBought(animalId);
            GameEvents.RaiseAnimalsChanged();
            GameEvents.RaiseToast($"🐾 Welcome, {data.displayName}!");
            return true;
        }

        public void FeedAnimal(AnimalInstance animal)
        {
            if (animal == null) return;
            var inventory = ServiceLocator.Get<InventoryService>();
            if (!inventory.TryRemove("feed", 1))
            {
                GameEvents.RaiseToast("No feed! Buy some at the market.");
                return;
            }
            animal.Feed();
            ServiceLocator.Get<EconomyService>().AddXp(5, "feed_animal");
            GameEvents.RaiseAnimalsChanged();
        }

        public void PetAnimal(AnimalInstance animal)
        {
            if (animal == null) return;
            animal.Pet();
            ServiceLocator.Get<EconomyService>().AddXp(3, "pet_animal");
            GameEvents.RaiseAnimalsChanged();
            GameEvents.RaiseToast("💕 So happy!");
        }

        /// <summary>Collects products into inventory. Returns count collected.</summary>
        public int CollectProducts(AnimalInstance animal)
        {
            if (animal == null) return 0;
            int n = animal.Collect();
            if (n <= 0) return 0;
            ServiceLocator.Get<InventoryService>().Add(animal.Data.productItemId, n);
            ServiceLocator.Get<EconomyService>().AddXp(animal.Data.xpPerCollect, "collect_product");
            GameEvents.RaiseAnimalsChanged();
            GameEvents.RaiseToast($"Got {n} {animal.Data.productItemId}!");
            return n;
        }

        private void PollReady()
        {
            bool changed = false;
            foreach (var animal in _animals)
            {
                if (animal.ProductsReady > 0 && !animal.ReadyNotified)
                {
                    animal.ReadyNotified = true;
                    GameEvents.RaiseAnimalProductReady(animal.InstanceId);
                    changed = true;
                }
            }
            if (changed) GameEvents.RaiseAnimalsChanged();
        }

        // ---------- save ----------
        public AnimalSaveData CaptureState()
        {
            var data = new AnimalSaveData();
            foreach (var a in _animals)
                data.animals.Add(a.CaptureState());
            return data;
        }

        public void RestoreState(AnimalSaveData data)
        {
            _animals.Clear();
            if (data == null) return;
            foreach (var entry in data.animals)
            {
                var animalData = _database.GetAnimal(entry.animalId);
                if (animalData == null) continue;
                var animal = new AnimalInstance(animalData, _time);
                animal.RestoreState(entry);
                _animals.Add(animal);
            }
        }
    }
}
