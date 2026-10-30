using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Decor;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Machines;
using FarmQuest.Systems.Store;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>
    /// FarmQuest Store, three tabs:
    ///  OFFERS — real-money bundles. Every purchase asks a grown-up first (parent gate).
    ///  GEMS   — premium seeds, advanced/premium supplies, gem-model machines.
    ///  DECOR  — cosmetics and country packs (coins or gems). Purely visual.
    /// Everything needed to play is in the Market for Farm Coins.
    /// </summary>
    public class StorePanelController : MonoBehaviour
    {
        public Transform listContent;
        public GameObject rowPrefab;
        public Text headerText;
        public Button offersTabButton;
        public Button gemsTabButton;
        public Button decorTabButton;
        public Button closeButton;
        public ParentGateController parentGate;

        private enum Tab { Offers, Gems, Decor }
        private Tab _tab = Tab.Gems;

        private void Awake()
        {
            offersTabButton?.onClick.AddListener(() => Show(Tab.Offers));
            gemsTabButton?.onClick.AddListener(() => Show(Tab.Gems));
            decorTabButton?.onClick.AddListener(() => Show(Tab.Decor));
            closeButton?.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void OnEnable()
        {
            GameEvents.StoreChanged += Rebuild;
            GameEvents.GemsChanged += OnWallet;
            GameEvents.CoinsChanged += OnWallet;
            Rebuild();
        }

        private void OnDisable()
        {
            GameEvents.StoreChanged -= Rebuild;
            GameEvents.GemsChanged -= OnWallet;
            GameEvents.CoinsChanged -= OnWallet;
        }

        private void OnWallet(long _) => Rebuild();

        private void Show(Tab tab) { _tab = tab; Rebuild(); }

        private void Rebuild()
        {
            if (!isActiveAndEnabled) return;
            RowList.Clear(listContent);
            var economy = ServiceLocator.Get<EconomyService>();
            if (headerText != null)
                headerText.text = $"{economy.Coins:N0} Farm Coins   ·   {economy.Gems:N0} Farm Gems\n" +
                                  "Everything you need to farm is free in the Market.";
            switch (_tab)
            {
                case Tab.Offers: BuildOffers(); break;
                case Tab.Gems: BuildGems(); break;
                default: BuildDecor(); break;
            }
        }

        private void Row(string name, string price, string button = null, UnityEngine.Events.UnityAction action = null) =>
            RowList.Add(listContent, rowPrefab, name, price, button, action);

        // ---------- real-money offers ----------
        private void BuildOffers()
        {
            if (!ServiceLocator.TryGet(out StoreService store) || store.Catalog == null)
            {
                Row("Store data missing — run Generate All Game Data.", "");
                return;
            }
            if (!store.PurchasesAllowed) { Row("Purchases are turned off in the parent dashboard.", ""); return; }
            if (!store.IsStoreAvailable) { Row("The store isn't available right now. You can keep farming!", ""); return; }
            Row("A grown-up must approve every purchase.", "");
            var db = ServiceLocator.Get<CropDatabase>();
            foreach (var bundle in store.VisibleBundles())
            {
                string id = bundle.productId;
                string title = bundle.title;
                Row($"<b>{bundle.title}</b> — {bundle.description}\n{Rewards.Describe(bundle.contents, db)}",
                    store.PriceLabel(bundle), "BUY", () => AskGrownUp(() => store.BuyBundle(id, ok =>
                    {
                        if (!ok) GameEvents.RaiseToast($"{title}: purchase not completed. Nothing was charged by FarmQuest.");
                    })));
            }
        }

        private void AskGrownUp(System.Action action)
        {
            if (parentGate != null) parentGate.Request(action);
            else GameEvents.RaiseToast("Ask a grown-up: the parent gate is missing from this scene.");
        }

        // ---------- gem shop ----------
        private void BuildGems()
        {
            ServiceLocator.TryGet(out StoreService store);
            var db = ServiceLocator.Get<CropDatabase>();
            var inventory = ServiceLocator.Get<Systems.Inventory.InventoryService>();
            Row("<b>Premium seeds</b> (3 per pack, ~4x selling value, never needed for cities)", "");
            foreach (var crop in db.crops)
            {
                if (crop == null || !crop.isPremium) continue;
                var seed = db.GetItem("seed_" + crop.cropId);
                string id = crop.cropId;
                if (!StoreService.IsItemAvailable(seed))
                {
                    Row($"   {crop.displayName}", $"Lv {(seed != null ? seed.unlockCityIndex + 1 : 1)}");
                    continue;
                }
                Row($"   {crop.displayName} (have {inventory.GetCount("seed_" + id)})", $"{crop.gemSeedPrice} gems", "BUY",
                    () => store?.BuyPremiumSeeds(id));
            }

            Row("<b>Supplies</b> (Basic ones are in the Market for coins)", "");
            var market = ServiceLocator.Get<MarketService>();
            foreach (var item in db.items)
            {
                if (item == null || item.tier == ItemTier.Basic || item.category == ItemCategory.Seed ||
                    item.category == ItemCategory.Crop || item.effect == ConsumableEffect.None) continue;
                string id = item.itemId;
                string have = $"(have {inventory.GetCount(id)})";
                if (!StoreService.IsItemAvailable(item)) { Row($"   {item.displayName}", $"Lv {item.unlockCityIndex + 1}"); continue; }
                if (item.buyPrice > 0)
                    Row($"   {item.displayName} {have}\n   {item.benefit}", $"{item.buyPrice} coins", "BUY", () => market.BuyItem(id, 1));
                if (item.gemPrice > 0)
                    Row($"   {item.displayName} {have}" + (item.buyPrice > 0 ? "" : $"\n   {item.benefit}"),
                        $"{item.gemPrice} gems", "BUY", () => store?.BuyItemWithGems(id));
            }

            if (ServiceLocator.TryGet(out EquipmentService equipment))
            {
                Row("<b>Machines</b> (also sold for coins in Equip)", "");
                foreach (var e in equipment.AllEquipment())
                {
                    if (e == null || e.gemPrice <= 0) continue;
                    string id = e.equipmentId;
                    if (equipment.Has(id)) Row($"   {e.displayName}", "Owned");
                    else Row($"   {e.displayName} (level {e.unlockLevel})\n   {e.description}", $"{e.gemPrice} gems", "BUY",
                        () => { equipment.BuyEquipment(id, true); Rebuild(); });
                }
            }
        }

        // ---------- cosmetics ----------
        private void BuildDecor()
        {
            if (!ServiceLocator.TryGet(out DecorService decor)) return;
            Row("Decorations are just for fun — no gameplay advantage. Place them from Decor.", "");
            foreach (var d in decor.AllDecor())
            {
                if (d == null) continue;
                string id = d.decorId;
                string owned = decor.OwnedCount(id) > 0 ? $" (have {decor.OwnedCount(id)})" : "";
                if (!StoreService.IsCountryReached(d.requiredCountryId))
                {
                    Row($"{d.displayName} — {d.category}", "Reach country");
                    continue;
                }
                if (d.cost > 0) Row($"{d.displayName}{owned}", $"{d.cost} coins", "BUY", () => { decor.BuyDecor(id); Rebuild(); });
                if (d.gemPrice > 0) Row($"{d.displayName}{owned}", $"{d.gemPrice} gems", "BUY", () => { decor.BuyDecorWithGems(id); Rebuild(); });
            }
        }
    }
}
