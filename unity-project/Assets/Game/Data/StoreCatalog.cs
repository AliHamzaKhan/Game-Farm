using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmQuest.Data
{
    [Serializable]
    public class ItemGrant
    {
        public string itemId;
        public int count;
    }

    /// <summary>Everything a bundle, login day or challenge can give. Shared so all rewards read the same.</summary>
    [Serializable]
    public class RewardContents
    {
        public long coins;
        public long gems;
        public List<ItemGrant> items = new List<ItemGrant>();
        [Tooltip("Decoration ids added to the player's decor inventory.")]
        public List<string> decorIds = new List<string>();
        [Tooltip("Equipment ids granted (owned permanently).")]
        public List<string> equipmentIds = new List<string>();

        public bool IsEmpty => coins <= 0 && gems <= 0 && items.Count == 0 && decorIds.Count == 0 && equipmentIds.Count == 0;
    }

    /// <summary>A real-money product. The price shown in game always comes from the store at runtime.</summary>
    [Serializable]
    public class StoreBundle
    {
        [Tooltip("Store product id (Google Play / App Store), e.g. com.ahkstudios.farmquest.gems_100.")]
        public string productId = "gems_100";
        public string title = "Gem Pack";
        [TextArea] public string description = "";
        [Tooltip("Fallback label when the store hasn't returned a localized price (illustrative only).")]
        public string fallbackPriceLabel = "$1.99";
        public bool consumable = true;
        [Tooltip("Only shown once the player reaches this country on the world tour (empty = always).")]
        public string requiredCountryId = "";
        public RewardContents contents = new RewardContents();
    }

    public enum ChallengeType { HarvestCrops, FulfillOrders, EarnFromSales, CareActions, CompleteCities, DeliverToCity }

    [Serializable]
    public class WeeklyChallengeDef
    {
        public string challengeId = "harvest_200";
        public string title = "Harvest 200 crops";
        public ChallengeType type;
        public long target = 200;
        public RewardContents reward = new RewardContents();
    }

    /// <summary>
    /// Free-to-play catalog (data-driven): real-money bundles, the 7-day login
    /// calendar, weekly challenges and the out-of-seeds safety net. Item, seed,
    /// equipment and decor prices live on their own data assets.
    /// </summary>
    [CreateAssetMenu(fileName = "StoreCatalog", menuName = "FarmQuest/Store Catalog")]
    public class StoreCatalog : ScriptableObject
    {
        public List<StoreBundle> bundles = new List<StoreBundle>();

        [Header("Daily login calendar (cycles; missed days pause the streak, never reset it)")]
        public List<RewardContents> loginCalendar = new List<RewardContents>();
        [Tooltip("Free seeds of a current-city crop added to every daily claim.")]
        public int dailyFreeSeeds = 3;

        [Header("Weekly challenges (reset Monday 00:00 UTC)")]
        public List<WeeklyChallengeDef> weeklyChallenges = new List<WeeklyChallengeDef>();

        [Header("Safety net: a player who is completely out gets free seeds")]
        public int rescueSeeds = 5;

        public StoreBundle GetBundle(string productId)
        {
            foreach (var b in bundles) if (b != null && b.productId == productId) return b;
            return null;
        }
    }
}
