using System.Collections.Generic;
using FarmQuest.Core.Services;
using FarmQuest.Core.Time;

namespace FarmQuest.Systems.Farming
{
    /// <summary>
    /// LOGIC: watches growing crops and fires events on stage change / ready.
    /// Polls once per second via ITimeService.SecondTick — no per-crop Update() (§49).
    /// Offline crops simply compute progress from timestamps (§9).
    /// </summary>
    public class CropGrowthService
    {
        private readonly ITimeService _time;
        private readonly Dictionary<CropInstance, FarmTile> _tracked = new Dictionary<CropInstance, FarmTile>();
        private readonly Dictionary<CropInstance, int> _lastStage = new Dictionary<CropInstance, int>();

        public CropGrowthService(ITimeService time)
        {
            _time = time;
            _time.SecondTick += OnSecondTick;
        }

        public void Track(CropInstance crop, FarmTile tile)
        {
            if (crop == null || _tracked.ContainsKey(crop)) return;
            _tracked[crop] = tile;
            _lastStage[crop] = crop.CurrentStage;
            // Offline case: already ready while away.
            if (crop.IsReady) NotifyReady(crop, tile);
        }

        public void Untrack(CropInstance crop)
        {
            _tracked.Remove(crop);
            _lastStage.Remove(crop);
        }

        public void ApplyRainToAll()
        {
            foreach (var kvp in _tracked)
            {
                kvp.Key.ApplyRain();
                GameEvents.RaiseTileChanged(kvp.Value);
            }
        }

        private void OnSecondTick()
        {
            // Copy keys: NotifyReady may be called during iteration safety.
            var snapshot = new List<KeyValuePair<CropInstance, FarmTile>>(_tracked);
            foreach (var kvp in snapshot)
            {
                var crop = kvp.Key;
                var tile = kvp.Value;
                int stage = crop.CurrentStage;
                if (_lastStage.TryGetValue(crop, out int last) && stage != last)
                {
                    _lastStage[crop] = stage;
                    if (stage > 0) tile.MarkGrowing();
                    GameEvents.RaiseTileChanged(tile);
                }
                if (crop.IsReady) NotifyReady(crop, tile);
            }
        }

        private void NotifyReady(CropInstance crop, FarmTile tile)
        {
            if (crop.ReadyNotified) return;
            crop.ReadyNotified = true;
            tile.MarkReady();
            GameEvents.RaiseTileChanged(tile);
            GameEvents.RaiseCropReady(crop);
        }
    }
}
