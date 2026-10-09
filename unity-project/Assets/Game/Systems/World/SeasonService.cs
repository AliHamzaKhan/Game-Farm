using FarmQuest.Core.Save;
using FarmQuest.Core.Services;
using FarmQuest.Core.Time;
using FarmQuest.Data;

namespace FarmQuest.Systems.World
{
    /// <summary>
    /// Seasons (§26): 4 seasons × 30 days. Out-of-season crops grow slower
    /// (handled in FarmService via CropInstance.GrowthTimeMultiplier).
    /// Starts in Spring.
    /// </summary>
    public class SeasonService
    {
        public const int DaysPerSeason = 30;

        private static readonly Season[] Order =
            { Season.Spring, Season.Summer, Season.Autumn, Season.Winter };

        private int _daysElapsed;

        public Season CurrentSeason => Order[(_daysElapsed / DaysPerSeason) % Order.Length];

        public int DayInSeason => (_daysElapsed % DaysPerSeason) + 1;

        public SeasonService(ITimeService time)
        {
            time.DayChanged += OnDayChanged;
        }

        private void OnDayChanged()
        {
            Season before = CurrentSeason;
            _daysElapsed++;
            if (CurrentSeason != before)
            {
                GameEvents.RaiseSeasonChanged();
                GameEvents.RaiseToast(SeasonGreeting(CurrentSeason));
            }
        }

        private static string SeasonGreeting(Season season) => season switch
        {
            Season.Spring => "🌸 Spring has arrived!",
            Season.Summer => "☀️ Summer is here!",
            Season.Autumn => "🍂 Autumn leaves are falling!",
            Season.Winter => "❄️ Winter has come!",
            _ => ""
        };

        // ---------- save ----------
        public SeasonSaveData CaptureState() => new SeasonSaveData { daysElapsed = _daysElapsed };

        public void RestoreState(SeasonSaveData data)
        {
            _daysElapsed = data != null ? data.daysElapsed : 0;
        }
    }
}
