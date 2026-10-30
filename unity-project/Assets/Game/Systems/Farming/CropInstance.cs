using System;
using System.Collections.Generic;
using FarmQuest.Core.Time;
using FarmQuest.Data;
using UnityEngine;

namespace FarmQuest.Systems.Farming
{
    /// <summary>Snapshot of a crop's care state, used by save/restore.</summary>
    [Serializable]
    public struct CropCareState
    {
        public int wateringsDone;
        public long lastWateredTicks;
        public double dryPenaltySeconds;
        public double bonusGrowthSeconds;
        public bool compostApplied;
        public bool fertilized;
        public bool specialNutrientApplied;
        public bool pruned;
        public float pestAtProgress;
        public bool pestSprayed;
        public float bonusYieldMultiplier;
    }

    /// <summary>
    /// LOGIC+DATA: one planted crop (§7, §8). Everything derives from stored
    /// timestamps and ITimeService, so growth, drying out and pests all resolve
    /// correctly after the app was closed — no per-crop timers (§9).
    ///
    /// Growth runs on "effective seconds": wall time since planting, minus time
    /// lost while dry, plus time granted by rewarded ads / boosts.
    /// </summary>
    public class CropInstance
    {
        public const int LifecycleStages = 6;

        public CropData Data { get; }
        public long PlantedAtTicks { get; private set; }
        public int WateringsDone { get; private set; }
        public bool CompostApplied { get; private set; }
        /// <summary>Fertilizer applied (historic name kept for saves and quality).</summary>
        public bool NutrientApplied { get; private set; }
        public bool SpecialNutrientApplied { get; private set; }
        public bool Pruned { get; private set; }
        /// <summary>Progress (0-1) at which a pest attack starts; -1 = no pests this growth.</summary>
        public float PestAtProgress { get; private set; } = -1f;
        public bool PestSprayed { get; private set; }
        /// <summary>Last watering time (UTC ticks); 0 = never watered.</summary>
        public long LastWateredTicks { get; private set; }
        /// <summary>Growth seconds already lost to dryness (settled at each watering).</summary>
        public double DryPenaltySeconds { get; private set; }
        /// <summary>Growth seconds granted by ads/boosts.</summary>
        public double BonusGrowthSeconds { get; private set; }
        /// <summary>Extra yield from ads/upgrades (1 = none).</summary>
        public float BonusYieldMultiplier { get; set; } = 1f;
        public bool ReadyNotified { get; set; }

        private readonly ITimeService _time;

        public CropInstance(CropData data, ITimeService time, long plantedAtTicks)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            _time = time ?? throw new ArgumentNullException(nameof(time));
            PlantedAtTicks = plantedAtTicks;
        }

        /// <summary>
        /// Growth time multiplier (Phase 5 seasons, upgrades): 1 = normal,
        /// 2 = half speed. Never blocks planting — kid-friendly.
        /// </summary>
        public float GrowthTimeMultiplier { get; set; } = 1f;

        public double DurationSeconds => Math.Max(0.001, Data.growthDurationSeconds * GrowthTimeMultiplier);

        private double WallSeconds => _time.GetElapsedSeconds(PlantedAtTicks);

        private bool DriesOut => Data.waterIntervalSeconds > 0f;
        private double Penalty => Mathf.Clamp(Data.dryGrowthPenalty, 0f, 0.9f);

        /// <summary>Wall seconds after planting at which the crop is (or became) dry.</summary>
        private double DryStartWall
        {
            get
            {
                if (LastWateredTicks == 0) return 0; // needs its first watering
                double wateredAt = (LastWateredTicks - PlantedAtTicks) / (double)TimeSpan.TicksPerSecond;
                return Math.Max(0, wateredAt) + Data.waterIntervalSeconds;
            }
        }

        /// <summary>Dry penalty accruing since the last watering, frozen once the crop is ready.</summary>
        private double OngoingDryPenalty(double wall)
        {
            if (!DriesOut || Penalty <= 0) return 0;
            double dryStart = DryStartWall;
            if (wall <= dryStart) return 0;
            double settled = DryPenaltySeconds - BonusGrowthSeconds;
            double effAtDryStart = dryStart - settled;
            double readyWall = effAtDryStart >= DurationSeconds
                ? dryStart // already ready before drying out
                : dryStart + (DurationSeconds - effAtDryStart) / (1 - Penalty);
            return Penalty * (Math.Min(wall, readyWall) - dryStart);
        }

