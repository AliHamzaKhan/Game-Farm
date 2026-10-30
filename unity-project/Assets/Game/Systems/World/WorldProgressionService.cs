using System;
using System.Collections.Generic;
using FarmQuest.Core.Save;
using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Farming;
using FarmQuest.Systems.Inventory;
using UnityEngine;

namespace FarmQuest.Systems.World
{
    public enum CityStatus { Completed, Current, Locked }

    /// <summary>A city resolved for play: authored data, or a scaled copy on later laps.</summary>
    public class CityLevel
    {
        public int GlobalIndex;   // 0-based level across the whole tour
        public int Lap;           // 0 = first time through the authored world
        public int CountryIndex;  // index into WorldData.countries
        public int IndexInCountry;
        public CountryDef Country;
        public CityDef City;      // objectives/rewards already scaled for the lap
        public string CityName => Lap == 0 ? City.displayName : $"{City.displayName} ★{Lap + 1}";
        public string CountryName => Lap == 0 ? Country.displayName : $"{Country.displayName} ★{Lap + 1}";
        public int LevelNumber => GlobalIndex + 1;
        public bool IsLastInCountry => IndexInCountry == Country.cities.Count - 1;
    }

    /// <summary>
    /// LOGIC: the global farming tour (countries → cities → objectives).
    /// Starts in the first city of the first country, tracks objective progress
    /// from game events, completes the city when every objective is met, then
    /// unlocks the next city (and the next country after a country's last city).
    /// All content comes from WorldData; no city is hardcoded.
    /// </summary>
    public class WorldProgressionService
    {
        public const int NewCropStarterSeeds = 3;

        private readonly WorldData _world;
        private readonly List<long> _progress = new List<long>();
        private CityLevel _current;
        private bool _completing;

        public int CurrentCityIndex { get; private set; }
        public int CitiesCompleted { get; private set; }
        public int CountriesCompleted { get; private set; }

        public WorldProgressionService(WorldData world)
        {
            _world = world;
            SetCity(0);
            GameEvents.CropHarvested += OnCropHarvested;
            GameEvents.SaleCompleted += OnSaleCompleted;
        }

        public bool HasWorld => _world != null && _world.AuthoredCityCount > 0;
        public CityLevel Current => _current;
        public IReadOnlyList<long> ObjectiveProgress => _progress;
        public int AuthoredCityCount => _world != null ? _world.AuthoredCityCount : 0;

        // ---------- resolving cities ----------

        /// <summary>Resolves any level of the tour, including endless laps.</summary>
        public CityLevel GetCityLevel(int globalIndex)
        {
            if (!HasWorld || globalIndex < 0) return null;
            int n = _world.AuthoredCityCount;
            int lap = globalIndex / n;
            int local = globalIndex % n;
            for (int ci = 0; ci < _world.countries.Count; ci++)
            {
                var country = _world.countries[ci];
                if (country?.cities == null) continue;
                if (local < country.cities.Count)
                {
                    return new CityLevel
                    {
                        GlobalIndex = globalIndex,
                        Lap = lap,
                        CountryIndex = ci,
                        IndexInCountry = local,
                        Country = country,
                        City = lap == 0 ? country.cities[local] : ScaleForLap(country.cities[local], lap)
                    };
                }
                local -= country.cities.Count;
            }
            return null;
        }

        private CityDef ScaleForLap(CityDef src, int lap)
        {
            float t = 1f + _world.lapTargetGrowth * lap;
            float r = 1f + _world.lapRewardGrowth * lap;
            var city = new CityDef
            {
                cityId = $"{src.cityId}_lap{lap}",
                displayName = src.displayName,
                description = src.description,
                introducedCropIds = src.introducedCropIds,
                prices = src.prices,
                rewardCoins = (long)Math.Round(src.rewardCoins * r),
                rewardXp = Mathf.RoundToInt(src.rewardXp * r),
                rewardItems = src.rewardItems
            };
            foreach (var o in src.objectives)
                city.objectives.Add(new CityObjective
                {
                    type = o.type,
                    itemId = o.itemId,
                    label = o.label,
                    target = Math.Max(1, (long)Math.Round(o.target * t))
                });
            return city;
        }

        public CityStatus GetStatus(int globalIndex)
        {
            if (globalIndex < CurrentCityIndex) return CityStatus.Completed;
            return globalIndex == CurrentCityIndex ? CityStatus.Current : CityStatus.Locked;
        }

        private void SetCity(int index)
        {
            CurrentCityIndex = Math.Max(0, index);
            _current = GetCityLevel(CurrentCityIndex);
            _progress.Clear();
            if (_current != null)
                foreach (var _ in _current.City.objectives) _progress.Add(0);
        }

        // ---------- objectives ----------

        public long GetTarget(int objectiveIndex) =>
            _current != null && objectiveIndex >= 0 && objectiveIndex < _current.City.objectives.Count
                ? _current.City.objectives[objectiveIndex].target : 0;

        public bool IsObjectiveDone(int i) => i >= 0 && i < _progress.Count && _progress[i] >= GetTarget(i);

