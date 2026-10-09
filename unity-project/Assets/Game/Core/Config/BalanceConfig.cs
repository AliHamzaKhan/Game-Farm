using UnityEngine;

namespace FarmQuest.Core.Config
{
    /// <summary>
    /// Remote-config seam (§47): all tunable balance lives here (or in data
    /// assets), never hardcoded. IRemoteConfigProvider lets a backend override
    /// these later without a client update.
    /// </summary>
    public interface IRemoteConfigProvider
    {
        int MaxRewardedAdsPerDay { get; }
        float RewardedAdCooldownSeconds { get; }
        float MaxTimeReductionPerDaySeconds { get; }
        float CropAdReductionSeconds { get; }
        float ProductionAdReductionSeconds { get; }

        // Phase 6: generic key-value seam for backend-driven tuning.
        string GetString(string key, string defaultValue);
        int GetInt(string key, int defaultValue);
        float GetFloat(string key, float defaultValue);
        bool GetBool(string key, bool defaultValue);
    }

    [CreateAssetMenu(fileName = "BalanceConfig", menuName = "FarmQuest/Balance Config")]
    public class BalanceConfig : ScriptableObject, IRemoteConfigProvider
    {
        [Header("Rewarded ads (§12) — conservative, kid-safe defaults")]
        public int maxRewardedAdsPerDay = 5;
        public float rewardedAdCooldownSeconds = 300f;
        public float maxTimeReductionPerDaySeconds = 600f;
        public float cropAdReductionSeconds = 60f;
        public float productionAdReductionSeconds = 180f;
        public int maxAdsPerActivity = 2;

        int IRemoteConfigProvider.MaxRewardedAdsPerDay => maxRewardedAdsPerDay;
        float IRemoteConfigProvider.RewardedAdCooldownSeconds => rewardedAdCooldownSeconds;
        float IRemoteConfigProvider.MaxTimeReductionPerDaySeconds => maxTimeReductionPerDaySeconds;
        float IRemoteConfigProvider.CropAdReductionSeconds => cropAdReductionSeconds;
        float IRemoteConfigProvider.ProductionAdReductionSeconds => productionAdReductionSeconds;

        // Local fallback: no backend, so keys return defaults.
        public string GetString(string key, string defaultValue) => defaultValue;
        public int GetInt(string key, int defaultValue) => defaultValue;
        public float GetFloat(string key, float defaultValue) => defaultValue;
        public bool GetBool(string key, bool defaultValue) => defaultValue;
    }

    /// <summary>Local fallback until a backend remote-config exists (§43/§47).</summary>
    public class LocalRemoteConfigProvider : IRemoteConfigProvider
    {
        private readonly BalanceConfig _local;
        public LocalRemoteConfigProvider(BalanceConfig local) { _local = local; }
        public int MaxRewardedAdsPerDay => _local.maxRewardedAdsPerDay;
        public float RewardedAdCooldownSeconds => _local.rewardedAdCooldownSeconds;
        public float MaxTimeReductionPerDaySeconds => _local.maxTimeReductionPerDaySeconds;
        public float CropAdReductionSeconds => _local.cropAdReductionSeconds;
        public float ProductionAdReductionSeconds => _local.productionAdReductionSeconds;
        public string GetString(string key, string defaultValue) => _local.GetString(key, defaultValue);
        public int GetInt(string key, int defaultValue) => _local.GetInt(key, defaultValue);
        public float GetFloat(string key, float defaultValue) => _local.GetFloat(key, defaultValue);
        public bool GetBool(string key, bool defaultValue) => _local.GetBool(key, defaultValue);
    }

    /// <summary>
    /// HTTP remote config (§47): fetches key-value overrides from the cloud
    /// server (/config). Missing keys / failures fall back to defaults.
    /// </summary>
    public class HttpRemoteConfigProvider : IRemoteConfigProvider
    {
        private readonly IRemoteConfigProvider _fallback;
        private readonly System.Collections.Generic.Dictionary<string, object> _overrides =
            new System.Collections.Generic.Dictionary<string, object>();
        private bool _fetched;

        public HttpRemoteConfigProvider(IRemoteConfigProvider fallback) { _fallback = fallback; }

