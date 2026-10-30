using FarmQuest.Core.Services;
using FarmQuest.Core.Time;
using FarmQuest.Systems.Farming;

namespace FarmQuest.Systems.Machines
{
    /// <summary>
    /// Advanced watering (§9 GDD / Phase 4): owned irrigation equipment
    /// auto-waters thirsty crops on a timer. Uses the best owned system.
    /// Goes through FarmService.TryWater so XP/events stay consistent.
    /// </summary>
    public class IrrigationService
    {
        private float _timer;

        public IrrigationService(ITimeService time)
        {
            time.SecondTick += OnSecondTick;
        }

        private float _fertilizerTimer;
        private const float FertilizerMachineInterval = 60f;

        private void OnSecondTick()
        {
            if (!ServiceLocator.TryGet(out EquipmentService equipment)) return;
            RunFertilizerMachine(equipment);
            var best = equipment.BestIrrigation();
            if (best == null) return;

            _timer += 1f;
            if (_timer < best.waterIntervalSeconds) return;
            _timer = 0f;

            var farm = ServiceLocator.Get<FarmService>();
            int done = 0;
            foreach (var tile in farm.AllTiles())
            {
                if (done >= best.waterCount) break;
                if (tile.HasCrop && tile.Crop.NeedsWater)
                {
                    if (farm.TryWater(tile)) done++;
                }
            }
            if (done > 0)
                GameEvents.RaiseToast($"💧 Irrigation watered {done} crops!");
        }

        /// <summary>Fertilizer machine: feeds crops that need it, using fertilizer from the bag.</summary>
        private void RunFertilizerMachine(EquipmentService equipment)
        {
            if (!equipment.HasType(EquipmentType.FertilizerMachine)) return;
            _fertilizerTimer += 1f;
            if (_fertilizerTimer < FertilizerMachineInterval) return;
            _fertilizerTimer = 0f;
            var farm = ServiceLocator.Get<FarmService>();
            var inventory = ServiceLocator.Get<Inventory.InventoryService>();
            int done = 0;
            foreach (var tile in farm.AllTiles())
            {
                if (!tile.HasCrop || !tile.Crop.NeedsFertilizer) continue;
                if (inventory.GetCount("fertilizer") <= 0) break;
                if (farm.TryApplyNutrient(tile)) done++;
            }
            if (done > 0) GameEvents.RaiseToast($"Fertilizer machine fed {done} crops!");
        }
    }
}
