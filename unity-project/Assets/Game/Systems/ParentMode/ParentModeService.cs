using FarmQuest.Core.Save;
using FarmQuest.Core.Services;
using UnityEngine;

namespace FarmQuest.Systems.ParentMode
{
    /// <summary>
    /// Parent gate challenge: a simple arithmetic question a young child is
    /// unlikely to answer but any parent can. New question every attempt.
    /// </summary>
    public class ParentGateChallenge
    {
        public string Question;
        public int[] Options = new int[3];
        public int CorrectIndex;
    }

    /// <summary>
    /// LOGIC: parent mode (§43). Gate + settings. Settings:
    /// ads/analytics toggles and an optional daily playtime limit.
    /// </summary>
    public class ParentModeService
    {
        public bool AdsAllowed { get; set; } = true;
        public bool AnalyticsAllowed { get; set; } = true;
        /// <summary>0 = no limit.</summary>
        public int MaxDailyPlayMinutes { get; set; } = 0;

        private readonly System.Random _rng = new System.Random();

        public ParentGateChallenge GenerateChallenge()
        {
            int a = _rng.Next(11, 30);
            int b = _rng.Next(11, 30);
            int answer = a + b;
            var challenge = new ParentGateChallenge
            {
                Question = $"To continue, solve: {a} + {b} = ?"
            };
            int correctSlot = _rng.Next(3);
            challenge.CorrectIndex = correctSlot;
            var used = new System.Collections.Generic.HashSet<int> { answer };
            for (int i = 0; i < 3; i++)
            {
                if (i == correctSlot) { challenge.Options[i] = answer; continue; }
                int wrong;
                do { wrong = answer + _rng.Next(-9, 10); }
                while (wrong <= 0 || !used.Add(wrong));
                challenge.Options[i] = wrong;
            }
            return challenge;
        }

        public bool VerifyAnswer(ParentGateChallenge challenge, int pickedIndex) =>
            challenge != null && pickedIndex == challenge.CorrectIndex;

        // ---------- save ----------
        public ParentSettingsSave CaptureState()
        {
            return new ParentSettingsSave
            {
                adsAllowed = AdsAllowed,
                analyticsAllowed = AnalyticsAllowed,
                maxDailyPlayMinutes = MaxDailyPlayMinutes
            };
        }

        public void RestoreState(ParentSettingsSave data)
        {
            if (data == null) return;
            AdsAllowed = data.adsAllowed;
            AnalyticsAllowed = data.analyticsAllowed;
            MaxDailyPlayMinutes = data.maxDailyPlayMinutes;
        }
    }
}
