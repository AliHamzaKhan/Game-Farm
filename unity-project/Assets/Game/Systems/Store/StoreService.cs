using System;
using System.Collections.Generic;
using FarmQuest.Core.Save;
using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Inventory;
using FarmQuest.Systems.World;
using UnityEngine;

namespace FarmQuest.Systems.Store
{
    public struct PurchaseResult
    {
        public bool success;
        public string productId;
        /// <summary>Store transaction id; used so one purchase is never credited twice.</summary>
        public string transactionId;
        public string error;
    }

    /// <summary>
    /// Real-money store seam (Unity IAP / Google Play Billing / StoreKit behind it).
    /// Implementations must only report success for a completed, validated purchase.
    /// </summary>
    public interface IIapProvider
    {
        bool IsAvailable { get; }
        /// <summary>Localized price from the store, or null when unknown.</summary>
        string GetPriceLabel(string productId);
        void Purchase(string productId, Action<PurchaseResult> onResult);
    }

    /// <summary>
    /// Development provider. With simulateSuccess (editor) purchases complete
    /// instantly with a fake transaction id; otherwise the store is unavailable,
    /// so builds without a real provider can never grant paid items.
    /// </summary>
    public class StubIapProvider : IIapProvider
    {
        private readonly bool _simulateSuccess;
        public StubIapProvider(bool simulateSuccess) { _simulateSuccess = simulateSuccess; }
        public bool IsAvailable => _simulateSuccess;
        public string GetPriceLabel(string productId) => null;
        public void Purchase(string productId, Action<PurchaseResult> onResult)
        {
            onResult?.Invoke(_simulateSuccess
                ? new PurchaseResult { success = true, productId = productId, transactionId = "stub_" + Guid.NewGuid().ToString("N") }
                : new PurchaseResult { success = false, productId = productId, error = "Store not available" });
        }
    }

    /// <summary>
    /// LOGIC: the FarmQuest Store and gem shop.
    /// - Real-money bundles: parents must pass the parent gate in the UI first;
    ///   contents are granted only on provider-confirmed success, once per transaction.
    /// - Gem shop: premium seeds, advanced/premium consumables, gem decor/equipment.
    /// Regular seeds and every item needed for city contracts stay on Farm Coins.
    /// </summary>
    public class StoreService
    {
        private readonly StoreCatalog _catalog;
        private readonly IIapProvider _iap;
        private readonly HashSet<string> _processedTransactions = new HashSet<string>();
        private readonly HashSet<string> _ownedNonConsumables = new HashSet<string>();
        private bool _purchasing;

        public StoreService(StoreCatalog catalog, IIapProvider iap)
        {
            _catalog = catalog;
            _iap = iap;
        }

        public StoreCatalog Catalog => _catalog;
        public bool IsPurchasing => _purchasing;

        public bool PurchasesAllowed =>
            !ServiceLocator.TryGet(out ParentMode.ParentModeService parents) || parents.PurchasesAllowed;

        public bool IsStoreAvailable => _iap != null && _iap.IsAvailable && PurchasesAllowed;

        public IEnumerable<StoreBundle> VisibleBundles()
        {
            if (_catalog == null) yield break;
            foreach (var b in _catalog.bundles)
            {
                if (b == null) continue;
                if (!b.consumable && _ownedNonConsumables.Contains(b.productId)) continue;
                if (!IsCountryReached(b.requiredCountryId)) continue;
                yield return b;
            }
        }

        public string PriceLabel(StoreBundle bundle) =>
            _iap?.GetPriceLabel(bundle.productId) ?? bundle.fallbackPriceLabel;

        /// <summary>
        /// Starts a real-money purchase. Call only after the parent gate passed.
        /// onDone(true) when the contents were granted.
        /// </summary>
        public void BuyBundle(string productId, Action<bool> onDone)
        {
            var bundle = _catalog != null ? _catalog.GetBundle(productId) : null;
            if (bundle == null || !IsStoreAvailable || _purchasing) { onDone?.Invoke(false); return; }
            _purchasing = true;
            try
            {
                _iap.Purchase(productId, result =>
                {
                    _purchasing = false;
                    onDone?.Invoke(ProcessPurchase(result));
                });
            }
            catch (Exception e)
            {
                _purchasing = false;
                Debug.LogWarning($"[Store] Purchase failed: {e.Message}");
                onDone?.Invoke(false);
            }
        }

        /// <summary>
        /// Grants a confirmed purchase. Safe to call again for the same transaction
        /// (store restores / duplicate callbacks): it is credited only once.
        /// </summary>
        public bool ProcessPurchase(PurchaseResult result)
        {
            if (!result.success || string.IsNullOrEmpty(result.transactionId)) return false;
            if (!_processedTransactions.Add(result.transactionId)) return false;
            var bundle = _catalog != null ? _catalog.GetBundle(result.productId) : null;
            if (bundle == null) return false;
            Rewards.Grant(bundle.contents, "iap:" + bundle.productId);
            if (!bundle.consumable) _ownedNonConsumables.Add(bundle.productId);
            GameEvents.RaiseToast($"Thank you! {bundle.title} added.");
            GameEvents.RaiseStoreChanged();
            // Persist immediately so a crash can't lose (or replay) a paid purchase.
            if (ServiceLocator.TryGet(out SaveCoordinator coordinator)) coordinator.SaveAll();
            return true;
        }

