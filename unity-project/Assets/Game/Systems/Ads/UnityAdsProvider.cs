using System;
using UnityEngine;

namespace FarmQuest.Systems.Ads
{
    /// <summary>
    /// Real rewarded-ads provider via Unity Ads (§46, Phase 6).
    ///
    /// SETUP (in the Unity editor, once):
    ///   1. Window → Package Manager → "Advertisement" (com.unity.ads) → Install.
    ///   2. Edit → Project Settings → Services → Ads → link your Unity project
    ///      and enable Test Mode while developing.
    ///   3. Add the scripting define FARMQUEST_UNITY_ADS:
    ///      Project Settings → Player → Scripting Define Symbols.
    ///   4. In Bootstrapper, register this provider instead of StubAdProvider
    ///      (see the commented line) and set your placement id below.
    ///
    /// Without the define/package, this file compiles to a safe fallback that
    /// reports "not ready" — the game always works without ads.
    /// </summary>
    public class UnityAdsProvider : IAdProvider
    {
        /// <summary>Your rewarded placement id from the Unity dashboard.</summary>
        public string PlacementId = "Rewarded_Android";

        public bool IsReady
        {
            get
            {
#if FARMQUEST_UNITY_ADS
                return UnityEngine.Advertisements.Advertisement.isInitialized
                    && UnityEngine.Advertisements.Advertisement.IsReady(PlacementId);
#else
                return false;
#endif
            }
        }

        public void Show(string placement, Action<bool> onComplete)
        {
#if FARMQUEST_UNITY_ADS
            var options = new UnityEngine.Advertisements.ShowOptions
            {
                resultCallback = result =>
                {
                    bool completed = result == UnityEngine.Advertisements.ShowResult.Finished;
                    Debug.Log($"[Ads] Unity Ads result: {result}");
                    onComplete?.Invoke(completed);
                }
            };
            UnityEngine.Advertisements.Advertisement.Show(
                string.IsNullOrEmpty(placement) ? PlacementId : placement, options);
#else
            Debug.LogWarning("[Ads] Unity Ads not configured (FARMQUEST_UNITY_ADS undefined).");
            onComplete?.Invoke(false);
#endif
        }

#if FARMQUEST_UNITY_ADS
        /// <summary>Call once at boot with your Unity Game ID.</summary>
        public static void Initialize(string gameId, bool testMode)
        {
            UnityEngine.Advertisements.Advertisement.Initialize(gameId, testMode);
        }
#endif
    }
}
