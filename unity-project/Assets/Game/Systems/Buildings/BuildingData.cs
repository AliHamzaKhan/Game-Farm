using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmQuest.Systems.Buildings
{
    [Serializable]
    public class BuildingLevel
    {
        public string levelName = "Small Farmhouse";
        public int cost; // cost to reach this level from the previous (0 = free/start)
        [TextArea] public string description = "";
    }

    /// <summary>Data-driven building (§24): house, barn… levels with costs.</summary>
    [CreateAssetMenu(fileName = "Building_New", menuName = "FarmQuest/Building Data")]
    public class BuildingData : ScriptableObject
    {
        public string buildingId = "house";
        public string displayName = "Farmhouse";
        [Tooltip("Level 1 is the starting state; cost = price to UPGRADE to this level.")]
        public List<BuildingLevel> levels = new List<BuildingLevel>();
    }
}