        public double EffectiveSeconds
        {
            get
            {
                double wall = WallSeconds;
                return Math.Max(0, wall - DryPenaltySeconds - OngoingDryPenalty(wall) + BonusGrowthSeconds);
            }
        }

        public float Progress => Mathf.Clamp01((float)(EffectiveSeconds / DurationSeconds));

        /// <summary>Growth seconds left, assuming the crop is cared for.</summary>
        public float RemainingSeconds => Mathf.Max(0f, (float)(DurationSeconds - EffectiveSeconds));

        public bool IsReady => EffectiveSeconds >= DurationSeconds;

        // ---------- lifecycle stages ----------

        private float Weight(int stage)
        {
            var w = Data.stageWeights;
            if (w == null || w.Length < LifecycleStages - 1) return 1f;
            return Mathf.Max(0.01f, w[stage]);
        }

        /// <summary>Progress (0-1) at which a growing stage ends. Mature ends at 1.</summary>
        public float StageEndProgress(GrowthStage stage)
        {
            if (stage >= GrowthStage.Fruiting) return 1f;
            float total = 0f, upTo = 0f;
            for (int i = 0; i < LifecycleStages - 1; i++)
            {
                total += Weight(i);
                if (i <= (int)stage) upTo += Weight(i);
            }
            return upTo / total;
        }

        public GrowthStage Stage
        {
            get
            {
                if (IsReady) return GrowthStage.Mature;
                float p = Progress;
                for (int i = 0; i < LifecycleStages - 1; i++)
                    if (p < StageEndProgress((GrowthStage)i)) return (GrowthStage)i;
                return GrowthStage.Fruiting;
            }
        }

        /// <summary>Stage index 0-5 (views/stage prefabs).</summary>
        public int CurrentStage => (int)Stage;

        /// <summary>Growth seconds until the current stage ends.</summary>
        public float SecondsToNextStage =>
            Mathf.Max(0f, (float)(StageEndProgress(Stage) * DurationSeconds - EffectiveSeconds));

        // ---------- care needs ----------

        public bool NeedsWater
        {
            get
            {
                if (IsReady) return false;
                if (DriesOut) return WallSeconds >= DryStartWall;
                return WateringsDone < Data.waterRequired;
            }
        }

        public bool NeedsFertilizer =>
            !IsReady && Data.needsFertilizer && !NutrientApplied && Stage >= GrowthStage.Vegetative;

        public bool HasNutrientDeficiency =>
            !IsReady && Data.requiredNutrient != NutrientType.None && !SpecialNutrientApplied
            && Stage >= GrowthStage.Flowering;

        public bool HasPests => PestAtProgress >= 0f && !PestSprayed && Progress >= PestAtProgress;

        public bool CanPrune =>
            Data.needsPruning && !Pruned &&
            (Stage == GrowthStage.Vegetative || Stage == GrowthStage.Flowering);

        /// <summary>Single most important state, for icons and the plot panel title.</summary>
        public CropHealth Health
        {
            get
            {
                if (HasPests) return CropHealth.PestAttack; // even on a ripe crop: spray before harvest
                if (IsReady) return CropHealth.ReadyToHarvest;
                if (NeedsWater) return CropHealth.NeedsWater;
                if (NeedsFertilizer) return CropHealth.NeedsFertilizer;
                if (HasNutrientDeficiency) return CropHealth.NutrientDeficiency;
                if (CanPrune) return CropHealth.NeedsPruning;
                return CropHealth.Healthy;
            }
        }

        /// <summary>Every outstanding need, for the plot panel checklist.</summary>
        public List<CropHealth> CurrentNeeds()
        {
            var list = new List<CropHealth>();
            if (HasPests) list.Add(CropHealth.PestAttack);
            if (NeedsWater) list.Add(CropHealth.NeedsWater);
            if (NeedsFertilizer) list.Add(CropHealth.NeedsFertilizer);
            if (HasNutrientDeficiency) list.Add(CropHealth.NutrientDeficiency);
            if (CanPrune) list.Add(CropHealth.NeedsPruning);
            return list;
        }

