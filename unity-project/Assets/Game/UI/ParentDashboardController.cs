using FarmQuest.Core.Services;
using FarmQuest.Systems.Ads;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.ParentMode;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>
    /// Parent dashboard (§43): playtime, progress, ad/analytics toggles,
    /// daily playtime limit. Only reachable through the parent gate.
    /// </summary>
    public class ParentDashboardController : MonoBehaviour
    {
        public Text statsText;
        public Button adsToggleButton;
        public Button analyticsToggleButton;
        public Button limitDownButton;
        public Button limitUpButton;
        public Text limitText;
        public Button closeButton;

        private ParentModeService _parents;
        private PlaytimeService _playtime;

        private void Awake()
        {
            closeButton?.onClick.AddListener(() => gameObject.SetActive(false));
            adsToggleButton?.onClick.AddListener(() =>
            {
                _parents.AdsAllowed = !_parents.AdsAllowed;
                Refresh();
            });
            analyticsToggleButton?.onClick.AddListener(() =>
            {
                _parents.AnalyticsAllowed = !_parents.AnalyticsAllowed;
                Refresh();
            });
            limitDownButton?.onClick.AddListener(() => ChangeLimit(-30));
            limitUpButton?.onClick.AddListener(() => ChangeLimit(30));
        }

        private void OnEnable()
        {
            _parents = ServiceLocator.Get<ParentModeService>();
            _playtime = ServiceLocator.Get<PlaytimeService>();
            Refresh();
        }

        private void ChangeLimit(int delta)
        {
            _parents.MaxDailyPlayMinutes =
                UnityEngine.Mathf.Max(0, _parents.MaxDailyPlayMinutes + delta);
            Refresh();
        }

        private void Refresh()
        {
            if (_parents == null) return;
            var progression = ServiceLocator.Get<ProgressionService>();
            var achievements = ServiceLocator.TryGet(out Systems.Missions.AchievementService a)
                ? a.UnlockedCount : 0;

            if (statsText != null)
                statsText.text =
                    $"⏱️ Playtime today: {_playtime.FormatDaily()}\n" +
                    $"⭐ Level {progression.Level} ({progression.Xp} XP)\n" +
                    $"🏆 Achievements unlocked: {achievements}\n" +
                    $"🪙 Coins: {ServiceLocator.Get<EconomyService>().Coins}";

            SetToggle(adsToggleButton, "Rewarded ads", _parents.AdsAllowed);
            SetToggle(analyticsToggleButton, "Anonymous analytics", _parents.AnalyticsAllowed);
            if (limitText != null)
                limitText.text = _parents.MaxDailyPlayMinutes <= 0
                    ? "Daily limit: none"
                    : $"Daily limit: {_parents.MaxDailyPlayMinutes} min";
        }

        private static void SetToggle(Button button, string label, bool on)
        {
            if (button == null) return;
            var t = button.GetComponentInChildren<Text>();
            if (t != null) t.text = $"{label}: {(on ? "ON ✅" : "OFF ❌")}";
        }
    }
}