        public int MaxRewardedAdsPerDay => GetInt("max_rewarded_ads_per_day", _fallback.MaxRewardedAdsPerDay);
        public float RewardedAdCooldownSeconds => GetFloat("rewarded_ad_cooldown_seconds", _fallback.RewardedAdCooldownSeconds);
        public float MaxTimeReductionPerDaySeconds => GetFloat("max_time_reduction_per_day_seconds", _fallback.MaxTimeReductionPerDaySeconds);
        public float CropAdReductionSeconds => GetFloat("crop_ad_reduction_seconds", _fallback.CropAdReductionSeconds);
        public float ProductionAdReductionSeconds => GetFloat("production_ad_reduction_seconds", _fallback.ProductionAdReductionSeconds);

        public string GetString(string key, string defaultValue)
        {
            if (_overrides.TryGetValue(key, out var v)) return v?.ToString() ?? defaultValue;
            return defaultValue;
        }

        public int GetInt(string key, int defaultValue)
        {
            if (_overrides.TryGetValue(key, out var v) && int.TryParse(v?.ToString(), out int n)) return n;
            return defaultValue;
        }

        public float GetFloat(string key, float defaultValue)
        {
            if (_overrides.TryGetValue(key, out var v) && float.TryParse(v?.ToString(), out float f)) return f;
            return defaultValue;
        }

        public bool GetBool(string key, bool defaultValue)
        {
            if (_overrides.TryGetValue(key, out var v) && bool.TryParse(v?.ToString(), out bool b)) return b;
            return defaultValue;
        }

        /// <summary>Best-effort fetch; safe to call at boot. Never throws.</summary>
        public async System.Threading.Tasks.Task FetchAsync(string endpoint, string apiKey)
        {
            if (_fetched || string.IsNullOrEmpty(endpoint)) return;
            _fetched = true;
            try
            {
                using (var request = UnityEngine.Networking.UnityWebRequest.Get(endpoint.TrimEnd('/') + "/config"))
                {
                    if (!string.IsNullOrEmpty(apiKey))
                        request.SetRequestHeader("X-API-Key", apiKey);
                    // NOTE: UnityWebRequestAsyncOperation has no awaiter on all
                    // Unity versions — poll instead of awaiting directly.
                    var operation = request.SendWebRequest();
                    while (!operation.isDone)
                        await System.Threading.Tasks.Task.Delay(50);
                    if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success) return;
                    foreach (var kvp in ParseFlatJson(request.downloadHandler.text))
                        _overrides[kvp.Key] = kvp.Value;
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.Log($"[RemoteConfig] Fetch failed: {e.Message}");
            }
        }

        /// <summary>
        /// Minimal parser for flat JSON objects ({"key": value, ...}) with
        /// string/number/boolean values. The /config contract is controlled,
        /// so a full JSON parser is unnecessary weight.
        /// </summary>
        public static System.Collections.Generic.Dictionary<string, string> ParseFlatJson(string json)
        {
            var result = new System.Collections.Generic.Dictionary<string, string>();
            if (string.IsNullOrEmpty(json)) return result;
            json = json.Trim();
            if (!json.StartsWith("{") || !json.EndsWith("}")) return result;
            string body = json.Substring(1, json.Length - 2);
            int i = 0;
            while (i < body.Length)
            {
                // Skip whitespace/commas.
                while (i < body.Length && (char.IsWhiteSpace(body[i]) || body[i] == ',')) i++;
                if (i >= body.Length || body[i] != '"') break;
                int keyEnd = body.IndexOf('"', i + 1);
                if (keyEnd < 0) break;
                string key = body.Substring(i + 1, keyEnd - i - 1);
                int colon = body.IndexOf(':', keyEnd + 1);
                if (colon < 0) break;
                i = colon + 1;
                while (i < body.Length && char.IsWhiteSpace(body[i])) i++;
                string value;
                if (i < body.Length && body[i] == '"')
                {
                    int valEnd = body.IndexOf('"', i + 1);
                    if (valEnd < 0) break;
                    value = body.Substring(i + 1, valEnd - i - 1);
                    i = valEnd + 1;
                }
                else
                {
                    int valEnd = i;
                    while (valEnd < body.Length && body[valEnd] != ',' && body[valEnd] != '}') valEnd++;
                    value = body.Substring(i, valEnd - i).Trim();
                    i = valEnd;
                }
                result[key] = value;
            }
            return result;
        }
    }
}

