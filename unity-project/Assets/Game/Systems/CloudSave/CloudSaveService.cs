using System.Threading.Tasks;
using FarmQuest.Core.Services;
using UnityEngine;

namespace FarmQuest.Systems.CloudSave
{
    /// <summary>
    /// LOGIC: cloud save orchestration (§43). Local-first: the local save is
    /// authoritative for play; the cloud is a backup. On boot, the newer of
    /// local/cloud wins (latest timestamp). Failures are silent — the game
    /// never blocks on the network.
    /// </summary>
    public class CloudSaveService
    {
        private readonly ICloudSaveProvider _provider;

        public CloudSaveService(ICloudSaveProvider provider) { _provider = provider; }

        private static bool IsOnline() =>
            Application.internetReachability != NetworkReachability.NotReachable;

        /// <summary>Best-effort upload after a local save. Never throws.</summary>
        public async Task UploadSaveAsync(string userId, string json, long updatedAtTicks)
        {
            if (!IsOnline()) return;
            try
            {
                var result = await _provider.UploadAsync(userId, json, updatedAtTicks);
                if (!result.Success)
                    Debug.Log($"[CloudSave] Upload skipped: {result.Error}");
            }
            catch (System.Exception e)
            {
                Debug.Log($"[CloudSave] Upload failed: {e.Message}");
            }
        }

        /// <summary>
        /// Returns cloud JSON if a NEWER cloud save exists, else null.
        /// Never throws; null = keep local.
        /// </summary>
        public async Task<string> DownloadNewerSaveAsync(string userId, long localUpdatedAtTicks)
        {
            if (!IsOnline()) return null;
            try
            {
                var result = await _provider.DownloadAsync(userId);
                if (!result.Success || !result.Found) return null;
                if (result.UpdatedAtTicks > localUpdatedAtTicks)
                {
                    Debug.Log("[CloudSave] Newer cloud save found — using it.");
                    return result.Json;
                }
                return null;
            }
            catch (System.Exception e)
            {
                Debug.Log($"[CloudSave] Download failed: {e.Message}");
                return null;
            }
        }
    }
}
