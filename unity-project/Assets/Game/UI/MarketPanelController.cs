using System.Collections.Generic;
using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>
    /// Village market (§19): BUY / SELL / ORDERS tabs. Child-friendly and simple.
    /// Rows are built from a prefab with children named "Name", "Price", "Button".
    /// </summary>
    public class MarketPanelController : MonoBehaviour
    {
        [Header("Wiring")]
        public Transform listContent;
        public GameObject rowPrefab;
        public Button buyTabButton;
        public Button sellTabButton;
        public Button ordersTabButton; // Phase 5: orders (stub for now)
        public Button closeButton;

        private MarketService _market;
        private CropDatabase _database;
        private ProgressionService _progression;

        private void Awake()
        {
            buyTabButton?.onClick.AddListener(ShowBuy);
            sellTabButton?.onClick.AddListener(ShowSell);
            ordersTabButton?.onClick.AddListener(() =>
                GameEvents.RaiseToast("Orders unlock at level 20!"));
            closeButton?.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void OnEnable()
        {
            _market = ServiceLocator.Get<MarketService>();
            _database = ServiceLocator.Get<CropDatabase>();
            _progression = ServiceLocator.Get<ProgressionService>();
            ShowBuy();
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
            SetText(row, "Name", name);
            SetText(row, "Price", price);
            var button = FindChild(row.transform, "Button")?.GetComponent<Button>();
            var label = button?.GetComponentInChildren<Text>();
            if (label != null) label.text = buttonLabel;
            button?.onClick.AddListener(() => { action?.Invoke(); RefreshCurrent(); });
        }

        private static void SetText(GameObject row, string child, string value)
        {
            var t = FindChild(row.transform, child)?.GetComponent<Text>();
            if (t != null) t.text = value;
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

        private string _currentTab = "buy";
        private void RefreshCurrent()
        {
            if (_currentTab == "buy") ShowBuy(); else ShowSell();
        }

        public void ShowBuy()
        {
            _currentTab = "buy";
            ClearList();
            foreach (var crop in _database.crops)
            {
                if (!_progression.IsLevelUnlocked(crop.unlockLevel)) continue;
                string c = crop.cropId; // capture
                AddRow($"{crop.displayName} seed", $"🪙 {crop.seedPrice}", "BUY",
                    () => _market.BuySeed(c, 1));
            }
            // Supplies
            AddRow("Compost", "🪙 8", "BUY", () => _market.BuyItem("compost", 1));
        }

        public void ShowSell()
        {
            _currentTab = "sell";
            ClearList();
            var inventory = ServiceLocator.Get<Systems.Inventory.InventoryService>();
            var seen = new HashSet<string>();
            foreach (var slot in inventory.Slots)
            {
                if (!seen.Add(slot.ItemId)) continue;
                var item = _database.GetItem(slot.ItemId);
                // Phase 3: sell crops AND animal products (anything with a sell price).
                if (item == null || item.sellPrice <= 0) continue;
                string id = slot.ItemId;
                int count = inventory.GetCount(id);
                AddRow($"{item.displayName} ×{count}", $"🪙 {item.sellPrice} ea", "SELL",
                    () =>
                    {
                        int earned = _market.Sell(id, count);
                        GameEvents.RaiseToast($"+{earned} coins!");
                    });
            }
            if (seen.Count == 0)
                GameEvents.RaiseToast("Harvest something first! 🌱");
        }
    }
}
