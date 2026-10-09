using System.Collections.Generic;
using FarmQuest.Core.Services;
using FarmQuest.Systems.ParentMode;

namespace FarmQuest.Systems.Analytics
{
    /// <summary>
    /// LOGIC: privacy-safe analytics (§46). Tracks gameplay milestones only
    /// (level ups, harvests, purchases of game items). Respects the parent
    /// opt-out toggle. No PII, no device IDs, no ad identifiers — ever.
    /// </summary>
    public class AnalyticsService
    {
        private readonly IAnalyticsProvider _provider;

        public AnalyticsService(IAnalyticsProvider provider) { _provider = provider; }

        private bool Allowed()
        {
            if (!ServiceLocator.TryGet(out ParentModeService parents)) return true;
            return parents.AnalyticsAllowed;
        }

        public void Track(string eventName, Dictionary<string, string> properties = null)
        {
            if (!Allowed()) return;
            var props = properties ?? new Dictionary<string, string>();
            // Coarse, non-identifying context only.
            if (!props.ContainsKey("app_version"))
                props["app_version"] = UnityEngine.Application.version;
            _provider.Track(eventName, props);
        }

        public void Flush() => _provider.Flush();

        /// <summary>Subscribes to key game events for automatic tracking.</summary>
        public void AttachAutoTracking()
        {
            GameEvents.LevelUp += level =>
                Track("level_up", new Dictionary<string, string> { { "level", level.ToString() } });
            GameEvents.AnimalBought += id =>
                Track("animal_bought", new Dictionary<string, string> { { "animal", id } });
            GameEvents.EquipmentBought += id =>
                Track("equipment_bought", new Dictionary<string, string> { { "equipment", id } });
            Track("session_start");
        }
    }
}
