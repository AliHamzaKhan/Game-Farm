using System.Collections.Generic;
using UnityEngine;

namespace FarmQuest.Systems.Village
{
    [CreateAssetMenu(fileName = "VillageDatabase", menuName = "FarmQuest/Village Database")]
    public class VillageDatabase : ScriptableObject
    {
        public List<ShopData> shops = new List<ShopData>();
    }
}
