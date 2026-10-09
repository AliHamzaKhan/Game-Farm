using System;
using FarmQuest.Core.Save;
using FarmQuest.Core.Time;

namespace FarmQuest.Systems.Orchard
{
    /// <summary>One planted tree: fruit regrows on a timer (perennials vs annuals lesson).</summary>
    public class TreeInstance
    {
        public TreeData Data { get; }
        public int SpotIndex { get; }

        private long _lastHarvestedAtTicks;
        private readonly ITimeService _time;

        public TreeInstance(TreeData data, int spotIndex, ITimeService time)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            SpotIndex = spotIndex;
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _lastHarvestedAtTicks = _time.UtcNow.Ticks;
        }

        public bool FruitReady =>
            _time.GetElapsedSeconds(_lastHarvestedAtTicks) >= Data.regrowIntervalSeconds;

        public float RegrowProgress =>
            UnityEngine.Mathf.Clamp01(
                (float)(_time.GetElapsedSeconds(_lastHarvestedAtTicks) / Data.regrowIntervalSeconds));

        /// <summary>Harvests if ready. Returns fruit count (0 if not ready).</summary>
        public int Harvest(Func<int> rollQuantity)
        {
            if (!FruitReady) return 0;
            _lastHarvestedAtTicks = _time.UtcNow.Ticks;
            return Math.Max(1, rollQuantity());
        }

        // ---------- save ----------
        public TreeSaveEntry CaptureState()
        {
            return new TreeSaveEntry
            {
                treeId = Data.treeId,
                spotIndex = SpotIndex,
                lastHarvestedAtTicks = _lastHarvestedAtTicks
            };
        }

        public void RestoreState(TreeSaveEntry entry)
        {
            _lastHarvestedAtTicks = entry.lastHarvestedAtTicks;
        }
    }
}
