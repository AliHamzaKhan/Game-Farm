using System;
using System.Text;
using System.Threading.Tasks;
using FarmQuest.Core.Config;
using FarmQuest.Core.Services;
using UnityEngine;
using UnityEngine.Networking;

namespace FarmQuest.Systems.CloudSave
{
    /// <summary>
    /// HTTP cloud save provider (§43) talking to the FarmQuest cloud server
    /// (see workspace/farm-quest/cloud-server/). Endpoint + API key come from
    /// remote config; empty endpoint = disabled (local-only mode).
    /// </summary>
    public class HttpCloudSaveProvider : ICloudSaveProvider
    {
        [Serializable]
        private class UploadPayload
        {
            public string save_json;
            public long updated_at;
        }

        [Serializable]
        private class DownloadPayload
        {
            public string save_json;
            public long updated_at;
        }

        private string Endpoint =>
            ServiceLocator.Get<IRemoteConfigProvider>().GetString("cloud_save_endpoint", "");

        private string ApiKey =>
            ServiceLocator.Get<IRemoteConfigProvider>().GetString("cloud_save_api_key", "");

        private string UserId =>
            ServiceLocator.Get<IRemoteConfigProvider>().GetString("cloud_user_id", "local");

        private bool IsConfigured() => !string.IsNullOrEmpty(Endpoint);

        private void AddAuth(UnityWebRequest request)
        {
            if (!string.IsNullOrEmpty(ApiKey))
                request.SetRequestHeader("X-API-Key", ApiKey);
        }

        public async Task<CloudUploadResult> UploadAsync(string userId, string json, long updatedAtTicks)
        {
            if (!IsConfigured())
                return new CloudUploadResult { Success = false, Error = "not configured" };
            try
            {
                var payload = JsonUtility.ToJson(new UploadPayload
                {
                    save_json = json,
                    updated_at = updatedAtTicks
                });
                string url = $"{Endpoint.TrimEnd('/')}/saves/{Uri.EscapeDataString(UserId)}";
                using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT))
                {
                    byte[] body = Encoding.UTF8.GetBytes(payload);
                    request.uploadHandler = new UploadHandlerRaw(body);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    AddAuth(request);
                    // NOTE: UnityWebRequestAsyncOperation has no awaiter on all
                    // Unity versions — poll instead of awaiting directly.
                    var operation = request.SendWebRequest();
                    while (!operation.isDone)
                        await Task.Delay(50);
                    if (request.result != UnityWebRequest.Result.Success)
                        return new CloudUploadResult { Success = false, Error = request.error };
                    return new CloudUploadResult { Success = true };
                }
            }
            catch (Exception e)
            {
                return new CloudUploadResult { Success = false, Error = e.Message };
            }
        }

        public async Task<CloudDownloadResult> DownloadAsync(string userId)
        {
            if (!IsConfigured())
                return new CloudDownloadResult { Success = false, Error = "not configured" };
            try
            {
                string url = $"{Endpoint.TrimEnd('/')}/saves/{Uri.EscapeDataString(UserId)}";
                using (var request = UnityWebRequest.Get(url))
                {
                    AddAuth(request);
                    var operation = request.SendWebRequest();
                    while (!operation.isDone)
                        await Task.Delay(50);
                    if (request.responseCode == 404)
                        return new CloudDownloadResult { Success = true, Found = false };
                    if (request.result != UnityWebRequest.Result.Success)
                        return new CloudDownloadResult { Success = false, Error = request.error };
                    var payload = JsonUtility.FromJson<DownloadPayload>(request.downloadHandler.text);
                    return new CloudDownloadResult
                    {
                        Success = true,
                        Found = true,
                        Json = payload.save_json,
                        UpdatedAtTicks = payload.updated_at
                    };
                }
            }
            catch (Exception e)
            {
                return new CloudDownloadResult { Success = false, Error = e.Message };
            }
        }
    }
}
