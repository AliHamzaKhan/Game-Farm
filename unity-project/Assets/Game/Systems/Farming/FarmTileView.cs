using System.Collections;
using FarmQuest.Core.Services;
using FarmQuest.Data;
using UnityEngine;

namespace FarmQuest.Systems.Farming
{
    /// <summary>
    /// VIEW: renders one FarmTile — soil colour, the crop's current lifecycle
    /// stage, and a floating indicator for what the plant needs. Uses the crop's
    /// stage prefabs when assigned; otherwise builds a cheap procedural plant
    /// (seed mound → sprout → leaves → flowers → small fruit → ripe fruit) in the
    /// crop's own colours. Animations are short coroutines that run only on change.
    /// Contains zero gameplay logic (§51).
    /// </summary>
    public class FarmTileView : MonoBehaviour
    {
        [Header("Soil visuals per state")]
        public Renderer soilRenderer;
        public Color grassColor = new Color(0.45f, 0.75f, 0.35f);
        public Color clearedColor = new Color(0.6f, 0.5f, 0.35f);
        public Color dugColor = new Color(0.45f, 0.32f, 0.2f);
        public Color preparedColor = new Color(0.5f, 0.36f, 0.22f);
        public Color wetSoilColor = new Color(0.33f, 0.23f, 0.15f);

        [Header("Water indicator")]
        public GameObject dropletIcon; // optional art; procedural indicator is used otherwise

        public FarmTile Tile { get; private set; }
        // Upright, unscaled anchor for plants/indicators (the tile itself is a rotated, scaled quad).
        private Transform _anchor;
        private GameObject _cropVisual;
        private int _shownStage = -1;
        private CropData _shownCrop;
        private GameObject _indicator;
        private Renderer _indicatorRenderer;
        private CropHealth _shownHealth = CropHealth.Healthy;
        private Color _lastProduceColor = Color.white;
        private bool _lastPremium;

        public void Bind(FarmTile tile)
        {
            Tile = tile;
            if (_anchor == null)
            {
                _anchor = new GameObject($"{name}_Plant").transform;
                _anchor.SetParent(transform.parent, false);
                _anchor.position = transform.position;
                _anchor.rotation = Quaternion.identity;
                _anchor.localScale = Vector3.one * 1.6f; // plants fill most of a 2m tile
            }
            GameEvents.TileChanged += OnTileChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            GameEvents.TileChanged -= OnTileChanged;
            if (_anchor != null) Destroy(_anchor.gameObject);
        }

        private void OnTileChanged(FarmTile changed)
        {
            if (changed == Tile) Refresh();
        }

        public void Refresh()
        {
            if (Tile == null || soilRenderer == null || _anchor == null) return;

            bool wet = Tile.HasCrop && !Tile.Crop.NeedsWater && !Tile.Crop.IsReady;
            soilRenderer.material.color = Tile.SoilState switch
            {
                SoilState.Grass => grassColor,
                SoilState.Cleared => clearedColor,
                SoilState.Dug => dugColor,
                _ => wet ? wetSoilColor : preparedColor,
            };

            int wantStage = Tile.HasCrop ? Tile.Crop.CurrentStage : -1;
            var wantCrop = Tile.HasCrop ? Tile.Crop.Data : null;
            if (wantStage != _shownStage || wantCrop != _shownCrop)
            {
                bool harvested = _shownStage == (int)GrowthStage.Mature && wantStage < 0;
                bool planted = _shownStage < 0 && wantStage >= 0;
                _shownStage = wantStage;
                _shownCrop = wantCrop;
                if (_cropVisual != null) { Destroy(_cropVisual); _cropVisual = null; }
                if (harvested) StartCoroutine(CollectBurst(_lastProduceColor, _lastPremium));
                if (wantStage >= 0)
                {
                    _cropVisual = BuildCropVisual(Tile.Crop.Data, (GrowthStage)wantStage);
                    _lastProduceColor = Tile.Crop.Data.produceColor;
                    _lastPremium = Tile.Crop.Data.isPremium;
                    StartCoroutine(Pop(_cropVisual.transform, planted ? 0.2f : 0.6f));
                }
            }

            RefreshIndicator();
        }

        // ---------- plant models ----------

        private GameObject BuildCropVisual(CropData data, GrowthStage stage)
        {
            var prefabs = data.stagePrefabs;
            if (prefabs != null && prefabs.Length > 0)
            {
                var prefab = prefabs[Mathf.Min((int)stage, prefabs.Length - 1)];
                if (prefab != null) return Instantiate(prefab, _anchor.position, Quaternion.identity, _anchor);
            }

            var root = new GameObject($"Crop_{data.cropId}_{stage}");
            root.transform.SetParent(_anchor, false);
            var leaf = data.leafColor;
            var soil = new Color(0.36f, 0.25f, 0.16f);
            switch (stage)
            {
                case GrowthStage.Seed:
                    Part(root, PrimitiveType.Sphere, new Vector3(0, 0.05f, 0), new Vector3(0.45f, 0.12f, 0.45f), soil);
                    Part(root, PrimitiveType.Sphere, new Vector3(0, 0.12f, 0), Vector3.one * 0.1f, new Color(0.85f, 0.75f, 0.5f));
                    break;
                case GrowthStage.Sprout:
                    Stem(root, 0.25f, leaf);
                    Leaves(root, 2, 0.25f, 0.14f, leaf);
                    break;
                case GrowthStage.Vegetative:
                    Stem(root, 0.5f, leaf * 0.9f);
                    Leaves(root, 4, 0.35f, 0.24f, leaf);
                    Leaves(root, 3, 0.5f, 0.2f, leaf * 1.05f);
                    break;
                case GrowthStage.Flowering:
                    Stem(root, 0.6f, leaf * 0.9f);
                    Leaves(root, 4, 0.35f, 0.26f, leaf);
                    Ring(root, 3, 0.65f, 0.14f, 0.11f, data.flowerColor);
                    break;
                case GrowthStage.Fruiting:
                    Stem(root, 0.65f, leaf * 0.9f);
                    Leaves(root, 4, 0.35f, 0.28f, leaf);
                    Ring(root, 3, 0.55f, 0.18f, 0.14f, data.produceColor * 0.85f);
                    break;
                default: // Mature
                    Stem(root, 0.7f, leaf * 0.85f);
                    Leaves(root, 5, 0.35f, 0.3f, leaf);
                    Ring(root, 4, 0.5f, 0.24f, 0.24f, data.produceColor);
                    if (data.isPremium) Ring(root, 6, 0.95f, 0.32f, 0.07f, new Color(1f, 0.92f, 0.4f)); // golden sparkle halo
                    break;
            }
            return root;
        }

