using System.Collections.Generic;
using FarmQuest.Core.Save;
using FarmQuest.Core.Services;
using FarmQuest.Systems.Animals;
using FarmQuest.Systems.Economy;

namespace FarmQuest.Systems.Decor
{
    public class PlacedDecor
    {
        public string DecorId;
        public float X; public float Z; public float RotY;
    }

    /// <summary>
    /// LOGIC: decoration shop + placement (§25). Buy → owned inventory →
    /// place on a 1m grid. Removing returns to inventory. No wrong placement.
    /// </summary>
    public class DecorService
    {
        private readonly AnimalDatabase _database;
        private readonly Dictionary<string, int> _owned = new Dictionary<string, int>();
        private readonly List<PlacedDecor> _placed = new List<PlacedDecor>();

        public IReadOnlyList<PlacedDecor> Placed => _placed;

        public DecorService(AnimalDatabase database) { _database = database; }

        public int OwnedCount(string decorId) =>
            _owned.TryGetValue(decorId, out int n) ? n : 0;

        public bool BuyDecor(string decorId)
        {
            var data = _database.GetDecor(decorId);
            if (data == null) return false;
            var progression = ServiceLocator.Get<ProgressionService>();
            if (!progression.IsLevelUnlocked(data.unlockLevel))
            {
                GameEvents.RaiseToast($"Reach level {data.unlockLevel} for {data.displayName}.");
                return false;
            }
            if (!ServiceLocator.Get<EconomyService>().TrySpend(data.cost))
            {
                GameEvents.RaiseToast($"Need {data.cost} coins.");
                return false;
            }
            _owned[decorId] = OwnedCount(decorId) + 1;
            GameEvents.RaiseDecorChanged();
            GameEvents.RaiseToast($"🛒 {data.displayName} — tap Place to put it down!");
            return true;
        }

        public bool PlaceDecor(string decorId, float x, float z, float rotY = 0f)
        {
            if (OwnedCount(decorId) <= 0) return false;
            _owned[decorId]--;
            _placed.Add(new PlacedDecor { DecorId = decorId, X = x, Z = z, RotY = rotY });
            GameEvents.RaiseDecorChanged();
            return true;
        }

        public bool RemovePlaced(PlacedDecor placed)
        {
            if (placed == null || !_placed.Remove(placed)) return false;
            _owned[placed.DecorId] = OwnedCount(placed.DecorId) + 1;
            GameEvents.RaiseDecorChanged();
            return true;
        }

        public IEnumerable<DecorData> OwnedDecorations()
        {
            foreach (var kvp in _owned)
            {
                if (kvp.Value <= 0) continue;
                var data = _database.GetDecor(kvp.Key);
                if (data != null) yield return data;
            }
        }

        // ---------- save ----------
        public DecorSaveData CaptureState()
        {
            var data = new DecorSaveData();
            foreach (var kvp in _owned)
                data.owned.Add(new DecorOwnedSave { decorId = kvp.Key, count = kvp.Value });
            foreach (var p in _placed)
                data.placed.Add(new DecorPlacedSave
                    { decorId = p.DecorId, x = p.X, z = p.Z, rotY = p.RotY });
            return data;
        }

        public void RestoreState(DecorSaveData data)
        {
            _owned.Clear();
            _placed.Clear();
            if (data == null) return;
            foreach (var o in data.owned) _owned[o.decorId] = o.count;
            foreach (var p in data.placed)
                _placed.Add(new PlacedDecor
                    { DecorId = p.decorId, X = p.x, Z = p.z, RotY = p.rotY });
        }
    }
}
