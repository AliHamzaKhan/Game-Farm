using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Village;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>Village panel (§20): NPC shops (with open hours) + bank access.</summary>
    public class VillagePanelController : MonoBehaviour
    {
        public Transform listContent;
        public GameObject rowPrefab; // children: "Name", "Price", "Button"
        public Button closeButton;
        public Button bankButton;

        private VillageService _village;
        private ShopData _openShop;

        private void Awake()
        {
            closeButton?.onClick.AddListener(() => gameObject.SetActive(false));
            bankButton?.onClick.AddListener(() =>
            {
                var bank = FindObjectOfType<BankPanelController>();
                if (bank != null) bank.gameObject.SetActive(true);
            });
        }

        private void OnEnable()
        {
            _village = ServiceLocator.Get<VillageService>();
            _openShop = null;
            Rebuild();
        }

        private void Rebuild()
        {
            if (listContent == null || rowPrefab == null) return;
            foreach (Transform child in listContent) Destroy(child.gameObject);

            if (_openShop == null)
            {
                foreach (var shop in _village.Shops)
                {
                    var s = shop;
                    bool open = s.IsOpenNow();
                    AddRow($"🏪 {s.displayName}", open
                            ? $"Run by {s.shopkeeperName} • Open now!"
                            : $"Run by {s.shopkeeperName} • Opens {s.openHour}:00–{s.closeHour}:00",
                        "VISIT", () => { _openShop = s; Rebuild(); }, open);
                }
            }
            else
            {
                AddRow($"← Back to village", _openShop.displayName, "", null, false, true);
                var market = ServiceLocator.Get<MarketService>();
                var cropDb = ServiceLocator.Get<CropDatabase>();
                var animalDb = ServiceLocator.Get<Systems.Animals.AnimalDatabase>();
                var decorService = ServiceLocator.Get<Systems.Decor.DecorService>();
                foreach (var entry in _openShop.items)
                {
                    var item = cropDb.GetItem(entry.itemId);
                    if (item != null)
                    {
                        int price = entry.priceOverride >= 0 ? entry.priceOverride : item.buyPrice;
                        if (price <= 0) continue;
                        string id = entry.itemId;
                        AddRow($"🛒 {item.displayName}", $"🪙{price}",
                            "BUY", () => market.BuyItem(id, 1), true, false);
                    }
                    else
                    {
                        // Decorations live in the animal database, not the item DB.
                        var decor = animalDb.GetDecor(entry.itemId);
                        if (decor == null) continue;
                        string decorId = entry.itemId;
                        AddRow($"🌷 {decor.displayName}", $"🪙{decor.cost}",
                            "BUY", () => decorService.BuyDecor(decorId), true, false);
                    }
                }
            }
        }

        private void AddRow(string name, string info, string buttonLabel,
            UnityEngine.Events.UnityAction action, bool enabled, bool isBack = false)
        {
            var row = Instantiate(rowPrefab, listContent);
            var nameT = row.transform.Find("Name")?.GetComponent<Text>();
            if (nameT != null) nameT.text = name;
            var priceT = row.transform.Find("Price")?.GetComponent<Text>();
            if (priceT != null) priceT.text = info;
            var button = row.transform.Find("Button")?.GetComponent<Button>()
                         ?? row.GetComponentInChildren<Button>();
            if (button == null) return;
            if (isBack)
            {
                var label = button.GetComponentInChildren<Text>();
                if (label != null) label.text = "BACK";
                button.onClick.AddListener(() => { _openShop = null; Rebuild(); });
                return;
            }
            var lbl = button.GetComponentInChildren<Text>();
            if (lbl != null) lbl.text = buttonLabel;
            button.interactable = enabled;
            if (enabled && action != null) button.onClick.AddListener(action);
        }
    }
}
