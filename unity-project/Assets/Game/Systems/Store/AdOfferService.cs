using System;
using System.Collections.Generic;
using FarmQuest.Core.Config;
using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Ads;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Farming;
using FarmQuest.Systems.Inventory;
using UnityEngine;

namespace FarmQuest.Systems.Store
{
    /// <summary>One optional ad offer shown at a natural moment. Declining costs nothing.</summary>
    public class AdOffer
    {
        public string Placement;
        public string Message;
        internal Action Grant;
    }

    /// <summary>
    /// LOGIC: offers rewarded ads at natural moments — a good harvest, a
    /// delivery, a claimed mission, running out of a supply. Never on every
    /// plant/harvest: each placement has its own cooldown, there is a global gap
    /// between offers, and the daily ad cap / parent switch still apply. Rewards
    /// are small extras (+50% coins, an extra harvest, a few supplies) so ads
    /// shorten progress without replacing the farming economy.
    /// </summary>
    public class AdOfferService
    {
        public const int MaxDoubledHarvest = 20;

        private readonly RewardedAdService _ads;
        private readonly IRemoteConfigProvider _config;
        private readonly Dictionary<string, float> _lastOffer = new Dictionary<string, float>();
        private float _lastAnyOffer = -99999f;

        /// <summary>Raised when an offer should be shown; the UI calls Accept or Decline.</summary>
        public event Action<AdOffer> OfferAvailable;
        public AdOffer Current { get; private set; }

        /// <summary>Clock for cooldowns (overridable in tests).</summary>
        public Func<float> Clock = () => Time.realtimeSinceStartup;

        public AdOfferService(RewardedAdService ads, IRemoteConfigProvider config)
        {
            _ads = ads;
            _config = config;
            GameEvents.CropHarvested += OnHarvest;
            GameEvents.OrderFulfilled += OnOrder;
            GameEvents.MissionRewarded += OnMission;
            GameEvents.SupplyMissing += OnSupplyMissing;
        }

        private float Cooldown(string placement, float fallback) =>
            _config != null ? _config.GetFloat($"ad_offer_cooldown_{placement}", fallback) : fallback;

        private float GlobalGap => _config != null ? _config.GetFloat("ad_offer_global_gap_seconds", 90f) : 90f;

        private void TryOffer(string placement, float cooldown, string message, Action grant)
        {
            if (_ads == null || Current != null || !_ads.IsRewardedAdAvailable()) return;
            float now = Clock();
            if (now - _lastAnyOffer < GlobalGap) return;
            if (_lastOffer.TryGetValue(placement, out float last) && now - last < Cooldown(placement, cooldown)) return;
            _lastOffer[placement] = now;
            _lastAnyOffer = now;
            Current = new AdOffer { Placement = placement, Message = message, Grant = grant };
            OfferAvailable?.Invoke(Current);
        }

        public void Accept(Action<bool> onDone = null)
        {
            var offer = Current;
            Current = null;
            if (offer == null) { onDone?.Invoke(false); return; }
            _ads.ShowAd(offer.Placement, completed =>
            {
                if (completed) offer.Grant?.Invoke(); // reward only on confirmed completion
                onDone?.Invoke(completed);
            });
        }

        public void Decline() => Current = null;

        private void OnHarvest(CropInstance crop, int qty)
        {
            if (crop?.Data == null || qty < 4) return;
            int extra = Math.Min(qty, MaxDoubledHarvest);
            string id = crop.Data.cropId;
            TryOffer("double_harvest", 300f, $"Great harvest! Watch an ad for {extra} more {crop.Data.displayName}?",
                () => ServiceLocator.Get<InventoryService>().Add("crop_" + id, extra));
        }

        private void OnOrder(long coins)
        {
            long bonus = coins / 2;
            if (bonus <= 0) return;
            TryOffer("delivery_bonus", 300f, $"Delivery done! Watch an ad for a +{bonus} coin bonus?",
                () => ServiceLocator.Get<EconomyService>().AddCoins(bonus, "ad_delivery_bonus"));
        }

        private void OnMission(string missionId, long coins)
        {
            long bonus = coins / 2;
            if (bonus <= 0) return;
            TryOffer("mission_bonus", 300f, $"Mission complete! Watch an ad for +{bonus} bonus coins?",
                () => ServiceLocator.Get<EconomyService>().AddCoins(bonus, "ad_mission_bonus"));
        }

        private void OnSupplyMissing(string itemId)
        {
            if (itemId != "fertilizer" && itemId != "compost" && itemId != CropCareService.PesticideItemId) return;
            var item = ServiceLocator.TryGet(out CropDatabase db) ? db.GetItem(itemId) : null;
            string name = item != null ? item.displayName : itemId;
            TryOffer("free_supply", 600f, $"Out of {name}. Watch an ad for 2 free? (Or buy it in the Market.)",
                () => ServiceLocator.Get<InventoryService>().Add(itemId, 2));
        }
    }
}
