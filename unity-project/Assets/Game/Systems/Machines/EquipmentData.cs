using UnityEngine;

namespace FarmQuest.Systems.Machines
{
    public enum EquipmentType
    {
        Tractor,    // drivable vehicle; defines work radius + speed
        Seeder,     // unlocks tractor Plant mode
        Sprayer,    // unlocks tractor Fertilize mode
        Harvester,  // unlocks tractor Harvest mode
        Planter,    // auto-planter: widens Plant mode radius
        Irrigation  // auto-waters crops on a timer
    }

    /// <summary>Data-driven equipment (§23, tiers from GDD §13).</summary>
    [CreateAssetMenu(fileName = "Equipment_New", menuName = "FarmQuest/Equipment Data")]
    public class EquipmentData : ScriptableObject
    {
        public string equipmentId = "small_tractor";
        public string displayName = "Small Tractor";
        public EquipmentType type = EquipmentType.Tractor;
        public int unlockLevel = 15;
        public int cost = 2500;
        [TextArea] public string description = "Plows a 3x3 area as you drive.";

        [Header("Tractor stats")]
        public int workRadius = 1;       // 1 = 3x3, 2 = 5x5, 3 = 7x7
        public float speedMultiplier = 1f;

        [Header("Irrigation stats")]
        public float waterIntervalSeconds = 90f;
        public int waterCount = 8;       // crops per cycle (999 = all)

        public Sprite icon;
        public GameObject prefab;
    }
}
