using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Ads;
using FarmQuest.Systems.Store;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>
    /// Daily rewards panel: 7-day login calendar (free seeds every day, optional
    /// ad to double) and this week's challenges. All rewards are free.
    /// </summary>
    public class DailyPanelController : MonoBehaviour
    {
        public Transform listContent;
        public GameObject rowPrefab;
        public Text headerText;
        public Button closeButton;

        private void Awake() => closeButton?.onClick.AddListener(() => gameObject.SetActive(false));

        private void OnEnable()
        {
            GameEvents.StoreChanged += Rebuild;
            Rebuild();
        }

        private void OnDisable() => GameEvents.StoreChanged -= Rebuild;

        private void Row(string name, string price, string button = null, UnityEngine.Events.UnityAction action = null) =>
            RowList.Add(listContent, rowPrefab, name, price, button, action);

        private void Rebuild()
        {
            if (!isActiveAndEnabled) return;
            RowList.Clear(listContent);
            if (!ServiceLocator.TryGet(out DailyRewardService daily) || daily.CalendarLength == 0)
            {
                Row("Daily rewards data missing — run Generate All Game Data.", "");
                return;
            }
            var db = ServiceLocator.Get<CropDatabase>();
            if (headerText != null)
                headerText.text = "Come back every day for free gifts.\nMissed a day? Your streak just waits for you.";

            string seedCrop = daily.DailySeedCropId();
            var crop = seedCrop != null ? db.GetCrop(seedCrop) : null;
            string seeds = crop != null ? $" + {daily.DailySeedCount()} {crop.displayName} seeds" : "";
            for (int i = 0; i < daily.CalendarLength; i++)
            {
                bool today = i == daily.CalendarIndex;
                string text = $"Day {i + 1}: {Rewards.Describe(daily.Catalog.loginCalendar[i], db)}" + (today ? seeds : "");
                if (today && daily.CanClaimToday)
                    Row($"<b>{text}</b>", "Today", "CLAIM", () => daily.ClaimDaily());
                else
                    Row(text, today ? "Tomorrow" : i < daily.CalendarIndex ? "Done" : "");
            }
            if (daily.CanClaimToday && ServiceLocator.TryGet(out RewardedAdService ads) && ads.IsRewardedAdAvailable())
                Row("Optional: watch an ad to get today's gift twice", "Ad", "WATCH",
                    () => daily.ClaimDailyWithAd(ok =>
                    {
                        if (!ok) GameEvents.RaiseToast("Ad not finished — you can still claim normally.");
                    }));

            Row("<b>This week's challenges</b> (reset every Monday)", "");
            for (int i = 0; i < daily.WeeklyChallenges.Count; i++)
            {
                var c = daily.WeeklyChallenges[i];
                int idx = i;
                string reward = Rewards.Describe(c.reward, db);
                if (daily.IsWeeklyClaimed(i)) Row($"{c.title}\n   {reward}", "Done");
                else if (daily.IsWeeklyDone(i)) Row($"<b>{c.title}</b>\n   {reward}", "Ready", "CLAIM", () => daily.ClaimWeekly(idx));
                else Row($"{c.title}\n   {reward}", $"{daily.WeeklyProgress(i):N0}/{c.target:N0}");
            }
        }
    }
}
