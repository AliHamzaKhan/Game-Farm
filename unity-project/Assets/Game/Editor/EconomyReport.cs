using System.IO;
using System.Text;
using FarmQuest.Data;
using FarmQuest.Systems.Animals;
using FarmQuest.Systems.Machines;
using UnityEditor;
using UnityEngine;

namespace FarmQuest.Editor
{
    /// <summary>
    /// Writes ECONOMY_TABLE.md from the generated data assets, so the balance
    /// table always matches the game: price (coins/gems), sell value, benefit,
    /// unlock requirement, city availability and free/premium sources.
    /// Menu: FarmQuest → Generate → Economy Report.
    /// </summary>
    public static class EconomyReport
    {
        private const string Root = "Assets/Resources/Data";

        [MenuItem("FarmQuest/Generate/Economy Report")]
        public static void Write()
        {
            var crops = AssetDatabase.LoadAssetAtPath<CropDatabase>($"{Root}/CropDatabase.asset");
            var machines = AssetDatabase.LoadAssetAtPath<MachineDatabase>($"{Root}/MachineDatabase.asset");
            var animals = AssetDatabase.LoadAssetAtPath<AnimalDatabase>($"{Root}/AnimalDatabase.asset");
            var catalog = AssetDatabase.LoadAssetAtPath<StoreCatalog>($"{Root}/StoreCatalog.asset");
            if (crops == null) { Debug.LogError("[FarmQuest] Run Generate → All Game Data first."); return; }

            var sb = new StringBuilder();
            sb.AppendLine("# FarmQuest Economy Table (generated)");
            sb.AppendLine();
            sb.AppendLine("Generated from `Assets/Resources/Data` by FarmQuest → Generate → Economy Report. Do not edit by hand.");
            sb.AppendLine();
            sb.AppendLine("## Crops");
            sb.AppendLine("| Crop | Seed (coins) | Seed (gems/pack) | Sell each | Yield | Growth | Coins/min per plot | Unlock | Care |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
            foreach (var c in crops.crops)
            {
                if (c == null) continue;
                float avgYield = (c.harvestMin + c.harvestMax) / 2f;
                float perMin = (avgYield * c.sellPrice - (c.isPremium ? 0 : c.seedPrice)) / Mathf.Max(1f, c.growthDurationSeconds / 60f);
                string care = (c.waterIntervalSeconds > 0 ? $"water/{c.waterIntervalSeconds:0}s " : "water ") +
                              (c.needsFertilizer ? "fert " : "") +
                              (c.requiredNutrient != NutrientType.None ? c.requiredNutrient + " " : "") +
                              (c.needsPruning ? "prune " : "") + (c.pestChance > 0 ? $"pests {c.pestChance:P0}" : "");
                sb.AppendLine($"| {c.displayName}{(c.isPremium ? " (premium)" : "")} | {(c.isPremium ? "—" : c.seedPrice.ToString())} | " +
                              $"{(c.isPremium ? $"{c.gemSeedPrice}/{c.premiumSeedPack}" : "—")} | {c.sellPrice} | {c.harvestMin}-{c.harvestMax} | " +
                              $"{c.growthDurationSeconds / 60f:0.#} min | {perMin:0.#} | {(c.isPremium ? "gems / rewards" : $"Lv {c.unlockLevel} or city")} | {care} |");
            }
            sb.AppendLine();
            sb.AppendLine("## Supplies & consumables");
            sb.AppendLine("| Item | Tier | Coins | Gems | Benefit | From level (city #) |");
            sb.AppendLine("|---|---|---|---|---|---|");
            foreach (var i in crops.items)
            {
                if (i == null || i.category == ItemCategory.Seed || i.category == ItemCategory.Crop) continue;
                if (i.buyPrice <= 0 && i.gemPrice <= 0) continue;
                sb.AppendLine($"| {i.displayName} | {i.tier} | {(i.buyPrice > 0 ? i.buyPrice.ToString() : "—")} | " +
                              $"{(i.gemPrice > 0 ? i.gemPrice.ToString() : "—")} | {i.benefit} | {i.unlockCityIndex + 1} |");
            }
            if (machines != null)
            {
                sb.AppendLine();
                sb.AppendLine("## Equipment (permanent)");
                sb.AppendLine("| Machine | Coins | Gems | Player level | Benefit |");
                sb.AppendLine("|---|---|---|---|---|");
                foreach (var e in machines.equipment)
                    if (e != null)
                        sb.AppendLine($"| {e.displayName} | {e.cost} | {(e.gemPrice > 0 ? e.gemPrice.ToString() : "—")} | {e.unlockLevel} | {e.description} |");
            }
            if (animals != null)
            {
                sb.AppendLine();
                sb.AppendLine("## Decorations (cosmetic only)");
                sb.AppendLine("| Decoration | Coins | Gems | Needs country |");
                sb.AppendLine("|---|---|---|---|");
                foreach (var d in animals.decorations)
                    if (d != null)
                        sb.AppendLine($"| {d.displayName} | {(d.cost > 0 ? d.cost.ToString() : "—")} | {(d.gemPrice > 0 ? d.gemPrice.ToString() : "—")} | {(string.IsNullOrEmpty(d.requiredCountryId) ? "—" : d.requiredCountryId)} |");
            }
            if (catalog != null)
            {
                sb.AppendLine();
                sb.AppendLine("## Store bundles (illustrative prices; the store's localized price is shown in game)");
                sb.AppendLine("| Bundle | Price | Contents | Type |");
                sb.AppendLine("|---|---|---|---|");
                foreach (var b in catalog.bundles)
                    sb.AppendLine($"| {b.title} | {b.fallbackPriceLabel} | {Describe(b.contents)} | {(b.consumable ? "repeatable" : "one-time")} |");
                sb.AppendLine();
                sb.AppendLine("## Free rewards");
                for (int i = 0; i < catalog.loginCalendar.Count; i++)
                    sb.AppendLine($"- Login day {i + 1}: {Describe(catalog.loginCalendar[i])} + {catalog.dailyFreeSeeds} seeds");
                foreach (var w in catalog.weeklyChallenges)
                    sb.AppendLine($"- Weekly: {w.title} → {Describe(w.reward)}");
                sb.AppendLine($"- Safety net: {catalog.rescueSeeds} free seeds/day when completely out");
            }
            File.WriteAllText(Path.Combine(Application.dataPath, "../ECONOMY_TABLE.md"), sb.ToString());
            Debug.Log("[FarmQuest] Wrote unity-project/ECONOMY_TABLE.md");
        }

        private static string Describe(RewardContents r)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (r.coins > 0) parts.Add($"{r.coins:N0} coins");
            if (r.gems > 0) parts.Add($"{r.gems} gems");
            foreach (var i in r.items) parts.Add($"{i.count} {i.itemId}");
            foreach (var d in r.decorIds) parts.Add(d);
            foreach (var e in r.equipmentIds) parts.Add(e);
            return string.Join(", ", parts);
        }
    }
}
