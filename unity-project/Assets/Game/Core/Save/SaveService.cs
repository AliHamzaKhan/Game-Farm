using System;
using System.IO;
using UnityEngine;

namespace FarmQuest.Core.Save
{
    /// <summary>
    /// Versioned JSON save/load (§41). Never loses progress on minor updates:
    /// old saves pass through SaveMigrator before use.
    /// </summary>
    public class SaveService
    {
        public const int CurrentVersion = 1;
        private const string FileName = "farmquest_save.json";

        private readonly string _path;

        public SaveService()
        {
            _path = Path.Combine(Application.persistentDataPath, FileName);
        }

        // Test seam: allow injecting a path.
        public SaveService(string path) { _path = path; }

        public bool HasSave() => File.Exists(_path);

        public void Save(SaveData data)
        {
            try
            {
                data.version = CurrentVersion;
                data.savedAtTicks = DateTime.UtcNow.Ticks;
                File.WriteAllText(_path, JsonUtility.ToJson(data, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Save failed: {e.Message}");
            }
        }

        public bool TryLoad(out SaveData data)
        {
            data = null;
            try
            {
                if (!File.Exists(_path)) return false;
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(_path));
                if (data == null) return false;
                data = SaveMigrator.Migrate(data);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Load failed (starting fresh): {e.Message}");
                data = null;
                return false;
            }
        }

        public void DeleteSave()
        {
            try { if (File.Exists(_path)) File.Delete(_path); }
            catch (Exception e) { Debug.LogError($"[SaveService] Delete failed: {e.Message}"); }
        }
    }
}
