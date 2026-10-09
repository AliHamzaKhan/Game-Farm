using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmQuest.Systems.Village
{
    [Serializable]
    public class ShopEntry
    {
        public string itemId;
        [Tooltip("Override price; -1 = use the item's buyPrice.")]
        public int priceOverride = -1;
    }

    /// <summary>Village shop (§20): NPC-run, simple open hours.</summary>
    [CreateAssetMenu(fileName = "Shop_New", menuName = "FarmQuest/Shop Data")]
    public class ShopData : ScriptableObject
    {
        public string shopId = "seed_store";
        public string displayName = "Seed Store";
        public string shopkeeperName = "Rafiq";
        [Range(0, 23)] public int openHour = 9;
        [Range(0, 23)] public int closeHour = 18;
        public List<ShopEntry> items = new List<ShopEntry>();

        public bool IsOpenNow()
        {
            int h = DateTime.Now.Hour;
            return h >= openHour && h < closeHour;
        }
    }
}
