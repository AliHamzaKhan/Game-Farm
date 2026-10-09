using System;
using FarmQuest.Core.Save;
using NUnit.Framework;

namespace FarmQuest.Tests
{
    /// <summary>Time math and save migration (§58).</summary>
    public class TimeAndSaveTests
    {
        [Test]
        public void Elapsed_ClampsNegativeClockJump_ToZero()
        {
            var time = new FakeTimeService();
            // fromUtc in the future (clock moved backwards): no negative progress.
            Assert.AreEqual(0, time.GetElapsedSeconds(time.Now.AddHours(1)));
        }

        [Test]
        public void Elapsed_MeasuresCorrectly()
        {
            var time = new FakeTimeService();
            var planted = time.Now;
            time.AdvanceSeconds(90);
            Assert.AreEqual(90, time.GetElapsedSeconds(planted.Ticks), 0.01);
        }

        [Test]
        public void Migrator_UpgradesV0_ToV1()
        {
            var data = new SaveData { version = 0 };
            data.economy.adDayStamp = null;
            var migrated = SaveMigrator.Migrate(data);
            Assert.AreEqual(SaveService.CurrentVersion, migrated.version);
            Assert.IsNotNull(migrated.economy.adDayStamp);
        }

        [Test]
        public void Migrator_RepairsMissingSections()
        {
            var data = new SaveData { version = 1, farm = null, inventory = null };
            var migrated = SaveMigrator.Migrate(data);
            Assert.IsNotNull(migrated.farm);
            Assert.IsNotNull(migrated.inventory);
        }

        [Test]
        public void SaveService_RoundTrips_ToDisk()
        {
            string path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), $"fq_test_{Guid.NewGuid():N}.json");
            try
            {
                var service = new SaveService(path);
                var data = new SaveData();
                data.economy.coins = 12345;
                service.Save(data);

                Assert.IsTrue(service.TryLoad(out var loaded));
                Assert.AreEqual(12345, loaded.economy.coins);
                Assert.AreEqual(SaveService.CurrentVersion, loaded.version);
            }
            finally
            {
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
        }
    }
}
