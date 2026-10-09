using System;
using FarmQuest.Core.Save;
using FarmQuest.Core.Time;
using UnityEngine;

namespace FarmQuest.Systems.Animals
{
    public enum AnimalState { Hungry, Happy, Producing, Ready, Sleeping }

    /// <summary>
    /// LOGIC+DATA: one animal (§21). Mirrors CropInstance: timestamps + ITimeService,
    /// so offline time just works. Kindness rules: animals never suffer — low care
    /// only slows production.
    /// </summary>
    public class AnimalInstance
    {
        public AnimalData Data { get; }
        public string InstanceId { get; }

        private long _lastFedAtTicks;
        private long _lastCollectedAtTicks;
        private float _happiness;       // 0-100 at _happinessAtTicks
        private long _happinessAtTicks;

        public bool ReadyNotified { get; set; }

        private readonly ITimeService _time;

        public AnimalInstance(AnimalData data, ITimeService time)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            _time = time ?? throw new ArgumentNullException(nameof(time));
            InstanceId = Guid.NewGuid().ToString("N");
            long now = _time.UtcNow.Ticks;
            _lastFedAtTicks = now;
            _lastCollectedAtTicks = now;
            _happiness = 60f;
            _happinessAtTicks = now;
        }

        /// <summary>Happiness decays 5/hour without interaction — petting matters.</summary>
        public float Happiness
        {
            get
            {
                double hours = _time.GetElapsedSeconds(_happinessAtTicks) / 3600.0;
                return Mathf.Max(0f, _happiness - (float)(hours * 5.0));
            }
        }

        public bool IsHungry =>
            _time.GetElapsedSeconds(_lastFedAtTicks) > Data.feedIntervalSeconds;

        /// <summary>Happy animals produce faster; sad ones slower. Never zero.</summary>
        public float ProductionMultiplier
        {
            get
            {
                float h = Happiness;
                if (h >= 70f) return 1.2f;
                if (h < 30f) return 0.8f;
                return 1f;
            }
        }

        public float EffectiveIntervalSeconds =>
            Data.productionIntervalSeconds / ProductionMultiplier;

        public bool Produces => !string.IsNullOrEmpty(Data.productItemId);

        public int ProductsReady
        {
            get
            {
                if (!Produces) return 0;
                double elapsed = _time.GetElapsedSeconds(_lastCollectedAtTicks);
                int n = (int)(elapsed / EffectiveIntervalSeconds);
                return Math.Min(Data.maxStoredProducts, n);
            }
        }

        public AnimalState CurrentState
        {
            get
            {
                if (ProductsReady > 0) return AnimalState.Ready;
                if (IsHungry) return AnimalState.Hungry;
                if (Happiness >= 70f) return AnimalState.Happy;
                return AnimalState.Producing;
            }
        }

        public void Feed()
        {
            long now = _time.UtcNow.Ticks;
            _lastFedAtTicks = now;
            SetHappiness(Mathf.Min(100f, Happiness + 10f));
        }

        public void Pet()
        {
            SetHappiness(Mathf.Min(100f, Happiness + 25f));
        }

        private void SetHappiness(float value)
        {
            _happiness = value;
            _happinessAtTicks = _time.UtcNow.Ticks;
        }

        /// <summary>Collects ready products. Returns count collected.</summary>
        public int Collect()
        {
            int n = ProductsReady;
            if (n > 0)
            {
                _lastCollectedAtTicks = _time.UtcNow.Ticks;
                ReadyNotified = false;
                SetHappiness(Mathf.Min(100f, Happiness + 5f));
            }
            return n;
        }

        // ---------- save ----------
        public AnimalSaveEntry CaptureState()
        {
            return new AnimalSaveEntry
            {
                animalId = Data.animalId,
                instanceId = InstanceId,
                lastFedAtTicks = _lastFedAtTicks,
                lastCollectedAtTicks = _lastCollectedAtTicks,
                happiness = _happiness,
                happinessAtTicks = _happinessAtTicks
            };
        }

        public void RestoreState(AnimalSaveEntry entry)
        {
            _lastFedAtTicks = entry.lastFedAtTicks;
            _lastCollectedAtTicks = entry.lastCollectedAtTicks;
            _happiness = entry.happiness;
            _happinessAtTicks = entry.happinessAtTicks;
            ReadyNotified = false;
        }
    }
}
