using System.Threading.Tasks;

namespace FarmQuest.Systems.CloudSave
{
    public struct CloudUploadResult
    {
        public bool Success;
        public string Error;
    }

    public struct CloudDownloadResult
    {
        public bool Success;
        public bool Found;
        public string Json;
        public long UpdatedAtTicks;
        public string Error;
    }

    /// <summary>
    /// Cloud save provider seam (§43): local-first always; the cloud is a
    /// backup/sync convenience, never required for play.
    /// </summary>
    public interface ICloudSaveProvider
    {
        Task<CloudUploadResult> UploadAsync(string userId, string json, long updatedAtTicks);
        Task<CloudDownloadResult> DownloadAsync(string userId);
    }

    /// <summary>Offline/test provider: pretends the cloud is unreachable.</summary>
    public class OfflineCloudSaveProvider : ICloudSaveProvider
    {
        public Task<CloudUploadResult> UploadAsync(string userId, string json, long updatedAtTicks) =>
            Task.FromResult(new CloudUploadResult { Success = false, Error = "offline" });

        public Task<CloudDownloadResult> DownloadAsync(string userId) =>
            Task.FromResult(new CloudDownloadResult { Success = false, Error = "offline" });
    }
}