        // ---------- gem shop ----------

        public static bool IsCountryReached(string countryId)
        {
            if (string.IsNullOrEmpty(countryId)) return true;
            if (!ServiceLocator.TryGet(out WorldProgressionService world) || world.Current == null) return false;
            for (int i = 0; i <= world.CurrentCityIndex && i < world.AuthoredCityCount; i++)
            {
                var level = world.GetCityLevel(i);
                if (level != null && level.Country.countryId == countryId) return true;
            }
            return world.Current.Lap > 0; // second lap and later: everything reached
        }

        public static bool IsItemAvailable(ItemData item)
        {
            if (item == null) return false;
            if (!ServiceLocator.TryGet(out WorldProgressionService world)) return item.unlockCityIndex <= 0;
            return world.CurrentCityIndex >= item.unlockCityIndex;
        }

        public bool BuyItemWithGems(string itemId, int quantity = 1)
        {
            var item = ServiceLocator.Get<CropDatabase>().GetItem(itemId);
            if (item == null || item.gemPrice <= 0 || quantity <= 0 || !IsItemAvailable(item)) return false;
            long cost = (long)item.gemPrice * quantity;
            if (!ServiceLocator.Get<EconomyService>().TrySpendGems(cost))
            {
                GameEvents.RaiseToast($"Need {cost} gems.");
                return false;
            }
            ServiceLocator.Get<InventoryService>().Add(itemId, quantity);
            GameEvents.RaiseToast($"+{quantity} {item.displayName}");
            GameEvents.RaiseStoreChanged();
            return true;
        }

        /// <summary>Premium seeds come in packs and cost gems (also free from login day 7 and challenges).</summary>
        public bool BuyPremiumSeeds(string cropId)
        {
            var db = ServiceLocator.Get<CropDatabase>();
            var crop = db.GetCrop(cropId);
            if (crop == null || !crop.isPremium || crop.gemSeedPrice <= 0) return false;
            var seedItem = db.GetItem("seed_" + cropId);
            if (seedItem != null && !IsItemAvailable(seedItem)) return false; // no item data = no city requirement
            if (!ServiceLocator.Get<EconomyService>().TrySpendGems(crop.gemSeedPrice))
            {
                GameEvents.RaiseToast($"Need {crop.gemSeedPrice} gems.");
                return false;
            }
            int pack = Mathf.Max(1, crop.premiumSeedPack);
            ServiceLocator.Get<InventoryService>().Add("seed_" + cropId, pack);
            GameEvents.RaiseToast($"+{pack} {crop.displayName} seeds!");
            GameEvents.RaiseStoreChanged();
            return true;
        }

        // ---------- save ----------
        public StoreSaveData CaptureState() => new StoreSaveData
        {
            processedTransactions = new List<string>(_processedTransactions),
            ownedNonConsumables = new List<string>(_ownedNonConsumables)
        };

        public void RestoreState(StoreSaveData data)
        {
            _processedTransactions.Clear();
            _ownedNonConsumables.Clear();
            if (data == null) return;
            if (data.processedTransactions != null)
                foreach (var t in data.processedTransactions) if (!string.IsNullOrEmpty(t)) _processedTransactions.Add(t);
            if (data.ownedNonConsumables != null)
                foreach (var p in data.ownedNonConsumables) if (!string.IsNullOrEmpty(p)) _ownedNonConsumables.Add(p);
        }
    }

    /// <summary>Applies RewardContents to the player's wallet, bag, decor and equipment.</summary>
    public static class Rewards
    {
        public static void Grant(RewardContents r, string reason, int multiplier = 1)
        {
            if (r == null) return;
            multiplier = Math.Max(1, multiplier);
            var economy = ServiceLocator.Get<EconomyService>();
            economy.AddCoins(r.coins * multiplier, reason);
            economy.AddGems(r.gems * multiplier, reason);
            if (r.items != null && ServiceLocator.TryGet(out InventoryService inventory))
                foreach (var i in r.items)
                    if (!string.IsNullOrEmpty(i.itemId) && i.count > 0) inventory.Add(i.itemId, i.count * multiplier);
            if (r.decorIds != null && ServiceLocator.TryGet(out Decor.DecorService decor))
                foreach (var d in r.decorIds) decor.Grant(d);
            if (r.equipmentIds != null && ServiceLocator.TryGet(out Machines.EquipmentService equipment))
                foreach (var e in r.equipmentIds) equipment.Grant(e);
        }

        public static string Describe(RewardContents r, CropDatabase db, int multiplier = 1)
        {
            if (r == null) return "";
            var parts = new List<string>();
            if (r.coins > 0) parts.Add($"{r.coins * multiplier:N0} coins");
            if (r.gems > 0) parts.Add($"{r.gems * multiplier} gems");
            if (r.items != null)
                foreach (var i in r.items)
                {
                    var item = db != null ? db.GetItem(i.itemId) : null;
                    parts.Add($"{i.count * multiplier} {(item != null ? item.displayName : i.itemId)}");
                }
            if (r.decorIds != null && r.decorIds.Count > 0) parts.Add($"{r.decorIds.Count} decoration(s)");
            if (r.equipmentIds != null && r.equipmentIds.Count > 0) parts.Add($"{r.equipmentIds.Count} machine(s)");
            return string.Join(", ", parts);
        }
    }
}
