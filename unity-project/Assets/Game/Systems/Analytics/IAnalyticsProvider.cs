using System.Collections.Generic;

namespace FarmQuest.Systems.Analytics
{
    /// <summary>
    /// Analytics provider seam (§46). Implementations must be privacy-safe:
    /// gameplay facts only — never PII, device IDs, or ad identifiers.
    /// </summary>
    public interface IAnalyticsProvider
    {
        void Track(string eventName, Dictionary<string, string> properties);
        void Flush();
    }

    /// <summary>Editor provider: logs events to the console.</summary>
    public class DebugAnalyticsProvider : IAnalyticsProvider
    {
        public void Track(string eventName, Dictionary<string, string> properties)
        {
            string props = properties != null ? string.Join(",", properties) : "";
            UnityEngine.Debug.Log($"[Analytics] {eventName} {props}");
        }

        public void Flush() { }
    }
}
