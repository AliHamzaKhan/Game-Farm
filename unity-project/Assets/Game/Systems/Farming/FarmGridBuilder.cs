using FarmQuest.Core.Services;
using UnityEngine;

namespace FarmQuest.Systems.Farming
{
    /// <summary>
    /// VIEW: spawns one tappable quad per farm tile at boot (Phase 6 test slice).
    /// Put on an empty GameObject in the farm scene. Tiles use <see cref="FarmTileView"/>
    /// for soil/crop visuals. Replace quads with art prefabs when ready —
    /// the game logic does not depend on this builder.
    /// </summary>
    public class FarmGridBuilder : MonoBehaviour
    {
        [Tooltip("Physics layer index the tiles live on (must match FarmInteractionController.tileLayer).")]
        public int tileLayer = 8;

        [Tooltip("Gap between tiles, as a fraction of tile size.")]
        [Range(0f, 0.3f)] public float gap = 0.05f;

        private void Start()
        {
            var farm = ServiceLocator.Get<FarmService>();
            float size = farm.TileSize * (1f - gap);
            foreach (var tile in farm.AllTiles())
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = $"Tile_{tile.GridPosition.x}_{tile.GridPosition.y}";
                go.transform.SetParent(transform, false);
                go.transform.position = farm.GridToWorld(tile.GridPosition);
                go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
                go.transform.localScale = new Vector3(size, size, 1f);
                go.layer = tileLayer;

                var view = go.AddComponent<FarmTileView>();
                view.soilRenderer = go.GetComponent<Renderer>();
                view.Bind(tile);
            }
        }
    }
}
