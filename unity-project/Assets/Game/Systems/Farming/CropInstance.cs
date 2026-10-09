using System;
using FarmQuest.Core.Time;
using FarmQuest.Data;
using UnityEngine;

namespace FarmQuest.Systems.Farming
{
    /// <summary>
    /// LOGIC+DATA: one planted crop (§7, §8). Progress derives from ITimeService,
    /// so offline time simply works — no per-crop timers (§9).
    /// </summary>
    public class CropInstance
    {
        public CropData Data { get; }
        public long PlantedAtTicks { get; private set; }
        public int WateringsDone { get; private set; }
        public bool CompostApplied { get; private set; }
        public bool NutrientApplied { get; private set; }
        public bool ReadyNotified { get; set; }

        private readonly ITimeService _time;

        public CropInstance(CropData data, ITimeService time, long plantedAtTicks)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            _time = time ?? throw new ArgumentNullException(nameof(time));
            PlantedAtTicks = plantedAtTicks;
        }

        /// <summary>
        /// Growth time multiplier (Phase 5 seasons): 1 = normal, 2 = half speed
        /// when planted out of season. Never blocks planting — kid-friendly.
        /// </summary>
        public float GrowthTimeMultiplier { get; set; } = 1f;

        public float Progress
        {
            get
            {
                float duration = Data.growthDurationSeconds * GrowthTimeMultiplier;
                if (duration <= 0) return 1f;
                double elapsed = _time.GetElapsedSeconds(PlantedAtTicks);
                return Mathf.Clamp01((float)(elapsed / duration));
            }
        }

        public float RemainingSeconds =>
            Mathf.Max(0f, Data.growthDurationSeconds * GrowthTimeMultiplier
                - (float)_time.GetElapsedSeconds(PlantedAtTicks));

        public int CurrentStage =>
            Mathf.Min(Data.growthStages - 1, (int)(Progress * Data.growthStages));

        public bool IsReady => Progress >= 1f;

        public void Water()
        {
            if (IsReady) return;
            WateringsDone++;
        }

        public void ApplyCompost() => CompostApplied = true;
        public void ApplyNutrient() => NutrientApplied = true;

        /// <summary>Rain counts as one watering (§15).</summary>
        public void ApplyRain()
        {
            if (!IsReady && WateringsDone < Data.waterRequired) WateringsDone++;
        }

        /// <summary>Time acceleration (§11): shifts planting earlier.</summary>
        public void ReduceRemainingTime(float seconds)
        {
            PlantedAtTicks -= (long)(Mathf.Max(0f, seconds) * TimeSpan.TicksPerSecond);
        }

        /// <summary>
        /// Quality (§14): kids are never punished — missed care only lowers grade.
        /// Crops never die.
        /// </summary>
        public QualityGrade CalculateQuality()
        {
            bool waterOk = WateringsDone >= Data.waterRequired;
            bool compostOk = CompostApplied || !Data.compostRequiredForTopQuality;
            bool nutrientOk = NutrientApplied;

            int score = 0;
            if (waterOk) score++;
            if (compostOk) score++;
            if (nutrientOk) score++;

            // Harvest timing: within a generous "perfect window" after ready.
            double secondsSinceReady = _time.GetElapsedSeconds(PlantedAtTicks) - Data.growthDurationSeconds;
            bool perfectTiming = secondsSinceReady <= Data.growthDurationSeconds * 2f;
            if (perfectTiming) score++;

            if (score >= 4) return QualityGrade.Premium;
            if (score == 3) return QualityGrade.Excellent;
            if (score == 2) return QualityGrade.Good;
            return QualityGrade.Normal;
        }

        public static float QualityPriceMultiplier(QualityGrade grade)
        {
            switch (grade)
            {
                case QualityGrade.Good: return 1.3f;
                case QualityGrade.Excellent: return 1.7f;
                case QualityGrade.Premium: return 2.2f;
                default: return 1f;
            }
        }

        public static float QualityYieldMultiplier(QualityGrade grade)
        {
            switch (grade)
            {
                case QualityGrade.Good: return 1.2f;
                case QualityGrade.Excellent: return 1.4f;
                case QualityGrade.Premium: return 1.6f;
                default: return 1f;
            }
        }
    }
}
