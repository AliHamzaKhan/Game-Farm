using UnityEditor;
using UnityEngine;
using FarmQuest.Systems.Animals;
using FarmQuest.Systems.Buildings;
using FarmQuest.Systems.Fishing;
using FarmQuest.Systems.Machines;
using FarmQuest.Systems.Npcs;
using FarmQuest.Systems.Orchard;
using FarmQuest.Systems.Wildlife;

namespace FarmQuest.Editor
{
    /// <summary>
    /// Places the living world: house (upgradable), tractor, 8 NPC villagers,
    /// animal pen, orchard, pet, fishing pond, wildlife. Procedural placeholders
    /// so the ownership fantasy ("this is MY farm") is visible without art.
    /// Called by SceneSetup after the core scene exists.
    /// </summary>
    public static class SceneSetupWorld
    {
        public static void Build()
        {
            EnsureFolder("Assets/Game/World/Generated");
            BuildHouse();
            BuildTractor();
            BuildNpcs();
            BuildAnimalPen();
            BuildOrchard();
            BuildPet();
            BuildPond();
            BuildWildlife();
        }

        // ---------------- house ----------------

        private static void BuildHouse()
        {
            var go = new GameObject("House");
            go.transform.position = new Vector3(-12f, 0f, -8f);
            var view = go.AddComponent<HouseView>();
            view.buildingId = "house";
            view.levelPrefabs = new[]
            {
                SavePrefab(BuildHouseLevel(1), "Assets/Game/World/Generated/House_L1.prefab"),
                SavePrefab(BuildHouseLevel(2), "Assets/Game/World/Generated/House_L2.prefab"),
                SavePrefab(BuildHouseLevel(3), "Assets/Game/World/Generated/House_L3.prefab"),
                SavePrefab(BuildHouseLevel(4), "Assets/Game/World/Generated/House_L4.prefab"),
            };
            // Tappable.
            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(9f, 8f, 7f);
            col.center = new Vector3(0f, 4f, 0f);
        }

        private static GameObject BuildHouseLevel(int level)
        {
            var root = new GameObject($"House_L{level}");
            var walls = new Color(0.95f, 0.9f, 0.8f);
            var roofC = new Color(0.7f, 0.3f, 0.2f);
            float w = 4f + level;          // width grows
            float d = 4f + level * 0.5f;   // depth grows
            float h = 3f + (level >= 3 ? 3f : 0f); // 2 stories at L3+

            AddBox(root, "Walls", new Vector3(w, h, d), new Vector3(0, h / 2, 0), walls);
            AddBox(root, "Roof", new Vector3(w + 1.5f, 1f, d + 1.5f), new Vector3(0, h + 0.5f, 0), roofC);
            AddBox(root, "Door", new Vector3(1.2f, 2.2f, 0.3f), new Vector3(0, 1.1f, d / 2 + 0.1f),
                new Color(0.45f, 0.28f, 0.15f));
            // Windows.
            AddBox(root, "WinL", new Vector3(1f, 1f, 0.2f), new Vector3(-w / 4, h / 2, d / 2 + 0.05f),
                new Color(0.3f, 0.45f, 0.6f));
            AddBox(root, "WinR", new Vector3(1f, 1f, 0.2f), new Vector3(w / 4, h / 2, d / 2 + 0.05f),
                new Color(0.3f, 0.45f, 0.6f));
            if (level >= 2)
                AddBox(root, "Chimney", new Vector3(0.8f, 2f, 0.8f),
                    new Vector3(w / 4, h + 1.5f, 0), new Color(0.6f, 0.35f, 0.3f));
            if (level >= 4)
            {
                // Porch.
                AddBox(root, "Porch", new Vector3(w, 0.4f, 2f), new Vector3(0, 0.2f, d / 2 + 1f),
                    new Color(0.6f, 0.5f, 0.4f));
                AddBox(root, "PorchRoof", new Vector3(w, 0.3f, 2.2f), new Vector3(0, 2.8f, d / 2 + 1f), roofC);
            }
            return root;
        }

        // ---------------- tractor ----------------

