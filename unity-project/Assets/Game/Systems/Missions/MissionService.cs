using System;
using System.Collections.Generic;
using FarmQuest.Core.Save;
using FarmQuest.Core.Services;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Farming;

namespace FarmQuest.Systems.Missions
{
    public class ActiveMission
    {
        public MissionData Data;
        public int Progress;
        public bool Completed;
        public bool Claimed;
    }

    /// <summary>
    /// Daily missions (§29): 5 per day from a level-filtered pool, progress via
    /// GameEvents, claimable rewards. Day rollover regenerates. Kid-safe: missing
    /// a day just starts fresh — no streak punishment.
    /// </summary>
    public class MissionService
    {
        public const int MissionsPerDay = 5;

        private readonly List<MissionData> _pool;
        private readonly List<ActiveMission> _active = new List<ActiveMission>();
        private string _dayStamp = "";

        public IReadOnlyList<ActiveMission> Active => _active;

        public MissionService(List<MissionData> pool)
        {
            _pool = pool ?? new List<MissionData>();
            GameEvents.CropHarvested += OnCropHarvested;
            GameEvents.CropWatered += OnCropWatered;
            GameEvents.CropPlanted += cropId => AddProgress(MissionType.PlantSeeds, cropId, 1);
            GameEvents.ItemsSold += (itemId, qty) => AddProgress(MissionType.SellCrops, CropIdFromItem(itemId), qty);
            GameEvents.CoinsEarned += amount => AddProgress(MissionType.EarnCoins, null, (int)amount);
        }

        private static string CropIdFromItem(string itemId) =>
            itemId != null && itemId.StartsWith("crop_") ? itemId.Substring(5) : null;

        /// <summary>Call after load and on app focus: regenerates if it's a new day.</summary>
        public void EnsureDailyMissions()
        {
            string today = DayStamp();
            if (today == _dayStamp && _active.Count > 0) return;
            GenerateDaily(today);
        }

        private static string DayStamp() => DateTime.UtcNow.ToString("yyyy-MM-dd");

        private void GenerateDaily(string dayStamp)
        {
            _dayStamp = dayStamp;
            _active.Clear();

            int level = 1;
            if (ServiceLocator.TryGet(out ProgressionService progression))
                level = progression.Level;

            var eligible = new List<MissionData>();
            foreach (var m in _pool)
                if (m != null && m.minLevel <= level) eligible.Add(m);

            // Deterministic daily shuffle: same missions all day, new set tomorrow.
            var rng = new System.Random(dayStamp.GetHashCode());
            for (int i = eligible.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var tmp = eligible[i]; eligible[i] = eligible[j]; eligible[j] = tmp;
            }

            int count = Math.Min(MissionsPerDay, eligible.Count);
            for (int i = 0; i < count; i++)
                _active.Add(new ActiveMission { Data = eligible[i] });
            GameEvents.RaiseMissionsChanged();
        }

        private void OnCropHarvested(CropInstance crop, int qty) =>
            AddProgress(MissionType.HarvestCrops, crop != null ? crop.Data.cropId : null, qty);

        private void OnCropWatered() => AddProgress(MissionType.WaterCrops, null, 1);

        private void AddProgress(MissionType type, string cropId, int amount)
        {
            if (amount <= 0) return;
            bool changed = false;
            foreach (var mission in _active)
            {
                if (mission.Completed || mission.Data.type != type) continue;
                if (!string.IsNullOrEmpty(mission.Data.targetCropId) &&
                    mission.Data.targetCropId != cropId) continue;
                mission.Progress = Math.Min(mission.Data.targetCount, mission.Progress + amount);
                changed = true;
                if (mission.Progress >= mission.Data.targetCount && !mission.Completed)
                {
                    mission.Completed = true;
                    GameEvents.RaiseMissionCompleted(mission.Data.missionId);
                    GameEvents.RaiseToast($"✅ Mission complete: {mission.Data.GetTitle()}");
                }
            }
            if (changed) GameEvents.RaiseMissionsChanged();
        }

        public bool Claim(string missionId)
        {
            foreach (var mission in _active)
            {
                if (mission.Data.missionId != missionId) continue;
                if (!mission.Completed || mission.Claimed) return false;
                mission.Claimed = true;
                var economy = ServiceLocator.Get<EconomyService>();
                economy.AddCoins(mission.Data.rewardCoins, "mission");
                economy.AddXp(mission.Data.rewardXp, "mission");
                GameEvents.RaiseMissionClaimed(missionId);
                GameEvents.RaiseToast($"🎁 +{mission.Data.rewardCoins} coins, +{mission.Data.rewardXp} XP!");
                return true;
            }
            return false;
        }

        // ---------- save ----------
        public MissionSaveData CaptureState()
        {
            var data = new MissionSaveData { dayStamp = _dayStamp };
            foreach (var m in _active)
                data.entries.Add(new MissionEntrySave
                {
                    missionId = m.Data.missionId,
                    progress = m.Progress,
                    completed = m.Completed,
                    claimed = m.Claimed
                });
            return data;
        }

        public void RestoreState(MissionSaveData data)
        {
            _active.Clear();
            if (data == null) { EnsureDailyMissions(); return; }
            _dayStamp = data.dayStamp ?? "";
            var map = new Dictionary<string, MissionData>();
            foreach (var m in _pool) if (m != null) map[m.missionId] = m;
            foreach (var e in data.entries)
                if (map.TryGetValue(e.missionId, out var missionData))
                    _active.Add(new ActiveMission
                    {
                        Data = missionData,
                        Progress = e.progress,
                        Completed = e.completed,
                        Claimed = e.claimed
                    });
            EnsureDailyMissions(); // regenerates if the stamp is stale
        }
    }
}
