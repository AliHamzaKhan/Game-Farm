using FarmQuest.Core.Save;
using FarmQuest.Core.Services;
using FarmQuest.Core.Time;

namespace FarmQuest.Systems.ParentMode
{
    /// <summary>
    /// LOGIC: playtime tracking (§43). Session + daily totals; daily resets on
    /// DayChanged. Gentle reminders only — never a hard lockout.
    /// </summary>
    public class PlaytimeService
    {
        public float SessionSeconds { get; private set; }
        public float DailySeconds { get; private set; }

        private float _lastReminderAt;

        public PlaytimeService(ITimeService time)
        {
            time.SecondTick += OnSecondTick;
            time.DayChanged += OnDayChanged;
        }

        private void OnSecondTick()
        {
            SessionSeconds += 1f;
            DailySeconds += 1f;
            CheckLimitReminder();
        }

        private void OnDayChanged()
        {
            DailySeconds = 0f;
            _lastReminderAt = 0f;
        }

        private void CheckLimitReminder()
        {
            if (!ServiceLocator.TryGet(out ParentModeService parents)) return;
            int limitMinutes = parents.MaxDailyPlayMinutes;
            if (limitMinutes <= 0) return;
            float limitSeconds = limitMinutes * 60f;
            if (DailySeconds >= limitSeconds && DailySeconds - _lastReminderAt >= 900f)
            {
                _lastReminderAt = DailySeconds;
                GameEvents.RaiseToast("🌙 You've played a while — maybe time for a break?");
            }
        }

        public bool IsOverLimit()
        {
            if (!ServiceLocator.TryGet(out ParentModeService parents)) return false;
            int limitMinutes = parents.MaxDailyPlayMinutes;
            return limitMinutes > 0 && DailySeconds >= limitMinutes * 60f;
        }

        public string FormatDaily() =>
            $"{(int)(DailySeconds / 60)} min today";

        // ---------- save ----------
        public PlaytimeSaveData CaptureState()
        {
            return new PlaytimeSaveData
            {
                dailySeconds = DailySeconds,
                dayStamp = System.DateTime.UtcNow.ToString("yyyy-MM-dd")
            };
        }

        public void RestoreState(PlaytimeSaveData data)
        {
            if (data == null) return;
            // Only keep the daily total if the save is from today.
            if (data.dayStamp == System.DateTime.UtcNow.ToString("yyyy-MM-dd"))
                DailySeconds = data.dailySeconds;
        }
    }
}