        public bool IsCityComplete
        {
            get
            {
                if (_current == null || _progress.Count == 0) return false;
                for (int i = 0; i < _progress.Count; i++) if (!IsObjectiveDone(i)) return false;
                return true;
            }
        }

        /// <summary>0-1 completion of the current city (average over objectives).</summary>
        public float CurrentCityCompletion
        {
            get
            {
                if (_current == null || _progress.Count == 0) return 0f;
                float sum = 0f;
                for (int i = 0; i < _progress.Count; i++)
                    sum += Mathf.Clamp01(_progress[i] / (float)Math.Max(1, GetTarget(i)));
                return sum / _progress.Count;
            }
        }

        /// <summary>0-1 completion of a country on the current lap (current city counts partially).</summary>
        public float CountryCompletion(int countryIndex)
        {
            if (!HasWorld || countryIndex < 0 || countryIndex >= _world.countries.Count) return 0f;
            int lapStart = (CurrentCityIndex / _world.AuthoredCityCount) * _world.AuthoredCityCount;
            int first = lapStart;
            for (int i = 0; i < countryIndex; i++) first += _world.countries[i].cities.Count;
            int count = _world.countries[countryIndex].cities.Count;
            if (count == 0) return 1f;
            float done = 0f;
            for (int i = 0; i < count; i++)
            {
                var status = GetStatus(first + i);
                if (status == CityStatus.Completed) done += 1f;
                else if (status == CityStatus.Current) done += CurrentCityCompletion;
            }
            return done / count;
        }

        public static string ObjectiveLabel(CityObjective o, CropDatabase db)
        {
            if (!string.IsNullOrEmpty(o.label)) return o.label;
            string item = ItemName(o.itemId, db);
            switch (o.type)
            {
                case CityObjectiveType.HarvestCrop: return $"Harvest {o.target} {item}";
                case CityObjectiveType.DeliverItem: return $"Deliver {o.target} {item}";
                case CityObjectiveType.EarnFromSales: return $"Earn {o.target:N0} coins from sales";
                case CityObjectiveType.SellItem: return $"Sell {o.target} {item}";
                default: return o.type.ToString();
            }
        }

        private static string ItemName(string id, CropDatabase db)
        {
            if (string.IsNullOrEmpty(id) || db == null) return id;
            var crop = db.GetCrop(id);
            if (crop != null) return crop.displayName;
            var item = db.GetItem(id);
            return item != null ? item.displayName : id;
        }

        private void OnCropHarvested(CropInstance crop, int qty)
        {
            if (crop?.Data == null) return;
            AddProgress(CityObjectiveType.HarvestCrop, crop.Data.cropId, qty);
        }

        private void OnSaleCompleted(string itemId, int qty, long coins)
        {
            AddProgress(CityObjectiveType.EarnFromSales, null, coins);
            AddProgress(CityObjectiveType.SellItem, itemId, qty);
        }

        private void AddProgress(CityObjectiveType type, string itemId, long amount)
        {
            if (_current == null || amount <= 0) return;
            bool changed = false;
            var objectives = _current.City.objectives;
            for (int i = 0; i < objectives.Count; i++)
            {
                var o = objectives[i];
                if (o.type != type) continue;
                if (itemId != null && o.itemId != itemId) continue;
                if (_progress[i] >= o.target) continue;
                _progress[i] = Math.Min(o.target, _progress[i] + amount);
                changed = true;
            }
            if (!changed) return;
            GameEvents.RaiseWorldChanged();
            TryCompleteCity();
        }

        /// <summary>How many more items a DeliverItem objective accepts right now.</summary>
        public int DeliverableCount(int objectiveIndex)
        {
            if (_current == null || objectiveIndex < 0 || objectiveIndex >= _progress.Count) return 0;
            var o = _current.City.objectives[objectiveIndex];
            if (o.type != CityObjectiveType.DeliverItem) return 0;
            long remaining = o.target - _progress[objectiveIndex];
            if (remaining <= 0) return 0;
            int have = ServiceLocator.Get<InventoryService>().GetCount(o.itemId);
            return (int)Math.Min(have, remaining);
        }

        /// <summary>Delivers as many items as the contract still needs (and the player has).</summary>
        public int Deliver(int objectiveIndex)
        {
            int n = DeliverableCount(objectiveIndex);
            if (n <= 0)
            {
                if (_current != null && objectiveIndex >= 0 && objectiveIndex < _progress.Count &&
                    !IsObjectiveDone(objectiveIndex))
                    GameEvents.RaiseToast("Grow some first — nothing to deliver yet!");
                return 0;
            }
            var o = _current.City.objectives[objectiveIndex];
            if (!ServiceLocator.Get<InventoryService>().TryRemove(o.itemId, n)) return 0;
            _progress[objectiveIndex] += n;
            ServiceLocator.Get<EconomyService>().AddXp(n, "city_delivery");
            GameEvents.RaiseToast($"Delivered {n}!");
            GameEvents.RaiseWorldChanged();
            TryCompleteCity();
            return n;
        }

