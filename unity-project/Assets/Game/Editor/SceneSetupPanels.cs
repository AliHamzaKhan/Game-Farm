using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using FarmQuest.UI;

namespace FarmQuest.Editor
{
    /// <summary>
    /// Builds every UI panel + bottom nav for the test scene (Phase 6 audit fix).
    /// Called by SceneSetup after the core scene exists. All panels start hidden;
    /// controllers null-guard and rebuild on enable. Row templates are saved as
    /// real prefab assets so Instantiate() clones work correctly.
    /// </summary>
    public static class SceneSetupPanels
    {
        private static Font _font;
        private static Font Font =>
            _font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        private static readonly Color CardColor = new Color(0.13f, 0.13f, 0.17f, 0.97f);
        private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color Accent = new Color(0.95f, 0.45f, 0.35f);
        private static readonly Color AccentDark = new Color(0.75f, 0.32f, 0.25f);

        public static void BuildAll(GameObject canvasGo, HudController hud)
        {
            var canvas = canvasGo.transform;
            EnsureFolder("Assets/Game/UI/Generated");
            var rowPrefab = CreateRowPrefabAsset();
            var seedButtonPrefab = CreateSeedButtonAsset();

            // ---- list panels ----
            var market = CreateListPanel<MarketPanelController>(canvas, "MarketPanel", "🏪 Market", rowPrefab, out var marketCtrl);
            marketCtrl.buyTabButton = AddButton(market.transform, "BuyTab", "Buy", new Vector2(260, 80), new Vector2(-280, -580));
            marketCtrl.sellTabButton = AddButton(market.transform, "SellTab", "Sell", new Vector2(260, 80), new Vector2(0, -580));
            marketCtrl.ordersTabButton = AddButton(market.transform, "OrdersTab", "Orders", new Vector2(260, 80), new Vector2(280, -580));
            hud.marketPanel = market;

            var inventory = CreateListPanel<InventoryPanelController>(canvas, "InventoryPanel", "🎒 Inventory", rowPrefab, out var invCtrl);
            invCtrl.sortButton = AddButton(inventory.transform, "SortButton", "Sort", new Vector2(260, 80), new Vector2(0, -580));
            hud.inventoryPanel = inventory;

            var seedShop = CreatePanel(canvas, "SeedShopPanel", "🌱 Seed Shop", out _);
            var seedCtrl = seedShop.AddComponent<SeedShopPanelController>();
            seedCtrl.listContent = CreateScrollContent(seedShop.transform);
            seedCtrl.seedButtonPrefab = seedButtonPrefab;
            seedCtrl.closeButton = AddCloseButton(seedShop.transform);
            hud.seedShopPanel = seedShop;

            var missions = CreateListPanel<MissionPanelController>(canvas, "MissionPanel", "📋 Missions", rowPrefab, out _);
            hud.missionPanel = missions;

            var orders = CreateListPanel<OrderBoardPanelController>(canvas, "OrderPanel", "📦 Orders", rowPrefab, out _);
            hud.orderPanel = orders;

            var animals = CreateListPanel<AnimalPanelController>(canvas, "AnimalPanel", "🐄 Barn", rowPrefab, out var animalCtrl);
            BuildAnimalDetail(animals.transform, animalCtrl);
            hud.animalPanel = animals;

            var production = CreateListPanel<ProductionPanelController>(canvas, "ProductionPanel", "🏭 Factory", rowPrefab, out _);
            hud.productionPanel = production;

            var village = CreateListPanel<VillagePanelController>(canvas, "VillagePanel", "🏘️ Village", rowPrefab, out var villageCtrl);
            villageCtrl.bankButton = AddButton(village.transform, "BankButton", "🏦 Bank", new Vector2(300, 80), new Vector2(0, -580));
            var bank = CreatePanel(canvas, "BankPanel", "🏦 Bank", out _);
            var bankCtrl = bank.AddComponent<BankPanelController>();
            bankCtrl.balanceText = AddText(bank.transform, "Balance", "", 40, new Vector2(0, -180));
            bankCtrl.deposit100Button = AddButton(bank.transform, "Dep100", "+100", new Vector2(260, 80), new Vector2(-280, -300));
            bankCtrl.deposit500Button = AddButton(bank.transform, "Dep500", "+500", new Vector2(260, 80), new Vector2(0, -300));
            bankCtrl.depositAllButton = AddButton(bank.transform, "DepAll", "All In", new Vector2(260, 80), new Vector2(280, -300));
            bankCtrl.withdraw100Button = AddButton(bank.transform, "Wd100", "-100", new Vector2(260, 80), new Vector2(-140, -400));
            bankCtrl.withdrawAllButton = AddButton(bank.transform, "WdAll", "All Out", new Vector2(260, 80), new Vector2(140, -400));
            bankCtrl.closeButton = AddCloseButton(bank.transform);
            hud.villagePanel = village;

            var equipment = CreateListPanel<EquipmentPanelController>(canvas, "EquipmentPanel", "🚜 Equipment", rowPrefab, out _);
            hud.equipmentPanel = equipment;

            var orchard = CreateListPanel<OrchardPanelController>(canvas, "OrchardPanel", "🌳 Orchard", rowPrefab, out var orchardCtrl);
            orchardCtrl.statusText = AddText(orchard.transform, "Status", "", 34, new Vector2(0, -580));
            hud.orchardPanel = orchard;

            var decor = CreateListPanel<DecorPanelController>(canvas, "DecorPanel", "✨ Decor", rowPrefab, out _);
            hud.decorPanel = decor;

            var house = CreatePanel(canvas, "HousePanel", "🏠 House", out _);
            var houseCtrl = house.AddComponent<HousePanelController>();
            houseCtrl.titleText = AddText(house.transform, "Title", "Farmhouse", 44, new Vector2(0, -170));
            houseCtrl.infoText = AddText(house.transform, "Info", "", 36, new Vector2(0, -320));
            houseCtrl.upgradeButton = AddButton(house.transform, "Upgrade", "Upgrade ⬆️", new Vector2(400, 90), new Vector2(0, -480));
            houseCtrl.closeButton = AddCloseButton(house.transform);
            hud.housePanel = house;

            // ---- parent gate + dashboard ----
            var gate = CreatePanel(canvas, "ParentGatePanel", "👪 Grown-ups", out _);
            var gateCtrl = gate.AddComponent<ParentGateController>();
            gateCtrl.questionText = AddText(gate.transform, "Question", "", 44, new Vector2(0, -220));
            gateCtrl.answerButtons = new[]
            {
                AddButton(gate.transform, "Ans0", "", new Vector2(300, 90), new Vector2(0, -380)),
                AddButton(gate.transform, "Ans1", "", new Vector2(300, 90), new Vector2(0, -490)),
                AddButton(gate.transform, "Ans2", "", new Vector2(300, 90), new Vector2(0, -600)),
            };
            gateCtrl.cancelButton = AddButton(gate.transform, "Cancel", "Cancel", new Vector2(300, 80), new Vector2(0, -720));
            var dashboard = CreatePanel(canvas, "ParentDashboardPanel", "👪 Dashboard", out _);
            var dashCtrl = dashboard.AddComponent<ParentDashboardController>();
            dashCtrl.statsText = AddText(dashboard.transform, "Stats", "", 36, new Vector2(0, -200));
            dashCtrl.adsToggleButton = AddButton(dashboard.transform, "AdsToggle", "Ads: ?", new Vector2(380, 80), new Vector2(0, -420));
            dashCtrl.analyticsToggleButton = AddButton(dashboard.transform, "AnalyticsToggle", "Stats: ?", new Vector2(380, 80), new Vector2(0, -520));
            dashCtrl.limitDownButton = AddButton(dashboard.transform, "LimitDown", "−", new Vector2(120, 80), new Vector2(-200, -640));
            dashCtrl.limitUpButton = AddButton(dashboard.transform, "LimitUp", "+", new Vector2(120, 80), new Vector2(200, -640));
            dashCtrl.limitText = AddText(dashboard.transform, "Limit", "", 36, new Vector2(0, -640));
            dashCtrl.closeButton = AddCloseButton(dashboard.transform);
            gateCtrl.dashboardPanel = dashboard;
            hud.parentGatePanel = gate;

            // ---- floating panels (opened by game code) ----
            var dialogue = CreatePanel(canvas, "DialoguePanel", "", out _);
            var dlgCtrl = dialogue.AddComponent<DialoguePanelController>();
            dlgCtrl.nameText = AddText(dialogue.transform, "Name", "", 44, new Vector2(0, -170));
            dlgCtrl.lineText = AddText(dialogue.transform, "Line", "", 38, new Vector2(0, -350));
            dlgCtrl.portraitImage = AddImage(dialogue.transform, "Portrait", new Vector2(160, 160), new Vector2(-320, -260));
            dlgCtrl.nextButton = AddButton(dialogue.transform, "Next", "Next →", new Vector2(300, 80), new Vector2(0, -560));
            dlgCtrl.closeButton = AddCloseButton(dialogue.transform);

            var tutorial = CreatePanel(canvas, "TutorialPanel", "", out _);
            var tutCtrl = tutorial.AddComponent<TutorialPanelController>();
            tutCtrl.hintText = AddText(tutorial.transform, "Hint", "", 40, new Vector2(0, -80));
            tutCtrl.skipButton = AddButton(tutorial.transform, "Skip", "Skip", new Vector2(260, 80), new Vector2(0, -260));
            // Tutorial is a slim banner, not a full card: shrink it.
            var tutCard = tutorial.transform.Find("Card");
            if (tutCard != null) ((RectTransform)tutCard).sizeDelta = new Vector2(900, 420);

            var tractor = CreatePanel(canvas, "TractorPanel", "🚜 Tractor", out _);
            var tractorCtrl = tractor.AddComponent<TractorPanelController>();
            tractorCtrl.statusText = AddText(tractor.transform, "Status", "", 36, new Vector2(0, -580));
            tractorCtrl.plowButton = AddButton(tractor.transform, "Plow", "Plow", new Vector2(280, 80), new Vector2(-150, -300));
            tractorCtrl.plantButton = AddButton(tractor.transform, "Plant", "Plant", new Vector2(280, 80), new Vector2(150, -300));
            tractorCtrl.fertilizeButton = AddButton(tractor.transform, "Fert", "Feed", new Vector2(280, 80), new Vector2(-150, -400));
            tractorCtrl.harvestButton = AddButton(tractor.transform, "Harv", "Harvest", new Vector2(280, 80), new Vector2(150, -400));
            tractorCtrl.exitButton = AddButton(tractor.transform, "Exit", "Get Off", new Vector2(280, 80), new Vector2(0, -520));

            BuildBottomNav(canvasGo, hud);
        }

