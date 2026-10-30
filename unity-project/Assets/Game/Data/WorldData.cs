using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmQuest.Data
{
    public enum CityObjectiveType
    {
        HarvestCrop,   // harvest N of a crop (itemId = cropId, e.g. "tomato")
        DeliverItem,   // hand N items to the city contract (itemId = inventory item, e.g. "crop_wheat")
        EarnFromSales, // earn N coins by selling at the market (itemId unused)
        SellItem       // sell N of an item at the market (itemId = inventory item)
    }

    [Serializable]
    public class CityObjective
    {
        public CityObjectiveType type;
        public string itemId = "";
        public long target = 10;
        [Tooltip("Optional label; generated from type/item when empty.")]
        public string label = "";
    }

    [Serializable]
    public class CityPriceModifier
    {
        public string itemId = "crop_tomato";
        [Tooltip("Sell price multiplier in this city's market (1.3 = pays 30% more).")]
        public float sellMultiplier = 1f;
    }

    [Serializable]
    public class CityRewardItem
    {
        public string itemId;
        public int count;
    }

    /// <summary>One city = one farming level.</summary>
    [Serializable]
    public class CityDef
    {
        public string cityId = "karachi";
        public string displayName = "Karachi";
        [TextArea] public string description = "";
        public List<CityObjective> objectives = new List<CityObjective>();
        [Tooltip("Crops this city introduces (seeds become available even below their unlock level).")]
        public List<string> introducedCropIds = new List<string>();
        public List<CityPriceModifier> prices = new List<CityPriceModifier>();
        public long rewardCoins = 500;
        public int rewardXp = 100;
        [Tooltip("Free premium currency for finishing the city.")]
        public int rewardGems = 2;
        public List<CityRewardItem> rewardItems = new List<CityRewardItem>();
    }

    [Serializable]
    public class CountryDef
    {
        public string countryId = "pakistan";
        public string displayName = "Pakistan";
        public List<CityDef> cities = new List<CityDef>();
        public long completionBonusCoins = 2000;
        public int completionBonusXp = 500;
        public int completionBonusGems = 10;
    }

    /// <summary>
    /// The world tour (data-driven): countries in order, each with cities in
    /// order. Add countries/cities here — no code changes. When every authored
    /// city is done, the tour repeats with scaled targets and rewards ("laps"),
    /// so progression never runs out.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldData", menuName = "FarmQuest/World Data")]
    public class WorldData : ScriptableObject
    {
        public List<CountryDef> countries = new List<CountryDef>();

        [Header("Endless laps after the last authored city")]
        [Tooltip("Objective targets grow by this fraction per extra lap (0.5 = +50%).")]
        public float lapTargetGrowth = 0.5f;
        [Tooltip("Rewards grow by this fraction per extra lap.")]
        public float lapRewardGrowth = 0.6f;

        public int AuthoredCityCount
        {
            get
            {
                int n = 0;
                foreach (var c in countries) if (c != null && c.cities != null) n += c.cities.Count;
                return n;
            }
        }
    }
}
