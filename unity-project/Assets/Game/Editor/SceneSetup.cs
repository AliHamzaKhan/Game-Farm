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
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            CreateMainScene();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true),
            };
            Debug.Log("[FarmQuest] Test scene created. Press Play on the Main scene.");
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

        private static void CreateMainScene()
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
            player.AddComponent<PlayerCustomization>();
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
            hud.clockText = MakeText(hudGo.transform, "Clock", "", 36, new Vector2(0, -30), TextAnchor.UpperCenter);

            // Plot panel (the core interaction UI).
            var plotGo = new GameObject("PlotPanel");
            plotGo.transform.SetParent(canvasGo.transform, false);
            var plot = plotGo.AddComponent<PlotPanelController>();
            var panelImg = plotGo.AddComponent<Image>();
            panelImg.color = new Color(0.12f, 0.12f, 0.16f, 0.92f);
            var rt = plotGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0.5f); rt.anchorMax = new Vector2(1, 0.5f);
            rt.anchoredPosition = new Vector2(-260, 0); rt.sizeDelta = new Vector2(480, 900);
            plot.titleText = MakeText(plotGo.transform, "Title", "Empty Plot", 40, Vector2.zero, TextAnchor.MiddleCenter, 0);
            plot.clearButton = MakeButton(plotGo.transform, "Clear", "Clear 🌿", 1);
            plot.digButton = MakeButton(plotGo.transform, "Dig", "Dig ⛏️", 2);
            plot.prepareButton = MakeButton(plotGo.transform, "Prepare", "Prepare 🌱", 3);
            plot.plantButton = MakeButton(plotGo.transform, "Plant", "Plant 🌾", 4);
            plot.waterButton = MakeButton(plotGo.transform, "Water", "Water 💧", 5);
            plot.compostButton = MakeButton(plotGo.transform, "Compost", "Compost ♻️", 6);
            plot.nutrientButton = MakeButton(plotGo.transform, "Nutrient", "Feed ✨", 7);
            plot.harvestButton = MakeButton(plotGo.transform, "Harvest", "Harvest 🧺", 8);
            plot.closeButton = MakeButton(plotGo.transform, "Close", "Close ✖️", 9);
            plotGo.SetActive(false);

            // Tap-to-tile interaction.
            var interactionGo = new GameObject("FarmInteraction");
            var interaction = interactionGo.AddComponent<FarmInteractionController>();
            interaction.tileLayer = 1 << TileLayer;
            interaction.plotPanel = plot;

            EnsureFolder("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
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
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return text;
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
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
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
