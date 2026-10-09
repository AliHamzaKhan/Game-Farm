using System.Collections.Generic;
using UnityEngine;

namespace FarmQuest.Data
{
    /// <summary>
    /// Registry of all CropData + ItemData assets. Loaded once by Bootstrapper
    /// from Resources. Lookup by id keeps save files and services decoupled
    /// from asset references.
    /// </summary>
    [CreateAssetMenu(fileName = "CropDatabase", menuName = "FarmQuest/Crop Database")]
    public class CropDatabase : ScriptableObject
    {
        public List<CropData> crops = new List<CropData>();
        public List<ItemData> items = new List<ItemData>();

        private Dictionary<string, CropData> _cropMap;
        private Dictionary<string, ItemData> _itemMap;

        public void Initialize()
        {
            _cropMap = new Dictionary<string, CropData>();
            foreach (var c in crops)
                if (c != null && !_cropMap.ContainsKey(c.cropId)) _cropMap.Add(c.cropId, c);
            _itemMap = new Dictionary<string, ItemData>();
            foreach (var i in items)
                if (i != null && !_itemMap.ContainsKey(i.itemId)) _itemMap.Add(i.itemId, i);
        }

        public CropData GetCrop(string cropId)
        {
            if (_cropMap == null) Initialize();
            _cropMap.TryGetValue(cropId, out var data);
            return data;
        }

        public ItemData GetItem(string itemId)
        {
            if (_itemMap == null) Initialize();
            _itemMap.TryGetValue(itemId, out var data);
            return data;
        }

        public IEnumerable<CropData> GetCropsForLevel(int level)
        {
            if (_cropMap == null) Initialize();
            foreach (var c in crops)
                if (c.unlockLevel <= level) yield return c;
        }
    }
}
