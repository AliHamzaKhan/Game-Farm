using System.Collections.Generic;
using System.Linq;
using FarmQuest.Core.Save;
using FarmQuest.Core.Services;
using FarmQuest.Data;

namespace FarmQuest.Systems.Inventory
{
    /// <summary>
    /// LOGIC: scalable inventory (§18). Stacks by (itemId, quality); supports
    /// add/remove/sort/filter. Pure C#, testable.
    /// </summary>
    public class InventorySlot
    {
        public string ItemId;
        public int Count;
        public QualityGrade Quality;
    }

    public class InventoryService
    {
        private readonly CropDatabase _database;
        private readonly List<InventorySlot> _slots = new List<InventorySlot>();

        public InventoryService(CropDatabase database) { _database = database; }

        public IReadOnlyList<InventorySlot> Slots => _slots;

        public int GetCount(string itemId)
        {
            int total = 0;
            foreach (var s in _slots)
                if (s.ItemId == itemId) total += s.Count;
            return total;
        }

        public bool Add(string itemId, int count, QualityGrade quality = QualityGrade.Normal)
        {
            if (count <= 0) return false;
            var item = _database.GetItem(itemId);
            int maxStack = item != null ? item.maxStack : 99;

            int remaining = count;
            // Fill existing matching stacks first.
            foreach (var slot in _slots)
            {
                if (remaining <= 0) break;
                if (slot.ItemId == itemId && slot.Quality == quality && slot.Count < maxStack)
                {
                    int space = maxStack - slot.Count;
                    int add = System.Math.Min(space, remaining);
                    slot.Count += add;
                    remaining -= add;
                }
            }
            while (remaining > 0)
            {
                int add = System.Math.Min(maxStack, remaining);
                _slots.Add(new InventorySlot { ItemId = itemId, Count = add, Quality = quality });
                remaining -= add;
            }
            GameEvents.RaiseInventoryChanged(itemId, GetCount(itemId));
            return true;
        }

        public bool TryRemove(string itemId, int count, QualityGrade? quality = null)
        {
            if (count <= 0) return false;
            if (GetCount(itemId) < count) return false;

            int remaining = count;
            // Remove from lowest quality first (keep the good stuff).
            var ordered = _slots
                .Where(s => s.ItemId == itemId && (!quality.HasValue || s.Quality == quality.Value))
                .OrderBy(s => s.Quality).ToList();

            foreach (var slot in ordered)
            {
                if (remaining <= 0) break;
                int take = System.Math.Min(slot.Count, remaining);
                slot.Count -= take;
                remaining -= take;
            }
            _slots.RemoveAll(s => s.Count <= 0);
            GameEvents.RaiseInventoryChanged(itemId, GetCount(itemId));
            return true;
        }

        public IEnumerable<InventorySlot> GetByCategory(ItemCategory category)
        {
            foreach (var slot in _slots)
            {
                var item = _database.GetItem(slot.ItemId);
                if (item != null && item.category == category) yield return slot;
            }
        }

        public void Sort()
        {
            _slots.Sort((a, b) =>
            {
                int c = string.Compare(a.ItemId, b.ItemId, System.StringComparison.Ordinal);
                return c != 0 ? c : a.Quality.CompareTo(b.Quality);
            });
        }

        // ---------- save ----------
        public InventorySaveData CaptureState()
        {
            var data = new InventorySaveData();
            foreach (var s in _slots)
                data.slots.Add(new InventorySlotSaveData
                {
                    itemId = s.ItemId, count = s.Count, quality = (int)s.Quality
                });
            return data;
        }

        public void RestoreState(InventorySaveData data)
        {
            _slots.Clear();
            if (data == null) return;
            foreach (var s in data.slots)
                _slots.Add(new InventorySlot
                {
                    ItemId = s.itemId, Count = s.count, Quality = (QualityGrade)s.quality
                });
        }
    }
}
