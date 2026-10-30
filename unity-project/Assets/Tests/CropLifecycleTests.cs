using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Farming;
using FarmQuest.Systems.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace FarmQuest.Tests
{
    /// <summary>Six-stage lifecycle, crop-specific care, health states, offline growth.</summary>
    public class CropLifecycleTests
    {
        private FakeTimeService _time;
        private CropData _tomato;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            _time = new FakeTimeService();
            _tomato = ScriptableObject.CreateInstance<CropData>();
            _tomato.cropId = "tomato";
            _tomato.displayName = "Tomato";
            _tomato.growthDurationSeconds = 300f;
            _tomato.stageWeights = new[] { 1f, 1f, 1f, 1f, 6f }; // long fruiting stage
            _tomato.waterRequired = 1;
            _tomato.waterIntervalSeconds = 120f;
            _tomato.dryGrowthPenalty = 0.5f;
            _tomato.needsFertilizer = true;
            _tomato.requiredNutrient = NutrientType.Calcium;
            _tomato.needsPruning = true;
            _tomato.harvestMin = 10;
            _tomato.harvestMax = 10;
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Clear();
            Object.DestroyImmediate(_tomato);
        }

        private CropInstance Plant()
        {
            var crop = new CropInstance(_tomato, _time, _time.Now.Ticks);
            crop.Water();
            return crop;
        }

        /// <summary>Waters whenever needed while advancing, like an attentive player.</summary>
        private void GrowWatered(CropInstance crop, int seconds)
        {
            for (int i = 0; i < seconds; i += 10)
            {
                _time.AdvanceSeconds(10);
                if (crop.NeedsWater) crop.Water();
            }
        }

        [Test]
        public void StageWeights_ControlStageTiming()
        {
            var crop = Plant();
            Assert.AreEqual(GrowthStage.Seed, crop.Stage);
            GrowWatered(crop, 30); // 10% of 300s = end of Seed (weight 1 of 10)
            Assert.AreEqual(GrowthStage.Sprout, crop.Stage);
            GrowWatered(crop, 90); // 40% -> start of Fruiting
            Assert.AreEqual(GrowthStage.Fruiting, crop.Stage);
            GrowWatered(crop, 170);
            Assert.AreEqual(GrowthStage.Fruiting, crop.Stage, "fruiting is 60% of this crop's timer");
            GrowWatered(crop, 20);
            Assert.AreEqual(GrowthStage.Mature, crop.Stage);
            Assert.IsTrue(crop.IsReady);
        }

        [Test]
        public void NeedsWater_AfterInterval_AndDryCropGrowsSlower()
        {
            var crop = Plant();
            Assert.IsFalse(crop.NeedsWater);
            _time.AdvanceSeconds(121);
            Assert.IsTrue(crop.NeedsWater);
            Assert.AreEqual(CropHealth.NeedsWater, crop.Health);

            // 120s at full speed + 180s at half speed = 210 growth seconds, not 300.
            _time.AdvanceSeconds(179);
            Assert.IsFalse(crop.IsReady);
            Assert.AreEqual(210.0, crop.EffectiveSeconds, 0.6);

            crop.Water(); // settles the penalty; growth resumes at full speed
            Assert.IsFalse(crop.NeedsWater);
            _time.AdvanceSeconds(91);
            Assert.IsTrue(crop.IsReady);
        }

        [Test]
        public void Offline_DryCropStillFinishes_AndPenaltyStopsWhenReady()
        {
            var crop = Plant();
            _time.AdvanceSeconds(24 * 3600); // app closed for a day, never watered
            Assert.IsTrue(crop.IsReady, "kid-safe: crops never die, they only slow down");
            double eff = crop.EffectiveSeconds;
            _time.AdvanceSeconds(3600);
            Assert.Greater(crop.EffectiveSeconds, eff, "no penalty accrues after ready");
        }

        [Test]
        public void Care_NeedsAppearAtTheRightStages()
        {
            var crop = Plant();
            Assert.IsFalse(crop.NeedsFertilizer, "not during Seed");
            GrowWatered(crop, 70); // Vegetative (20-30%)
            Assert.AreEqual(GrowthStage.Vegetative, crop.Stage);
            Assert.IsTrue(crop.NeedsFertilizer);
            Assert.IsTrue(crop.CanPrune);
            Assert.IsFalse(crop.HasNutrientDeficiency, "nutrients from Flowering");
            crop.ApplyNutrient(); // fertilizer
            Assert.IsFalse(crop.NeedsFertilizer);
            GrowWatered(crop, 30); // Flowering
            Assert.IsTrue(crop.HasNutrientDeficiency);
            crop.ApplySpecialNutrient();
            Assert.IsFalse(crop.HasNutrientDeficiency);
            crop.Prune();
            Assert.IsTrue(crop.Pruned);
            Assert.AreEqual(CropHealth.Healthy, crop.Health);
        }

        [Test]
        public void PruneWindow_ClosesAfterFlowering()
        {
            var crop = Plant();
            GrowWatered(crop, 130); // Fruiting
            Assert.AreEqual(GrowthStage.Fruiting, crop.Stage);
            Assert.IsFalse(crop.CanPrune);
            crop.Prune();
            Assert.IsFalse(crop.Pruned);
        }

        [Test]
        public void Pests_AppearAtScheduledProgress_AndSprayClearsThem()
        {
            var crop = Plant();
            crop.SchedulePest(0.5f);
            GrowWatered(crop, 140);
            Assert.IsFalse(crop.HasPests);
            GrowWatered(crop, 20);
            Assert.IsTrue(crop.HasPests);
            Assert.AreEqual(CropHealth.PestAttack, crop.Health, "pests are the top priority");
            crop.Spray();
            Assert.IsFalse(crop.HasPests);
        }

        [Test]
        public void CareYield_RewardsCare_AndPenalisesNeglect()
        {
            var neglected = Plant();
            neglected.SchedulePest(0.1f);
            GrowWatered(neglected, 300);
            // pests 0.6 x no fertilizer 0.8 x missing nutrient 0.9
            Assert.AreEqual(0.6f * 0.8f * 0.9f, neglected.CareYieldMultiplier, 0.001f);

            var cared = Plant();
            GrowWatered(cared, 70);
            cared.ApplyNutrient();
            cared.Prune();
            GrowWatered(cared, 30);
            cared.ApplySpecialNutrient();
            Assert.AreEqual(1.15f * 1.25f, cared.CareYieldMultiplier, 0.001f);
        }

        [Test]
        public void CropWithoutCareNeeds_HasNoCareModifiers()
        {
            var plain = ScriptableObject.CreateInstance<CropData>();
            plain.growthDurationSeconds = 60f;
            var crop = new CropInstance(plain, _time, _time.Now.Ticks);
            crop.Water();
            _time.AdvanceSeconds(30);
            Assert.IsFalse(crop.NeedsFertilizer);
            Assert.IsFalse(crop.HasNutrientDeficiency);
            Assert.IsFalse(crop.CanPrune);
            Assert.AreEqual(1f, crop.CareYieldMultiplier, 0.001f);
            Object.DestroyImmediate(plain);
        }

        [Test]
        public void CareState_RoundTripsThroughSave()
        {
            var crop = Plant();
            GrowWatered(crop, 70);
            crop.ApplyNutrient();
            crop.Prune();
            crop.SchedulePest(0.8f);
            crop.ReduceRemainingTime(20f);
            _time.AdvanceSeconds(200); // goes dry, penalty ongoing
            var state = crop.CaptureCare();

            var restored = new CropInstance(_tomato, _time, crop.PlantedAtTicks);
            restored.RestoreCare(state);
            Assert.AreEqual(crop.EffectiveSeconds, restored.EffectiveSeconds, 0.001);
            Assert.AreEqual(crop.Stage, restored.Stage);
            Assert.AreEqual(crop.Health, restored.Health);
            Assert.AreEqual(crop.CareYieldMultiplier, restored.CareYieldMultiplier, 0.001f);
        }

        [Test]
        public void CropCareService_ConsumesSupplies()
        {
            var db = ScriptableObject.CreateInstance<CropDatabase>();
            db.Initialize();
            var inventory = new InventoryService(db);
            ServiceLocator.Register(inventory);
            ServiceLocator.Register(new EconomyService());
            var care = new CropCareService();

            var tile = new FarmTile(Vector2Int.zero, 0, true);
            tile.Clear(); tile.Dig(); tile.Prepare();
            var crop = Plant();
            tile.Plant(crop);
            crop.SchedulePest(0.1f);
            GrowWatered(crop, 100); // Flowering, pests active

            Assert.IsFalse(care.TrySpray(tile), "no spray in the bag");
            inventory.Add(CropCareService.PesticideItemId, 1);
            Assert.IsTrue(care.TrySpray(tile));
            Assert.AreEqual(0, inventory.GetCount(CropCareService.PesticideItemId));
            Assert.IsFalse(crop.HasPests);

            inventory.Add("nutrient_calcium", 1);
            Assert.IsTrue(care.TryApplyNutrient(tile));
            Assert.IsTrue(crop.SpecialNutrientApplied);
            Assert.IsTrue(care.TryPrune(tile), "pruning is free");
            Object.DestroyImmediate(db);
        }
    }
}
