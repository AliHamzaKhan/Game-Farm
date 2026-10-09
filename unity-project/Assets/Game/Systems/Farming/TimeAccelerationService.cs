using FarmQuest.Core.Services;

namespace FarmQuest.Systems.Farming
{
    /// <summary>
    /// Generic time-reduction (§11): waiting, farm items, rewarded ads, equipment
    /// all funnel through here. Exposes remaining/original durations uniformly.
    /// </summary>
    public class TimeAccelerationService
    {
        public float GetRemainingSeconds(CropInstance crop) => crop.RemainingSeconds;
        public float GetOriginalDuration(CropInstance crop) => crop.Data.growthDurationSeconds;

        /// <summary>Reduces a crop's remaining time. Returns actual seconds reduced.</summary>
        public float AccelerateCrop(CropInstance crop, float seconds)
        {
            if (crop == null || crop.IsReady) return 0f;
            float before = crop.RemainingSeconds;
            crop.ReduceRemainingTime(seconds);
            float reduced = before - crop.RemainingSeconds;
            var tile = FindTile(crop);
            if (tile != null) GameEvents.RaiseTileChanged(tile);
            return reduced;
        }

        // Minimal tile lookup; a bidirectional map can replace this later.
        private FarmTile _lastTile;
        public void NoteTile(CropInstance crop, FarmTile tile)
        {
            if (tile != null && tile.Crop == crop) _lastTile = tile;
        }
        private FarmTile FindTile(CropInstance crop)
        {
            if (_lastTile != null && _lastTile.Crop == crop) return _lastTile;
            return null;
        }
    }
}