        // ---------- completion ----------

        private void TryCompleteCity()
        {
            if (_completing || !IsCityComplete) return;
            _completing = true;
            try
            {
                var done = _current;
                var economy = ServiceLocator.Get<EconomyService>();
                economy.AddCoins(done.City.rewardCoins, "city_complete");
                economy.AddXp(done.City.rewardXp, "city_complete");
                if (done.City.rewardItems != null && ServiceLocator.TryGet(out InventoryService inventory))
                    foreach (var r in done.City.rewardItems)
                        if (!string.IsNullOrEmpty(r.itemId) && r.count > 0) inventory.Add(r.itemId, r.count);
                CitiesCompleted++;
                GameEvents.RaiseCityCompleted(done.GlobalIndex, done.CityName);

                if (done.IsLastInCountry)
                {
                    long bonus = (long)Math.Round(done.Country.completionBonusCoins * (1f + _world.lapRewardGrowth * done.Lap));
                    economy.AddCoins(bonus, "country_complete");
                    economy.AddXp(done.Country.completionBonusXp, "country_complete");
                    CountriesCompleted++;
                    GameEvents.RaiseCountryCompleted(done.CountryName);
                }

                SetCity(done.GlobalIndex + 1);
                GrantStarterSeedsForNewCrops();
                GameEvents.RaiseWorldChanged();
                // Save straight away so a crash can't replay (or lose) the reward.
                if (ServiceLocator.TryGet(out SaveCoordinator coordinator)) coordinator.SaveAll();
            }
            finally { _completing = false; }
        }

        private void GrantStarterSeedsForNewCrops()
        {
            if (_current == null || _current.Lap > 0) return;
            if (!ServiceLocator.TryGet(out InventoryService inventory)) return;
            foreach (var cropId in _current.City.introducedCropIds)
                if (!string.IsNullOrEmpty(cropId)) inventory.Add("seed_" + cropId, NewCropStarterSeeds);
        }

        // ---------- crop availability & market ----------

        /// <summary>True when a crop was introduced by the current city or an earlier one.</summary>
        public bool IsCropIntroduced(string cropId)
        {
            if (!HasWorld) return false;
            int last = Math.Min(CurrentCityIndex, _world.AuthoredCityCount - 1);
            for (int i = 0; i <= last; i++)
            {
                var level = GetCityLevel(i);
                if (level != null && level.City.introducedCropIds.Contains(cropId)) return true;
            }
            return false;
        }

        /// <summary>Seed availability: player level OR the world tour introduced it.</summary>
        public static bool IsCropAvailable(CropData crop)
        {
            if (crop == null) return false;
            if (ServiceLocator.TryGet(out ProgressionService progression) &&
                progression.IsLevelUnlocked(crop.unlockLevel)) return true;
            return ServiceLocator.TryGet(out WorldProgressionService world) && world.IsCropIntroduced(crop.cropId);
        }

        /// <summary>Current city's market multiplier for an item (1 when it has no special price).</summary>
        public float GetSellMultiplier(string itemId)
        {
            if (_current?.City.prices == null) return 1f;
            foreach (var p in _current.City.prices)
                if (p.itemId == itemId) return Mathf.Max(0.1f, p.sellMultiplier);
            return 1f;
        }

        /// <summary>Items this city wants (objectives + premium prices), for orders and UI hints.</summary>
        public List<string> DemandedItemIds()
        {
            var list = new List<string>();
            if (_current == null) return list;
            foreach (var o in _current.City.objectives)
            {
                string id = o.type == CityObjectiveType.HarvestCrop ? "crop_" + o.itemId : o.itemId;
                if (!string.IsNullOrEmpty(id) && !list.Contains(id)) list.Add(id);
            }
            foreach (var p in _current.City.prices)
                if (p.sellMultiplier > 1f && !list.Contains(p.itemId)) list.Add(p.itemId);
            return list;
        }

        // ---------- save ----------

        public WorldSaveData CaptureState() => new WorldSaveData
        {
            currentCityIndex = CurrentCityIndex,
            currentCityId = _current?.City.cityId ?? "",
            objectiveProgress = new List<long>(_progress),
            citiesCompleted = CitiesCompleted,
            countriesCompleted = CountriesCompleted
        };

        public void RestoreState(WorldSaveData data)
        {
            if (data == null) { SetCity(0); return; }
            SetCity(data.currentCityIndex);
            CitiesCompleted = Math.Max(0, data.citiesCompleted);
            CountriesCompleted = Math.Max(0, data.countriesCompleted);
            // Only reuse progress if the saved city is still the same city (world data may be edited).
            bool sameCity = _current != null && (string.IsNullOrEmpty(data.currentCityId) ||
                                                  data.currentCityId == _current.City.cityId);
            if (sameCity && data.objectiveProgress != null)
                for (int i = 0; i < _progress.Count && i < data.objectiveProgress.Count; i++)
                    _progress[i] = Math.Max(0, Math.Min(GetTarget(i), data.objectiveProgress[i]));
            GameEvents.RaiseWorldChanged();
        }
    }
}
