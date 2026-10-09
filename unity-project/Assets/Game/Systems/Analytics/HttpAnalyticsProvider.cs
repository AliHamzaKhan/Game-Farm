using System;
using System.Collections.Generic;
using System.Text;
using FarmQuest.Core.Config;
using FarmQuest.Core.Services;
using UnityEngine;
using UnityEngine.Networking;

namespace FarmQuest.Systems.Analytics
{
    /// <summary>
    /// HTTP analytics provider (§46): batches events and POSTs to the cloud
    /// server (/analytics/batch). Best-effort; drops the batch on failure
    /// rather than growing memory. Privacy-safe by construction (see
    /// AnalyticsService — no identifiers are ever attached).
    /// </summary>
    public class HttpAnalyticsProvider : IAnalyticsProvider
    {
        [Serializable]
        private class EventEnvelope
        {
            public string name;
            public string props_json;
            public long ts;
        }

        [Serializable]
        private class Batch
        {
            public List<EventEnvelope> events = new List<EventEnvelope>();
        }

        private const int MaxBatch = 50;
        private readonly List<EventEnvelope> _queue = new List<EventEnvelope>();

        public void Track(string eventName, Dictionary<string, string> properties)
        {
            var sb = new StringBuilder("{");
            bool first = true;
            if (properties != null)
                foreach (var kvp in properties)
                {
                    if (!first) sb.Append(",");
                    first = false;
                    sb.Append($"\"{kvp.Key}\":\"{kvp.Value}\"");
                }
            sb.Append("}");
            _queue.Add(new EventEnvelope
            {
                name = eventName,
                props_json = sb.ToString(),
                ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });
            if (_queue.Count >= MaxBatch) Flush();
        }

        public async void Flush()
        {
            if (_queue.Count == 0) return;
            var config = ServiceLocator.Get<IRemoteConfigProvider>();
            string endpoint = config.GetString("cloud_save_endpoint", "");
            if (string.IsNullOrEmpty(endpoint) ||
                Application.internetReachability == NetworkReachability.NotReachable)
            {
                _queue.Clear(); // offline: drop rather than hoard
                return;
            }
            var batch = new Batch();
            batch.events.AddRange(_queue);
            _queue.Clear();
            try
            {
                string body = JsonUtility.ToJson(batch);
                string url = endpoint.TrimEnd('/') + "/analytics/batch";
                using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    string apiKey = config.GetString("cloud_save_api_key", "");
                    if (!string.IsNullOrEmpty(apiKey))
                        request.SetRequestHeader("X-API-Key", apiKey);
                    // NOTE: UnityWebRequestAsyncOperation has no awaiter on all
                    // Unity versions — poll instead of awaiting directly.
                    var operation = request.SendWebRequest();
                    while (!operation.isDone)
                        await System.Threading.Tasks.Task.Delay(50);
                    if (request.result != UnityWebRequest.Result.Success)
                        Debug.Log($"[Analytics] Upload failed: {request.error}");
                }
            }
            catch (Exception e)
            {
                Debug.Log($"[Analytics] Upload failed: {e.Message}");
            }
        }
    }
}