        // ---------------- panel scaffolding ----------------

        private static GameObject CreatePanel(Transform canvas, string name, string title, out GameObject card)
        {
            var go = new GameObject(name);
            go.transform.SetParent(canvas, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var dim = go.AddComponent<Image>();
            dim.color = DimColor;

            card = new GameObject("Card");
            card.transform.SetParent(go.transform, false);
            var crt = card.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(920, 1280);
            var cimg = card.AddComponent<Image>();
            cimg.color = CardColor;

            if (!string.IsNullOrEmpty(title))
                AddText(go.transform, "Title", title, 46, new Vector2(0, 560));
            go.SetActive(false);
            return go;
        }

        private static GameObject CreateListPanel<T>(Transform canvas, string name, string title,
            GameObject rowPrefab, out T ctrl) where T : Component
        {
            var go = CreatePanel(canvas, name, title, out _);
            ctrl = go.AddComponent<T>();
            var t = typeof(T);
            SetField(t, ctrl, "listContent", CreateScrollContent(go.transform));
            SetField(t, ctrl, "rowPrefab", rowPrefab);
            SetField(t, ctrl, "closeButton", AddCloseButton(go.transform));
            return go;
        }

        private static void SetField(System.Type t, Component ctrl, string field, object value)
        {
            var f = t.GetField(field,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (f != null && value != null) f.SetValue(ctrl, value);
        }

        private static Transform CreateScrollContent(Transform panel)
        {
            var scrollGo = new GameObject("ScrollView");
            scrollGo.transform.SetParent(panel.transform, false);
            var srt = scrollGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 0.5f); srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.anchoredPosition = new Vector2(0, -40);
            srt.sizeDelta = new Vector2(840, 960);
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var vrt = viewport.AddComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.25f);
            viewport.AddComponent<Mask>().showMaskGraphic = true;
            scroll.viewport = vrt;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var crt = content.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
            crt.offsetMin = new Vector2(10, 0); crt.offsetMax = new Vector2(-10, 0);
            crt.pivot = new Vector2(0.5f, 1);
            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12; layout.childControlWidth = true; layout.childControlHeight = false;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = crt;
            return content.transform;
        }

