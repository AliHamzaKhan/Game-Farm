using System;
using FarmQuest.Core.Save;
using FarmQuest.Core.Services;
using FarmQuest.Core.Time;
using FarmQuest.Systems.Farming;

namespace FarmQuest.Systems.World
{
    public enum WeatherType { Sunny, Cloudy, Rainy, Storm }

    /// <summary>
    /// Weather (§26): changes every few hours with weighted randomness.
    /// Rain waters all growing crops — a gift, never a punishment. Storms are
    /// gentle ambience only (kid-friendly: weather never destroys work).
    /// </summary>
    public class WeatherService
    {
        private readonly ITimeService _time;
        private readonly Random _rng = new Random();

        public WeatherType Current { get; private set; } = WeatherType.Sunny;
        private long _nextChangeTicks;

        public WeatherService(ITimeService time)
        {
            _time = time;
            _nextChangeTicks = _time.UtcNow.Ticks + ChangeDelayTicks();
            _time.SecondTick += Poll;
        }

        private long ChangeDelayTicks()
        {
            double hours = 3.0 + _rng.NextDouble() * 3.0; // every 3–6 hours
            return (long)(hours * TimeSpan.TicksPerHour);
        }

        private void Poll()
        {
            if (_time.UtcNow.Ticks < _nextChangeTicks) return;
            _nextChangeTicks = _time.UtcNow.Ticks + ChangeDelayTicks();
            RollWeather();
        }

        private void RollWeather()
        {
            double roll = _rng.NextDouble();
            WeatherType next = roll < 0.50 ? WeatherType.Sunny
                : roll < 0.75 ? WeatherType.Cloudy
                : roll < 0.93 ? WeatherType.Rainy
                : WeatherType.Storm;
            SetWeather(next);
        }

        private void SetWeather(WeatherType next)
        {
            if (next == Current) return;
            Current = next;
            GameEvents.RaiseWeatherChanged();
            switch (next)
            {
                case WeatherType.Rainy:
                    WaterAllCrops();
                    GameEvents.RaiseToast("🌧️ Rain! Your crops got watered!");
                    break;
                case WeatherType.Storm:
                    GameEvents.RaiseToast("⛈️ A gentle storm passes…");
                    break;
                case WeatherType.Cloudy:
                    GameEvents.RaiseToast("☁️ Cloudy skies.");
                    break;
                default:
                    GameEvents.RaiseToast("☀️ Sunny day!");
                    break;
            }
        }

        private void WaterAllCrops()
        {
            if (!ServiceLocator.TryGet(out FarmService farm)) return;
            foreach (var tile in farm.AllTiles())
            {
                if (tile.HasCrop && !tile.Crop.IsReady &&
                    tile.Crop.WateringsDone < tile.Crop.Data.waterRequired)
                    farm.TryWater(tile);
            }
        }

        public string Forecast()
        {
            double hours = Math.Max(0,
                (_nextChangeTicks - _time.UtcNow.Ticks) / (double)TimeSpan.TicksPerHour);
            return $"{Current} — changing in ~{(int)hours}h";
        }

        // ---------- save ----------
        public WeatherSaveData CaptureState()
        {
            return new WeatherSaveData
            {
                current = (int)Current,
                nextChangeTicks = _nextChangeTicks
            };
        }

        public void RestoreState(WeatherSaveData data)
        {
            if (data == null) return;
            Current = (WeatherType)data.current;
            _nextChangeTicks = data.nextChangeTicks;
        }
    }
}
