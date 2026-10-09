using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmQuest.Data
{
    [Serializable]
    public class FarmRegion
    {
        public int regionId;
        public string regionName = "Home Farm";
        public RectInt area = new RectInt(0, 0, 4, 4);
        public int unlockLevel = 1;
        public long unlockCost;
        [Tooltip("Region 0 is unlocked from the start.")]
        public bool unlockedByDefault;
    }

    /// <summary>Data-driven farm layout (§6): grid regions and unlock rules.</summary>
    [CreateAssetMenu(fileName = "FarmLayout", menuName = "FarmQuest/Farm Layout")]
    public class FarmLayoutData : ScriptableObject
    {
        public int gridWidth = 16;
        public int gridHeight = 16;
        public float tileSize = 2f;
        public List<FarmRegion> regions = new List<FarmRegion>();
    }
}