        private static void BuildAnimalDetail(Transform panel, AnimalPanelController ctrl)
        {
            var detail = new GameObject("DetailGroup");
            detail.transform.SetParent(panel, false);
            var drt = detail.AddComponent<RectTransform>();
            drt.anchorMin = new Vector2(0.5f, 0.5f); drt.anchorMax = new Vector2(0.5f, 0.5f);
            drt.sizeDelta = new Vector2(840, 960);
            drt.anchoredPosition = new Vector2(0, -40);
            detail.AddComponent<Image>().color = new Color(0, 0, 0, 0.2f);
            ctrl.detailGroup = detail;
            ctrl.detailTitle = AddText(detail.transform, "DetailTitle", "", 44, new Vector2(0, 380));
            ctrl.detailState = AddText(detail.transform, "DetailState", "", 36, new Vector2(0, 240));
            ctrl.feedButton = AddButton(detail.transform, "Feed", "Feed 🍎", new Vector2(360, 90), new Vector2(0, 60));
            ctrl.petButton = AddButton(detail.transform, "Pet", "Pet 💛", new Vector2(360, 90), new Vector2(0, -60));
            ctrl.collectButton = AddButton(detail.transform, "Collect", "Collect 🧺", new Vector2(360, 90), new Vector2(0, -180));
            ctrl.backButton = AddButton(detail.transform, "Back", "← Back", new Vector2(360, 80), new Vector2(0, -320));
            detail.SetActive(false);
        }