        // ---------- care actions ----------

        public void Water()
        {
            if (IsReady) return;
            SettleDryPenalty();
            WateringsDone++;
            LastWateredTicks = _time.UtcNow.Ticks;
        }

        private void SettleDryPenalty()
        {
            DryPenaltySeconds += OngoingDryPenalty(WallSeconds);
        }

        public void ApplyCompost() => CompostApplied = true;
        /// <summary>Applies fertilizer.</summary>
        public void ApplyNutrient() => NutrientApplied = true;
        public void ApplySpecialNutrient() => SpecialNutrientApplied = true;
        public void Prune() { if (CanPrune) Pruned = true; }
        public void Spray() { if (PestAtProgress >= 0f) PestSprayed = true; }

        /// <summary>Schedules this growth's pest attack (FarmService rolls the chance).</summary>
        public void SchedulePest(float atProgress) => PestAtProgress = Mathf.Clamp(atProgress, 0.05f, 0.95f);

        /// <summary>Rain counts as a watering (§15).</summary>
        public void ApplyRain()
        {
            if (IsReady) return;
            if (DriesOut || WateringsDone < Data.waterRequired) Water();
        }

        /// <summary>Time acceleration (§11, rewarded ads): grants growth seconds.</summary>
        public void ReduceRemainingTime(float seconds)
        {
            BonusGrowthSeconds += Mathf.Max(0f, seconds);
        }

        // ---------- harvest ----------

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
            double secondsSinceReady = EffectiveSeconds - DurationSeconds;
            bool perfectTiming = secondsSinceReady <= DurationSeconds * 2f;
            if (perfectTiming) score++;
            if (HasPests) score--;

            if (score >= 4) return QualityGrade.Premium;
            if (score == 3) return QualityGrade.Excellent;
            if (score == 2) return QualityGrade.Good;
            return QualityGrade.Normal;
        }

        /// <summary>Yield effect of the care this crop received (pests, fertilizer, nutrient, pruning, boosts).</summary>
        public float CareYieldMultiplier
        {
            get
            {
                float m = 1f;
                if (HasPests) m *= 0.6f;
                if (Data.needsFertilizer && !NutrientApplied) m *= 0.8f;
                if (Data.requiredNutrient != NutrientType.None) m *= SpecialNutrientApplied ? 1.15f : 0.9f;
                if (Data.needsPruning && Pruned) m *= 1.25f;
                return m * Mathf.Max(0f, BonusYieldMultiplier);
            }
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

        // ---------- save ----------

        public CropCareState CaptureCare() => new CropCareState
        {
            wateringsDone = WateringsDone,
            lastWateredTicks = LastWateredTicks,
            dryPenaltySeconds = DryPenaltySeconds,
            bonusGrowthSeconds = BonusGrowthSeconds,
            compostApplied = CompostApplied,
            fertilized = NutrientApplied,
            specialNutrientApplied = SpecialNutrientApplied,
            pruned = Pruned,
            pestAtProgress = PestAtProgress,
            pestSprayed = PestSprayed,
            bonusYieldMultiplier = BonusYieldMultiplier
        };

        public void RestoreCare(CropCareState s)
        {
            WateringsDone = s.wateringsDone;
            LastWateredTicks = s.lastWateredTicks;
            DryPenaltySeconds = Math.Max(0, s.dryPenaltySeconds);
            BonusGrowthSeconds = Math.Max(0, s.bonusGrowthSeconds);
            CompostApplied = s.compostApplied;
            NutrientApplied = s.fertilized;
            SpecialNutrientApplied = s.specialNutrientApplied;
            Pruned = s.pruned;
            PestAtProgress = s.pestAtProgress;
            PestSprayed = s.pestSprayed;
            BonusYieldMultiplier = s.bonusYieldMultiplier > 0f ? s.bonusYieldMultiplier : 1f;
        }

        public static string NutrientItemId(NutrientType type) =>
            type == NutrientType.None ? null : "nutrient_" + type.ToString().ToLowerInvariant();
    }
}
