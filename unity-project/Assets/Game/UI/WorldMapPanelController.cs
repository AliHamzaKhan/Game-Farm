using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Farming;
using FarmQuest.Systems.World;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>
    /// World tour panel with three tabs:
    ///  MAP — every country and city, completed / current / locked, with completion %.
    ///  CITY — the current city's contracts with progress and Deliver buttons.
    ///  FARM — farm upgrades and land expansion.
    /// Rows use the shared row prefab (children "Name", "Price", "Button").
    /// </summary>
    public class WorldMapPanelController : MonoBehaviour
    {
        public Transform listContent;
        public GameObject rowPrefab;
        public Text headerText;
        public Button mapTabButton;
        public Button cityTabButton;
        public Button farmTabButton;
        public Button closeButton;

        private enum Tab { Map, City, Farm }
        private Tab _tab = Tab.City;

        private void Awake()
        {
            mapTabButton?.onClick.AddListener(() => Show(Tab.Map));
            cityTabButton?.onClick.AddListener(() => Show(Tab.City));
            farmTabButton?.onClick.AddListener(() => Show(Tab.Farm));
            closeButton?.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void OnEnable()
        {
            GameEvents.WorldChanged += Rebuild;
            GameEvents.FarmUpgradesChanged += Rebuild;
            GameEvents.InventoryChanged += OnInventoryChanged;
            Rebuild();
        }

        private void OnDisable()
        {
            GameEvents.WorldChanged -= Rebuild;
            GameEvents.FarmUpgradesChanged -= Rebuild;
            GameEvents.InventoryChanged -= OnInventoryChanged;
        }

        private void OnInventoryChanged(string itemId, int count)
        {
            if (_tab == Tab.City) Rebuild();
        }

        private void Show(Tab tab)
        {
            _tab = tab;
            Rebuild();
        }

        private void Rebuild()
        {
            if (!isActiveAndEnabled) return;
            ClearList();
            if (!ServiceLocator.TryGet(out WorldProgressionService world) || !world.HasWorld)
            {
                SetHeader("World data missing — run FarmQuest > Generate > All Game Data.");
                return;
            }
            switch (_tab)
            {
                case Tab.Map: BuildMap(world); break;
                case Tab.City: BuildCity(world); break;
                default: BuildFarm(); break;
            }
        }

        // ---------- MAP ----------
        private void BuildMap(WorldProgressionService world)
        {
            var current = world.Current;
            SetHeader($"World tour · Level {current.LevelNumber}: {current.CityName}, {current.CountryName}\n" +
                      $"{world.CitiesCompleted} cities and {world.CountriesCompleted} countries completed");
            int lapStart = current.GlobalIndex - current.GlobalIndex % world.AuthoredCityCount;
            int index = lapStart;
            int countryIndex = 0;
            CountryDef lastCountry = null;
            for (int i = 0; i < world.AuthoredCityCount; i++, index++)
            {
                var level = world.GetCityLevel(index);
                if (level == null) break;
                if (level.Country != lastCountry)
                {
                    lastCountry = level.Country;
                    countryIndex = level.CountryIndex;
                    int pct = Mathf.RoundToInt(world.CountryCompletion(countryIndex) * 100f);
                    bool countryLocked = world.GetStatus(index) == CityStatus.Locked;
                    AddRow($"<b>{level.CountryName.ToUpperInvariant()}</b>",
                        countryLocked ? "Locked" : $"{pct}%", null, null);
                }
                var status = world.GetStatus(index);
                string mark = status == CityStatus.Completed ? "Done" : status == CityStatus.Current ? "Now" : "Locked";
                string extra = status == CityStatus.Current
                    ? $"{Mathf.RoundToInt(world.CurrentCityCompletion * 100f)}%" : mark;
                AddRow($"   Lv {level.LevelNumber}  {level.CityName}", extra,
                    status == CityStatus.Current ? "OPEN" : null,
                    status == CityStatus.Current ? (UnityEngine.Events.UnityAction)(() => Show(Tab.City)) : null);
            }
            AddRow("More laps follow, with bigger contracts and rewards.", "", null, null);
        }

        // ---------- CITY ----------
        private void BuildCity(WorldProgressionService world)
        {
            var level = world.Current;
            var db = ServiceLocator.Get<CropDatabase>();
            SetHeader($"Level {level.LevelNumber}: {level.CityName}, {level.CountryName} " +
                      $"({Mathf.RoundToInt(world.CurrentCityCompletion * 100f)}%)\n{level.City.description}");
            var objectives = level.City.objectives;
            for (int i = 0; i < objectives.Count; i++)
            {
                var o = objectives[i];
                long have = world.ObjectiveProgress[i];
                bool done = world.IsObjectiveDone(i);
                string label = WorldProgressionService.ObjectiveLabel(o, db);
                string progress = done ? "Done" : $"{have:N0}/{o.target:N0}";
                if (o.type == CityObjectiveType.DeliverItem && !done)
                {
                    int idx = i;
                    int can = world.DeliverableCount(i);
                    AddRow(label, progress, can > 0 ? $"GIVE {can}" : "GIVE", () => world.Deliver(idx));
                }
                else AddRow(label, progress, null, null);
            }
            AddRow($"Reward: {level.City.rewardCoins:N0} coins + {level.City.rewardXp} XP", "", null, null);
            if (level.IsLastInCountry)
                AddRow($"Last city in {level.CountryName}: country bonus on completion!", "", null, null);
            var sb = new System.Text.StringBuilder("Market here pays more for: ");
            bool any = false;
            foreach (var p in level.City.prices)
            {
                if (p.sellMultiplier <= 1f) continue;
                var item = db.GetItem(p.itemId);
                sb.Append(any ? ", " : "").Append(item != null ? item.displayName : p.itemId)
                  .Append($" (+{Mathf.RoundToInt((p.sellMultiplier - 1f) * 100f)}%)");
                any = true;
            }
            if (any) AddRow(sb.ToString(), "", null, null);
            if (level.City.introducedCropIds.Count > 0 && level.Lap == 0)
            {
                var names = new System.Text.StringBuilder("New crops here: ");
                for (int i = 0; i < level.City.introducedCropIds.Count; i++)
                {
                    var crop = db.GetCrop(level.City.introducedCropIds[i]);
                    names.Append(i > 0 ? ", " : "").Append(crop != null ? crop.displayName : level.City.introducedCropIds[i]);
                }
                AddRow(names.ToString(), "", null, null);
            }
        }

        // ---------- FARM ----------
        private void BuildFarm()
        {
            SetHeader("Farm upgrades & land\nMachines and irrigation are in Equip.");
            if (ServiceLocator.TryGet(out FarmUpgradeService upgrades))
                foreach (var u in upgrades.All())
                {
                    int lvl = upgrades.GetLevel(u.upgradeId);
                    string id = u.upgradeId;
                    if (upgrades.IsMaxed(u))
                        AddRow($"{u.displayName} (max)\n{u.description}", $"Lv {lvl}", null, null);
                    else
                        AddRow($"{u.displayName} Lv {lvl}/{u.maxLevel}\n{u.description}",
                            $"{upgrades.NextCost(u):N0}", "BUY", () => upgrades.TryBuy(id));
                }

            var farm = ServiceLocator.Get<FarmService>();
            var layout = ServiceLocator.Get<FarmLayoutData>();
            foreach (var region in layout.regions)
            {
                if (farm.IsRegionUnlocked(region.regionId)) continue;
                int rid = region.regionId;
                int plots = region.area.width * region.area.height;
                AddRow($"Expand: {region.regionName} (+{plots} plots, level {region.unlockLevel})",
                    $"{region.unlockCost:N0}", "BUY", () => { farm.TryUnlockRegion(rid); Rebuild(); });
            }
        }

        // ---------- rows ----------
        private void SetHeader(string text)
        {
            if (headerText != null) headerText.text = text;
        }

        private void ClearList()
        {
            if (listContent == null) return;
            foreach (Transform child in listContent) Destroy(child.gameObject);
        }

        private void AddRow(string name, string price, string buttonLabel, UnityEngine.Events.UnityAction action)
        {
            if (rowPrefab == null || listContent == null) return;
            var row = Instantiate(rowPrefab, listContent);
            SetText(row.transform, "Name", name);
            SetText(row.transform, "Price", price);
            var buttonT = FindChild(row.transform, "Button");
            var button = buttonT != null ? buttonT.GetComponent<Button>() : null;
            if (button == null) return;
            bool show = action != null;
            button.gameObject.SetActive(show);
            if (!show) return;
            var label = button.GetComponentInChildren<Text>();
            if (label != null) label.text = buttonLabel;
            button.onClick.AddListener(action);
        }

        private static void SetText(Transform row, string child, string value)
        {
            var t = FindChild(row, child);
            var text = t != null ? t.GetComponent<Text>() : null;
            if (text == null) return;
            text.supportRichText = true;
            // Long labels shrink to fit instead of being cut off.
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 18;
            text.resizeTextMaxSize = 36;
            text.text = value;
        }

        private static Transform FindChild(Transform parent, string name)
        {
            foreach (Transform c in parent)
            {
                if (c.name == name) return c;
                var deep = FindChild(c, name);
                if (deep != null) return deep;
            }
            return null;
        }
    }
}