        private static void BuildTractor()
        {
            var go = new GameObject("Tractor");
            go.transform.position = new Vector3(34f, 0f, 2f);
            go.transform.rotation = Quaternion.Euler(0f, -30f, 0f);
            go.AddComponent<TractorController>();
            go.AddComponent<TractorView>(); // IInteractable: tap to drive

            var green = new Color(0.25f, 0.55f, 0.25f);
            var dark = new Color(0.15f, 0.15f, 0.15f);
            AddBox(go, "Body", new Vector3(2.6f, 1.1f, 1.7f), new Vector3(0, 1.1f, 0), green);
            AddBox(go, "Cabin", new Vector3(1.2f, 1.2f, 1.5f), new Vector3(-0.5f, 2.2f, 0),
                new Color(0.35f, 0.5f, 0.65f));
            AddBox(go, "Hood", new Vector3(1.2f, 0.7f, 1.5f), new Vector3(1.1f, 1.4f, 0), green);
            AddBox(go, "Exhaust", new Vector3(0.15f, 1.2f, 0.15f), new Vector3(1.3f, 2f, 0.4f), dark);
            // Wheels.
            foreach (var (x, z, r) in new[] { (1f, 0.95f, 0.55f), (1f, -0.95f, 0.55f), (-0.9f, 0.95f, 0.4f), (-0.9f, -0.95f, 0.4f) })
            {
                var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "Wheel";
                wheel.transform.SetParent(go.transform, false);
                wheel.transform.localPosition = new Vector3(x, r, z);
                wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                wheel.transform.localScale = new Vector3(r * 2f, 0.4f, r * 2f);
                wheel.GetComponent<Renderer>().material.color = dark;
            }
            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(3.2f, 3f, 2.4f);
            col.center = new Vector3(0, 1.5f, 0);
        }

        // ---------------- NPCs ----------------

        private static void BuildNpcs()
        {
            var npcs = new (string id, Vector3 pos, Color color)[]
            {
                ("grandpa", new Vector3(-8f, 0f, -4f), new Color(0.7f, 0.65f, 0.6f)),
                ("builder", new Vector3(-14f, 0f, -2f), new Color(0.8f, 0.6f, 0.3f)),
                ("seed_seller", new Vector3(4f, 0f, -4f), new Color(0.4f, 0.7f, 0.4f)),
                ("equipment_dealer", new Vector3(32f, 0f, 6f), new Color(0.6f, 0.6f, 0.7f)),
                ("animal_doctor", new Vector3(34f, 0f, 14f), new Color(0.95f, 0.95f, 0.95f)),
                ("market_owner", new Vector3(10f, 0f, 34f), new Color(0.75f, 0.5f, 0.35f)),
                ("restaurant_owner", new Vector3(20f, 0f, 34f), new Color(0.85f, 0.45f, 0.55f)),
                ("teacher", new Vector3(-6f, 0f, 26f), new Color(0.45f, 0.55f, 0.85f)),
            };
            foreach (var (id, pos, color) in npcs)
            {
                var go = new GameObject($"NPC_{id}");
                go.transform.position = pos;
                var view = go.AddComponent<NpcView>();
                view.npcId = id;
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Body";
                body.transform.SetParent(go.transform, false);
                body.transform.localPosition = Vector3.up * 0.9f;
                body.transform.localScale = new Vector3(0.9f, 1.1f, 0.9f);
                body.GetComponent<Renderer>().material.color = color;
                // Head.
                var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                head.name = "Head";
                head.transform.SetParent(go.transform, false);
                head.transform.localPosition = new Vector3(0, 2f, 0);
                head.transform.localScale = Vector3.one * 0.55f;
                head.GetComponent<Renderer>().material.color = new Color(0.95f, 0.8f, 0.65f);
            }
        }

        // ---------------- animal pen ----------------

        private static void BuildAnimalPen()
        {
            var go = new GameObject("AnimalPen");
            go.transform.position = new Vector3(36f, 0f, 18f);
            var mgr = go.AddComponent<AnimalViewManager>();
            var origin = new GameObject("PenOrigin");
            origin.transform.SetParent(go.transform, false);
            origin.transform.localPosition = new Vector3(-4f, 0f, -4f);
            mgr.penOrigin = origin.transform;
            mgr.spacing = 3f;
            mgr.perRow = 4;

            // Fence square 12x12.
            var fenceC = new Color(0.55f, 0.4f, 0.25f);
            float s = 6f;
            AddBox(go, "FenceN", new Vector3(s * 2, 1f, 0.3f), new Vector3(0, 0.5f, -s), fenceC);
            AddBox(go, "FenceS", new Vector3(s * 2, 1f, 0.3f), new Vector3(0, 0.5f, s), fenceC);
            AddBox(go, "FenceW", new Vector3(0.3f, 1f, s * 2), new Vector3(-s, 0.5f, 0), fenceC);
            AddBox(go, "FenceE", new Vector3(0.3f, 1f, s * 2), new Vector3(s, 0.5f, 0), fenceC);
            // Hay bale.
            var hay = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hay.name = "Hay";
            hay.transform.SetParent(go.transform, false);
            hay.transform.localPosition = new Vector3(3.5f, 0.6f, 3.5f);
            hay.transform.localScale = new Vector3(1.6f, 1.2f, 1.6f);
            hay.GetComponent<Renderer>().material.color = new Color(0.85f, 0.7f, 0.35f);
        }

