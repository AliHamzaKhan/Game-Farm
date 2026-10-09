using FarmQuest.Core.Save;
using FarmQuest.Core.Services;

namespace FarmQuest.Systems.Story
{
    /// <summary>
    /// Scripted FTUE hints (GDD §4): reacts to the player's real actions.
    /// One hint at a time, skippable by just playing. Persists step in save.
    /// </summary>
    public enum TutorialStep
    {
        Start = 0,          // clear a plot
        ClearDone = 1,      // dig
        DigDone = 2,        // prepare
        PrepareDone = 3,    // plant a seed
        PlantDone = 4,      // water
        WaterDone = 5,      // wait + harvest
        HarvestDone = 6,    // sell at market
        SellDone = 7,       // free play
        Completed = 8
    }

    public class TutorialService
    {
        public TutorialStep Step { get; private set; } = TutorialStep.Start;
        public bool IsComplete => Step == TutorialStep.Completed;

        public TutorialService()
        {
            GameEvents.TileCleared += () => AdvanceTo(TutorialStep.ClearDone);
            GameEvents.TileDug += () => AdvanceTo(TutorialStep.DigDone);
            GameEvents.TilePrepared += () => AdvanceTo(TutorialStep.PrepareDone);
            GameEvents.CropPlanted += id => AdvanceTo(TutorialStep.PlantDone);
            GameEvents.CropWatered += () => AdvanceTo(TutorialStep.WaterDone);
            GameEvents.CropHarvested += (crop, qty) => AdvanceTo(TutorialStep.HarvestDone);
            GameEvents.ItemsSold += (id, qty) => AdvanceTo(TutorialStep.SellDone);
            PublishHint();
        }

        private void AdvanceTo(TutorialStep step)
        {
            if (IsComplete) return;
            if (step > Step)
            {
                Step = step;
                PublishHint();
                // Persist immediately — tutorial progress must survive app kill.
                if (ServiceLocator.TryGet(out SaveCoordinator coordinator))
                    coordinator.SaveAll();
            }
        }

        /// <summary>Player dismissed the tutorial early (a "Skip" button).</summary>
        public void Skip()
        {
            Step = TutorialStep.Completed;
            PublishHint();
        }

        private void PublishHint()
        {
            GameEvents.RaiseTutorialHintChanged(GetHint(Step));
        }

        public static string GetHint(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.Start: return "👋 Welcome! Tap a grass tile, then press Clear.";
                case TutorialStep.ClearDone: return "Great! Now press Dig to turn the soil.";
                case TutorialStep.DigDone: return "Nice! Press Prepare to rake it smooth.";
                case TutorialStep.PrepareDone: return "Pick seeds from the shop 🌱, then tap the plot to Plant.";
                case TutorialStep.PlantDone: return "It's planted! Press Water 💧 to help it grow.";
                case TutorialStep.WaterDone: return "Growing… come back soon, then tap it to Harvest! (It can't die — no rush.)";
                case TutorialStep.HarvestDone: return "What a harvest! Open the Market 🏪 and Sell it.";
                case TutorialStep.SellDone: return "You're a real farmer now! Check Missions 📋 for today's goals.";
                default: return "";
            }
        }

        public void Restore(int step)
        {
            Step = (TutorialStep)UnityEngine.Mathf.Clamp(step, 0, (int)TutorialStep.Completed);
            PublishHint();
        }
    }
}
