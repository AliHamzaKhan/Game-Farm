using UnityEngine;

namespace FarmQuest.Systems.Decor
{
    /// <summary>Data-driven decoration (§25 GDD / §17 spec). Purely creative.</summary>
    [CreateAssetMenu(fileName = "Decor_New", menuName = "FarmQuest/Decor Data")]
    public class DecorData : ScriptableObject
    {
        public string decorId = "fence";
        public string displayName = "Wooden Fence";
        public string category = "Fences";
        public int unlockLevel = 8;
        public int cost = 50;
        [Tooltip("Gem price; 0 = coins only. cost 0 + gemPrice > 0 = premium cosmetic.")]
        public int gemPrice;
        [Tooltip("Country id that unlocks it on the world tour (empty = always).")]
        public string requiredCountryId = "";
        public Sprite icon;
        public GameObject prefab;
    }
}