        // ---------------- orchard ----------------

        private static void BuildOrchard()
        {
            var go = new GameObject("Orchard");
            go.transform.position = new Vector3(-12f, 0f, 22f);
            var mgr = go.AddComponent<TreeViewManager>();
            var origin = new GameObject("OrchardOrigin");
            origin.transform.SetParent(go.transform, false);
            origin.transform.localPosition = new Vector3(-6f, 0f, -6f);
            mgr.orchardOrigin = origin.transform;
            mgr.spacing = 4f;
            mgr.perRow = 4;

            // Sign.
            AddBox(go, "Sign", new Vector3(3f, 1.2f, 0.3f), new Vector3(0, 1.6f, -8f),
                new Color(0.5f, 0.38f, 0.22f));
        }

        // ---------------- pet ----------------

        private static void BuildPet()
        {
            var go = new GameObject("Pet_Dog");
            go.transform.position = new Vector3(2f, 0f, 8f);
            go.AddComponent<PetView>();
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = Vector3.up * 0.45f;
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale = new Vector3(0.55f, 0.9f, 0.55f);
            body.GetComponent<Renderer>().material.color = new Color(0.6f, 0.42f, 0.25f);
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(go.transform, false);
            head.transform.localPosition = new Vector3(0, 0.75f, 0.55f);
            head.transform.localScale = Vector3.one * 0.45f;
            head.GetComponent<Renderer>().material.color = new Color(0.6f, 0.42f, 0.25f);
            // Ears.
            foreach (float x in new[] { -0.18f, 0.18f })
            {
                var ear = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ear.name = "Ear";
                ear.transform.SetParent(go.transform, false);
                ear.transform.localPosition = new Vector3(x, 1f, 0.5f);
                ear.transform.localScale = new Vector3(0.16f, 0.28f, 0.12f);
                ear.GetComponent<Renderer>().material.color = new Color(0.45f, 0.3f, 0.18f);
            }
        }

        // ---------------- fishing pond ----------------

        private static void BuildPond()
        {
            var go = new GameObject("FishingPond");
            go.transform.position = new Vector3(15f, 0f, 36f);
            var fishing = go.AddComponent<FishingController>(); // IInteractable: tap to fish
            // Assign the generated fish species.
            foreach (string fishId in new[] { "carp", "bass", "catfish", "koi" })
            {
                var fish = AssetDatabase.LoadAssetAtPath<FishData>(
                    $"Assets/Resources/Data/Fish_{fishId}.asset");
                if (fish != null) fishing.fishSpecies.Add(fish);
            }

            var water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            water.name = "Water";
            water.transform.SetParent(go.transform, false);
            water.transform.localPosition = new Vector3(0, 0.05f, 0);
            water.transform.localScale = new Vector3(8f, 0.2f, 8f);
            water.GetComponent<Renderer>().material.color = new Color(0.3f, 0.55f, 0.75f);
            // Sandy rim.
            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Rim";
            rim.transform.SetParent(go.transform, false);
            rim.transform.localPosition = new Vector3(0, 0.02f, 0);
            rim.transform.localScale = new Vector3(9.5f, 0.1f, 9.5f);
            rim.GetComponent<Renderer>().material.color = new Color(0.8f, 0.72f, 0.55f);
            // Dock.
            AddBox(go, "Dock", new Vector3(1.6f, 0.25f, 4f), new Vector3(0, 0.25f, -5f),
                new Color(0.55f, 0.4f, 0.25f));
        }

        // ---------------- wildlife ----------------

        private static void BuildWildlife()
        {
            var go = new GameObject("Wildlife");
            var svc = go.AddComponent<WildlifeService>();
            svc.spawnArea = new Vector2(45f, 45f);
        }

        // ---------------- helpers ----------------

        private static void AddBox(GameObject parent, string name, Vector3 size, Vector3 localPos, Color color)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent.transform, false);
            box.transform.localPosition = localPos;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().material.color = color;
        }

        private static GameObject SavePrefab(GameObject go, string path)
        {
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
