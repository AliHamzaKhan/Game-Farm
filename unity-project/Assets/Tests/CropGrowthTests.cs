using System;
using FarmQuest.Core.Services;
using FarmQuest.Data;
using FarmQuest.Systems.Farming;
using NUnit.Framework;
using UnityEngine;

namespace FarmQuest.Tests
{
    /// <summary>Crop growth, stages, offline progression, quality (§58).</summary>
    public class CropGrowthTests
    {
        private FakeTimeService _time;
        private CropData _carrot;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            _time = new FakeTimeService();
            _carrot = ScriptableObject.CreateInstance<CropData>();
            _carrot.cropId = "carrot";
            _carrot.displayName = "Carrot";
            _carrot.growthDurationSeconds = 120f;
            _carrot.growthStages = 6;
            _carrot.waterRequired = 1;
            _carrot.harvestMin = 4;
            _carrot.harvestMax = 6;
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Clear();
            UnityEngine.Object.DestroyImmediate(_carrot);
        }

        private CropInstance PlantAt(DateTime plantedAt) =>
            new CropInstance(_carrot, _time, plantedAt.Ticks);

        [Test]
        public void Progress_StartsAtZero()
        {
            var crop = PlantAt(_time.Now);
            Assert.AreEqual(0f, crop.Progress, 0.001f);
            Assert.IsFalse(crop.IsReady);
        }

        [Test]
        public void Progress_ReachesReady_AfterDuration()
        {
            var crop = PlantAt(_time.Now);
            _time.AdvanceSeconds(119);
            Assert.IsFalse(crop.IsReady);
            _time.AdvanceSeconds(2);
            Assert.IsTrue(crop.IsReady);
            Assert.AreEqual(1f, crop.Progress, 0.001f);
        }

        [Test]
        public void Progress_WorksOffline_AfterLongAbsence()
        {
            // Planted, game closed for a day, reopened: crop is ready (§9).
            var crop = PlantAt(_time.Now.AddHours(-25));
            Assert.IsTrue(crop.IsReady);
        }

        [Test]
        public void Stage_AdvancesThroughGrowth()
        {
            var crop = PlantAt(_time.Now);
            Assert.AreEqual(0, crop.CurrentStage);
            _time.AdvanceSeconds(60); // 50%
            Assert.AreEqual(3, crop.CurrentStage);
            _time.AdvanceSeconds(60);
            Assert.AreEqual(5, crop.CurrentStage); // clamped to stages-1
        }

        [Test]
        public void Quality_Premium_WithPerfectCare()
        {
            var crop = PlantAt(_time.Now);
            crop.Water();            // water requirement met
            crop.ApplyCompost();     // compost ok
            crop.ApplyNutrient();    // nutrient ok
            _time.AdvanceSeconds(120);
            // harvested right at ready => perfect timing => score 4
            Assert.AreEqual(QualityGrade.Premium, crop.CalculateQuality());
        }

        [Test]
        public void Quality_Normal_WhenNeglected()
        {
            var crop = PlantAt(_time.Now);
            _time.AdvanceSeconds(120);
            // no water, no compost, no nutrients, but harvested quickly => score 1
            Assert.AreEqual(QualityGrade.Normal, crop.CalculateQuality());
        }

        [Test]
        public void GrowthService_NotifiesReady_ExactlyOnce()
        {
            var growth = new CropGrowthService(_time);
            var tile = new FarmTile(new Vector2Int(0, 0), 0, true);
            var crop = PlantAt(_time.Now);
            int notifications = 0;
            GameEvents.CropReady += c => { if (c == crop) notifications++; };

            growth.Track(crop, tile);
            _time.AdvanceSeconds(130);
            _time.RaiseTick();
            _time.RaiseTick();
            Assert.AreEqual(1, notifications);
            Assert.AreEqual(SoilState.ReadyToHarvest, tile.SoilState);
        }

        [Test]
        public void TimeAcceleration_ReducesRemaining()
        {
            var crop = PlantAt(_time.Now);
            var accel = new TimeAccelerationService();
            float reduced = accel.AccelerateCrop(crop, 60f);
            Assert.AreEqual(60f, reduced, 0.01f);
            Assert.AreEqual(60f, crop.RemainingSeconds, 0.01f);
        }
    }
}
