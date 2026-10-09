using System;
using FarmQuest.Core.Save;
using FarmQuest.Core.Time;

namespace FarmQuest.Systems.Processing
{
    /// <summary>One queued production batch. Timestamp-based; offline-safe.</summary>
    public class ProductionInstance
    {
        public RecipeData Recipe { get; }
        public string BuildingId => Recipe.buildingId;

        private long _finishAtTicks;
        private readonly ITimeService _time;

        public bool ReadyNotified { get; set; }

        public ProductionInstance(RecipeData recipe, ITimeService time)
        {
            Recipe = recipe ?? throw new ArgumentNullException(nameof(recipe));
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _finishAtTicks = _time.UtcNow.Ticks
                + (long)(recipe.durationSeconds * TimeSpan.TicksPerSecond);
        }

        public bool IsReady => _time.UtcNow.Ticks >= _finishAtTicks;

        public float Progress
        {
            get
            {
                long total = (long)(Recipe.durationSeconds * TimeSpan.TicksPerSecond);
                if (total <= 0) return 1f;
                long remaining = _finishAtTicks - _time.UtcNow.Ticks;
                return UnityEngine.Mathf.Clamp01(1f - (float)remaining / total);
            }
        }

        // ---------- save ----------
        public ProductionSaveEntry CaptureState()
        {
            return new ProductionSaveEntry
            {
                recipeId = Recipe.recipeId,
                finishAtTicks = _finishAtTicks
            };
        }

        public void RestoreState(ProductionSaveEntry entry)
        {
            _finishAtTicks = entry.finishAtTicks;
            ReadyNotified = false;
        }
    }
}
