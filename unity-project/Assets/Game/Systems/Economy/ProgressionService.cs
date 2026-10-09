using FarmQuest.Core.Services;
using FarmQuest.Data;

namespace FarmQuest.Systems.Economy
{
    /// <summary>LOGIC: XP and levels (§27), driven by ProgressionData table.</summary>
    public class ProgressionService
    {
        private readonly ProgressionData _data;

        public int Xp { get; private set; }
        public int Level { get; private set; } = 1;

        public ProgressionService(ProgressionData data) { _data = data; }

        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            Xp += amount;
            int newLevel = _data != null ? _data.GetLevelForXp(Xp) : Level;
            if (newLevel > Level)
            {
                Level = newLevel;
                GameEvents.RaiseLevelUp(Level);
                GameEvents.RaiseToast($"Level {Level} — {_data.GetTitle(Level)}!");
            }
            GameEvents.RaiseLevelChanged(Level, Xp);
        }

        public bool IsLevelUnlocked(int requiredLevel) => Level >= requiredLevel;

        public void Restore(int xp, int level)
        {
            Xp = xp;
            Level = System.Math.Max(1, level);
            GameEvents.RaiseLevelChanged(Level, Xp);
        }
    }
}
