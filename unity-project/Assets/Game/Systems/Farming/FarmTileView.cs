using FarmQuest.Core.Services;
using FarmQuest.Data;
using UnityEngine;

namespace FarmQuest.Systems.Farming
{
    /// <summary>
    /// VIEW: renders one FarmTile (soil color + crop stage prefab). Listens to
    /// GameEvents.TileChanged and the per-second tick for growth updates.
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

        [Header("Water indicator")]
        public GameObject dropletIcon; // shown when crop needs water

        public FarmTile Tile { get; private set; }
        private GameObject _cropVisual;
        private int _shownStage = -1;

        public void Bind(FarmTile tile)
        {
            Tile = tile;
            GameEvents.TileChanged += OnTileChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            GameEvents.TileChanged -= OnTileChanged;
        }

        private void OnTileChanged(FarmTile changed)
        {
            if (changed == Tile) Refresh();
        }

        public void Refresh()
        {
            if (Tile == null || soilRenderer == null) return;

            soilRenderer.material.color = Tile.SoilState switch
            {
                SoilState.Grass => grassColor,
                SoilState.Cleared => clearedColor,
                SoilState.Dug => dugColor,
                _ => preparedColor,
            };

            // Crop visual per growth stage.
            int wantStage = Tile.HasCrop ? Tile.Crop.CurrentStage : -1;
            if (wantStage != _shownStage)
            {
                _shownStage = wantStage;
                if (_cropVisual != null) { Destroy(_cropVisual); _cropVisual = null; }
                if (wantStage >= 0)
                {
                    var prefabs = Tile.Crop.Data.stagePrefabs;
                    GameObject prefab = prefabs != null && prefabs.Length > 0
                        ? prefabs[Mathf.Min(wantStage, prefabs.Length - 1)] : null;
                    if (prefab != null)
                    {
                        _cropVisual = Instantiate(prefab, transform.position, Quaternion.identity, transform);
                    }
                    else
                    {
                        // Procedural fallback sprout (no art yet): grows with stage.
                        _cropVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        _cropVisual.transform.SetParent(transform, false);
                        _cropVisual.transform.localPosition = new Vector3(0f, 0.3f, 0f);
                        float s = 0.3f + wantStage * 0.25f;
                        _cropVisual.transform.localScale = new Vector3(s, s * 1.6f, s);
                        var rend = _cropVisual.GetComponent<Renderer>();
                        if (rend != null) rend.material.color = new Color(0.3f, 0.72f, 0.28f);
                        // No collider: taps must hit the tile, not the sprout.
                        var col = _cropVisual.GetComponent<Collider>();
                        if (col != null) Destroy(col);
                    }
                }
            }

            if (dropletIcon != null)
                dropletIcon.SetActive(Tile.HasCrop && !Tile.Crop.IsReady &&
                    Tile.Crop.WateringsDone < Tile.Crop.Data.waterRequired);
        }
    }
}