        // ---------------- bottom nav ----------------

        private static void BuildBottomNav(GameObject canvasGo, HudController hud)
        {
            var bar = new GameObject("BottomNav");
            bar.transform.SetParent(canvasGo.transform, false);
            var brt = bar.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0);
            brt.offsetMin = new Vector2(0, 0); brt.offsetMax = new Vector2(0, 300);
            bar.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.14f, 0.95f);

            var items = new (string label, System.Action action)[]
            {
                ("Market", hud.ToggleMarket), ("Seeds", hud.ToggleSeedShop), ("Bag", hud.ToggleInventory),
                ("Quests", hud.ToggleMissions), ("Orders", hud.ToggleOrders), ("Barn", hud.ToggleAnimals),
                ("Factory", hud.ToggleProduction), ("Village", hud.ToggleVillage), ("Equip", hud.ToggleEquipment),
                ("Trees", hud.ToggleOrchard), ("House", hud.ToggleHouse), ("Decor", hud.ToggleDecor),
                ("Parents", hud.ToggleParentGate),
            };
            for (int i = 0; i < items.Length; i++)
            {
                int row = i / 7, col = i % 7;
                var b = AddButton(bar.transform, "Nav" + i, items[i].label,
                    new Vector2(140, 120), Vector2.zero);
                var rt = (RectTransform)b.transform;
                rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(90 + col * 150, -75 - row * 135);
                int idx = i;
                b.onClick.AddListener(() => items[idx].action());
            }
        }

        // ---------------- widget helpers ----------------

        private static Text AddText(Transform parent, string name, string content, int size, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(800, 90);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = Font;
            return text;
        }

        private static Image AddImage(Transform parent, string name, Vector2 size, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.9f, 0.8f, 0.6f);
            return img;
        }

        private static Button AddButton(Transform parent, string name, string label, Vector2 size, Vector2 anchoredPos)
        {
            var go = new GameObject(name + "Button");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = Accent;
            var button = go.AddComponent<Button>();
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var text = labelGo.AddComponent<Text>();
            text.text = label;
            text.fontSize = 36;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = Font;
            return button;
        }

        private static Button AddCloseButton(Transform panel)
        {
            var b = AddButton(panel, "Close", "✕", new Vector2(100, 100), new Vector2(390, 560));
            b.GetComponent<Image>().color = AccentDark;
            var panelGo = panel.gameObject;
            b.onClick.AddListener(() => panelGo.SetActive(false));
            return b;
        }

        // ---------------- prefab assets ----------------

        private static GameObject CreateRowPrefabAsset()
        {
            var go = new GameObject("RowPrefab");
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(800, 96);
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16; layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = false; layout.childForceExpandHeight = true;
            go.AddComponent<Image>().color = new Color(1, 1, 1, 0.06f);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 96; le.flexibleWidth = 1;

            var nameT = AddText(go.transform, "Name", "", 36, Vector2.zero);
            var nle = nameT.gameObject.AddComponent<LayoutElement>();
            nle.flexibleWidth = 1;
            ((RectTransform)nameT.transform).anchorMin = Vector2.zero;
            ((RectTransform)nameT.transform).anchorMax = Vector2.one;
            nameT.alignment = TextAnchor.MiddleLeft;

            var priceT = AddText(go.transform, "Price", "", 36, Vector2.zero);
            var ple = priceT.gameObject.AddComponent<LayoutElement>();
            ple.preferredWidth = 220;
            priceT.alignment = TextAnchor.MiddleRight;

            var btn = AddButton(go.transform, "Button", "Do", new Vector2(200, 76), Vector2.zero);
            var ble = btn.gameObject.AddComponent<LayoutElement>();
            ble.preferredWidth = 200;

            var path = "Assets/Game/UI/Generated/RowPrefab.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateSeedButtonAsset()
        {
            var go = new GameObject("SeedButtonPrefab");
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(380, 130);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.3f, 0.55f, 0.3f);
            var btn = go.AddComponent<Button>();
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 380; le.preferredHeight = 130;
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var text = labelGo.AddComponent<Text>();
            text.fontSize = 32;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = Font;
            var path = "Assets/Game/UI/Generated/SeedButtonPrefab.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                int slash = path.LastIndexOf('/');
                AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
            }
        }
    }
}
