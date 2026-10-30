using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using FarmQuest.Core.Config;
using FarmQuest.Data;
using FarmQuest.Gameplay.Player;
using FarmQuest.Systems.Audio;
using FarmQuest.Systems.Farming;
using FarmQuest.UI;

namespace FarmQuest.Editor
{
    /// <summary>
    /// One-click test scene (Phase 6). Run AFTER FarmQuest/Generate/All Game Data.
    /// Creates a single scene:
    ///   Assets/Scenes/Main.unity — Bootstrapper (services + save) plus the farm:
    ///   camera, input, player, tile grid, HUD, plot panel.
    /// Single scene on purpose: no cross-scene loading, so it can't break when
    /// Unity's build-profile scene list drifts. The 3D farm + core tap loop
    /// (clear → dig → prepare → plant → water → harvest) works immediately;
    /// shop/mission/etc. panels are shells for manual UI wiring later.
    /// </summary>
    public static class SceneSetup
    {
        private const int TileLayer = 8;
        private const string DataRoot = "Assets/Resources/Data";

        [MenuItem("FarmQuest/Setup/Create Test Scenes")]
        public static void CreateTestScenes()
        {
            Debug.Log("[FarmQuest] Create Test Scenes started.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[FarmQuest] Cancelled: the save-current-scene dialog was cancelled. Run Create Test Scenes again and pick Save or Don't Save.");
                return;
            }

            // Bootstrapper must Awake before any other script's Start.
            var bootImporter = AssetImporter.GetAtPath(
                "Assets/Game/Core/Services/Bootstrapper.cs") as MonoImporter;
            if (bootImporter != null) MonoImporter.SetExecutionOrder(bootImporter.GetScript(), -100);