        private static void Stem(GameObject root, float height, Color c) =>
            Part(root, PrimitiveType.Cylinder, new Vector3(0, height * 0.5f, 0), new Vector3(0.07f, height * 0.5f, 0.07f), c);

        private static void Leaves(GameObject root, int count, float y, float size, Color c)
        {
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                var p = Part(root, PrimitiveType.Cube, new Vector3(Mathf.Cos(a) * size * 0.6f, y, Mathf.Sin(a) * size * 0.6f),
                    new Vector3(size, 0.04f, size * 0.5f), c);
                p.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 20f);
            }
        }

        private static void Ring(GameObject root, int count, float y, float radius, float size, Color c)
        {
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count + 0.4f;
                Part(root, PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius),
                    Vector3.one * size, c);
            }
        }

        private static GameObject Part(GameObject root, PrimitiveType type, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            // No collider: taps must hit the tile, not the plant.
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.material.color = color;
            return go;
        }

        // ---------- need indicator ----------

        private void RefreshIndicator()
        {
            var health = Tile.HasCrop ? Tile.Crop.Health : CropHealth.Healthy;
            bool show = health != CropHealth.Healthy;
            if (dropletIcon != null)
                dropletIcon.SetActive(health == CropHealth.NeedsWater);
            if (!show)
            {
                if (_indicator != null) _indicator.SetActive(false);
                _shownHealth = health;
                return;
            }
            if (_indicator == null)
            {
                _indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _indicator.name = "NeedIndicator";
                _indicator.transform.SetParent(_anchor, false);
                _indicator.transform.localScale = Vector3.one * 0.28f;
                var col = _indicator.GetComponent<Collider>();
                if (col != null) Destroy(col);
                _indicatorRenderer = _indicator.GetComponent<Renderer>();
            }
            _indicator.SetActive(true);
            if (_indicatorRenderer != null) _indicatorRenderer.material.color = IndicatorColor(health);
            _shownHealth = health;
        }

        public static Color IndicatorColor(CropHealth health)
        {
            switch (health)
            {
                case CropHealth.NeedsWater: return new Color(0.25f, 0.55f, 1f);
                case CropHealth.NeedsFertilizer: return new Color(0.95f, 0.85f, 0.2f);
                case CropHealth.NutrientDeficiency: return new Color(0.7f, 0.35f, 0.9f);
                case CropHealth.PestAttack: return new Color(0.9f, 0.15f, 0.1f);
                case CropHealth.NeedsPruning: return new Color(1f, 0.55f, 0.1f);
                case CropHealth.ReadyToHarvest: return new Color(1f, 0.85f, 0.1f);
                default: return Color.white;
            }
        }

        private void Update()
        {
            // Gentle bob only while an indicator is visible.
            if (_indicator == null || !_indicator.activeSelf) return;
            float bob = Mathf.Sin(Time.time * 3f + transform.position.x) * 0.08f;
            _indicator.transform.localPosition = new Vector3(0f, 1.3f + bob, 0f);
        }

        // ---------- animations ----------

        private static IEnumerator Pop(Transform t, float from)
        {
            if (t == null) yield break;
            Vector3 target = t.localScale;
            const float duration = 0.25f;
            for (float time = 0f; time < duration && t != null; time += Time.deltaTime)
            {
                float k = time / duration;
                float s = Mathf.Lerp(from, 1f, k) + Mathf.Sin(k * Mathf.PI) * 0.15f;
                t.localScale = target * s;
                yield return null;
            }
            if (t != null) t.localScale = target;
        }

        private IEnumerator CollectBurst(Color color, bool premium)
        {
            // Premium crops get a bigger golden burst.
            var bits = new Transform[premium ? 10 : 4];
            for (int i = 0; i < bits.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
                go.GetComponent<Renderer>().material.color = premium && i % 2 == 1 ? new Color(1f, 0.85f, 0.2f) : color;
                go.transform.position = transform.position + new Vector3(0f, 0.4f, 0f);
                go.transform.localScale = Vector3.one * 0.25f;
                bits[i] = go.transform;
            }
            const float duration = 0.6f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                for (int i = 0; i < bits.Length; i++)
                {
                    float a = i * Mathf.PI * 2f / bits.Length;
                    float spread = premium ? 1.3f : 0.8f;
                    bits[i].position = transform.position + new Vector3(Mathf.Cos(a) * k * spread, 0.4f + k * (premium ? 2.2f : 1.5f), Mathf.Sin(a) * k * spread);
                    bits[i].localScale = Vector3.one * 0.25f * (1f - k);
                }
                yield return null;
            }
            foreach (var b in bits) if (b != null) Destroy(b.gameObject);
        }
    }
}
