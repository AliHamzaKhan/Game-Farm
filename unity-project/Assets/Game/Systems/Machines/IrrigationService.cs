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

        private void OnSecondTick()
        {
            if (!ServiceLocator.TryGet(out EquipmentService equipment)) return;
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
                if (tile.HasCrop && !tile.Crop.IsReady &&
                    tile.Crop.WateringsDone < tile.Crop.Data.waterRequired)
                {
                    if (farm.TryWater(tile)) done++;
                }
            }
            if (done > 0)
                GameEvents.RaiseToast($"💧 Irrigation watered {done} crops!");
        }
    }
}