            if (!CreateMainScene()) return;

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainScenePath, true),
            };
            Debug.Log($"[FarmQuest] Test scene saved to {MainScenePath}. Press Play on the Main scene.");
            // Highlight the new scene in the Project window.
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath);
            if (sceneAsset != null) EditorGUIUtility.PingObject(sceneAsset);
        }

        // ---------------- Boot (services + save) ----------------

        private static void CreateBootObject()
        {
            var boot = new GameObject("Boot");
            var bootstrapper = boot.AddComponent<Core.Services.Bootstrapper>();

            // Assign generated data assets (must run Generate first).
            bootstrapper.cropDatabase = Load<CropDatabase>("CropDatabase");
            bootstrapper.progressionData = Load<ProgressionData>("Progression");
            bootstrapper.farmLayout = Load<FarmLayoutData>("FarmLayout");
            bootstrapper.balanceConfig = Load<BalanceConfig>("BalanceConfig");
            bootstrapper.animalDatabase = Load<Systems.Animals.AnimalDatabase>("AnimalDatabase");
            bootstrapper.machineDatabase = Load<Systems.Machines.MachineDatabase>("MachineDatabase");
            bootstrapper.villageDatabase = Load<Systems.Village.VillageDatabase>("VillageDatabase");
        }

        private static T Load<T>(string name) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>($"{DataRoot}/{name}.asset");
            if (asset == null)
                Debug.LogWarning($"[FarmQuest] Missing {DataRoot}/{name}.asset — run FarmQuest/Generate/All Game Data first.");
            return asset;
        }

        // ---------------- Main ----------------

        private const string MainScenePath = "Assets/Scenes/Main.unity";

        /// <summary>Builds and saves Main.unity. Returns false if the save failed.</summary>
        private static bool CreateMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Boot: services + save (Awake runs before everything else's Start).
            CreateBootObject();

            // Camera (isometric, follows player).
            var camGo = GameObject.Find("Main Camera");
            var cam = camGo.GetComponent<UnityEngine.Camera>();
            camGo.transform.position = new Vector3(0f, 18f, -10f);
            var camCtrl = camGo.AddComponent<Camera.FarmCameraController>();

            // Input + audio.
            new GameObject("Input").AddComponent<Input.InputService>();
            new GameObject("Audio").AddComponent<AudioService>();

            // Player.
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 0f, 6f);
            var playerCtrl = player.AddComponent<PlayerController>();
            playerCtrl.groundLayer = 1 << TileLayer;
            var customization = player.AddComponent<PlayerCustomization>();
            // Placeholder farmer body so the player is visible. No collider:
            // taps must reach the tiles underneath.
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            customization.bodyRenderer = body.GetComponent<Renderer>();
            camCtrl.Follow(player.transform);

            // Tile grid.
            var grid = new GameObject("FarmGrid");
            var builder = grid.AddComponent<FarmGridBuilder>();
            builder.tileLayer = TileLayer;

            // Canvas + HUD.
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGo.AddComponent<GraphicRaycaster>();
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var hudGo = new GameObject("Hud");
            hudGo.transform.SetParent(canvasGo.transform, false);
            var hud = hudGo.AddComponent<HudController>();
            hud.coinsText = MakeText(hudGo.transform, "Coins", "🪙 100", 44, new Vector2(-20, -30), TextAnchor.UpperRight);
            hud.levelText = MakeText(hudGo.transform, "Level", "Lv 1", 40, new Vector2(20, -30), TextAnchor.UpperLeft);
            hud.gemsText = MakeText(hudGo.transform, "Gems", "Gems 0", 38, new Vector2(-20, -150), TextAnchor.UpperRight);
            hud.clockText = MakeText(hudGo.transform, "Clock", "", 36, new Vector2(0, -30), TextAnchor.UpperCenter);
            hud.cityText = MakeText(hudGo.transform, "City", "", 34, new Vector2(0, -95), TextAnchor.UpperCenter);
            ((RectTransform)hud.cityText.transform).sizeDelta = new Vector2(1000, 60);

            // Plot panel (the core interaction UI).
            var plotGo = new GameObject("PlotPanel");
            plotGo.transform.SetParent(canvasGo.transform, false);
            var plot = plotGo.AddComponent<PlotPanelController>();
            var panelImg = plotGo.AddComponent<Image>();
            panelImg.color = new Color(0.12f, 0.12f, 0.16f, 0.92f);
            var rt = plotGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0.5f); rt.anchorMax = new Vector2(1, 0.5f);
            rt.anchoredPosition = new Vector2(-280, 120); rt.sizeDelta = new Vector2(540, 1340);
            plot.titleText = MakeText(plotGo.transform, "Title", "Empty Plot", 40, new Vector2(0, -20), TextAnchor.UpperCenter, 0);
            ((RectTransform)plot.titleText.transform).sizeDelta = new Vector2(500, 110);
            plot.infoText = MakeText(plotGo.transform, "Info", "", 28, new Vector2(0, -135), TextAnchor.UpperLeft, 0);
            ((RectTransform)plot.infoText.transform).sizeDelta = new Vector2(500, 250);
            // Two-column action grid; controllers hide what doesn't apply right now.
            plot.clearButton = MakeGridButton(plotGo.transform, "Clear", "Clear", 0, 0);
            plot.digButton = MakeGridButton(plotGo.transform, "Dig", "Dig", 1, 0);
            plot.prepareButton = MakeGridButton(plotGo.transform, "Prepare", "Prepare", 0, 1);
            plot.plantButton = MakeGridButton(plotGo.transform, "Plant", "Plant", 1, 1);
            plot.waterButton = MakeGridButton(plotGo.transform, "Water", "Water", 0, 2);
            plot.nutrientButton = MakeGridButton(plotGo.transform, "Fertilize", "Fertilize", 1, 2);
            plot.specialNutrientButton = MakeGridButton(plotGo.transform, "SpecialNutrient", "Nutrient", 0, 3);
            plot.sprayButton = MakeGridButton(plotGo.transform, "Spray", "Spray pests", 1, 3);
            plot.pruneButton = MakeGridButton(plotGo.transform, "Prune", "Prune", 0, 4);
            plot.compostButton = MakeGridButton(plotGo.transform, "Compost", "Compost", 1, 4);
            plot.harvestButton = MakeGridButton(plotGo.transform, "Harvest", "Harvest", 0, 5);
            // Consumables only show on a planted crop, so they share the soil buttons' slots.
            var itemColor = new Color(0.55f, 0.4f, 0.8f);
            plot.premiumFertilizerButton = MakeGridButton(plotGo.transform, "PremiumFertilizer", "Premium Fertilizer", 0, 0, itemColor);
            plot.liquidNutrientButton = MakeGridButton(plotGo.transform, "LiquidNutrient", "Liquid Nutrients", 1, 0, itemColor);
            plot.growthBoosterButton = MakeGridButton(plotGo.transform, "GrowthBooster", "Growth Booster", 0, 1, itemColor);
            plot.superCompostButton = MakeGridButton(plotGo.transform, "SuperCompost", "Super Compost", 1, 1, itemColor);
            plot.adOfferText = MakeText(plotGo.transform, "AdOffer", "", 26, new Vector2(0, -900), TextAnchor.UpperCenter, 0);
            ((RectTransform)plot.adOfferText.transform).sizeDelta = new Vector2(500, 90);
            var adColor = new Color(0.3f, 0.55f, 0.85f);
            plot.adSpeedupButton = MakeGridButton(plotGo.transform, "AdSpeed", "Ad: faster", 0, 7, adColor);
            plot.adStageButton = MakeGridButton(plotGo.transform, "AdStage", "Ad: next stage", 1, 7, adColor);
            plot.adFinishButton = MakeGridButton(plotGo.transform, "AdFinish", "Ad: finish now", 0, 8, adColor);
            plot.adYieldButton = MakeGridButton(plotGo.transform, "AdYield", "Ad: +50% crop", 1, 8, adColor);
            plot.closeButton = MakeGridButton(plotGo.transform, "Close", "Close", 0, 9);
            ((RectTransform)plot.closeButton.transform).anchoredPosition = new Vector2(0, -1260);
            plotGo.SetActive(false);

            // All game panels + bottom nav (audit fix).
            SceneSetupPanels.BuildAll(canvasGo, hud);

            // Living world: house, tractor, NPCs, pen, orchard, pet, pond, wildlife.
            SceneSetupWorld.Build();

            // Tap-to-tile interaction.
            var interactionGo = new GameObject("FarmInteraction");
            var interaction = interactionGo.AddComponent<FarmInteractionController>();
            interaction.tileLayer = 1 << TileLayer;
            interaction.plotPanel = plot;

            EnsureFolder("Assets/Scenes");
            if (!EditorSceneManager.SaveScene(scene, MainScenePath))
            {
                Debug.LogError($"[FarmQuest] Could not save {MainScenePath}. Check the folder is writable, then run Create Test Scenes again.");
                return false;
            }
            AssetDatabase.Refresh();
            return true;
        }

        // ---------------- UI helpers ----------------

        private static Text MakeText(Transform parent, string name, string content,
            int fontSize, Vector2 anchoredPos, TextAnchor anchor, int order = 0)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(500, 80);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
        }

        private static Button MakeGridButton(Transform parent, string name, string label, int col, int row, Color? color = null)
        {
            var b = MakeButton(parent, name, label, 0);
            var rt = (RectTransform)b.transform;
            rt.sizeDelta = new Vector2(240, 72);
            rt.anchoredPosition = new Vector2(col == 0 ? -127 : 127, -420 - row * 84);
            if (color.HasValue) b.GetComponent<Image>().color = color.Value;
            var text = b.GetComponentInChildren<Text>();
            if (text != null) text.fontSize = 30;
            return b;
        }

        private static Button MakeButton(Transform parent, string name, string label, int row)
        {
            var go = new GameObject(name + "Button");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -120 - row * 85);
            rt.sizeDelta = new Vector2(400, 72);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.95f, 0.45f, 0.35f);
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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return button;
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
                string name = System.IO.Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
