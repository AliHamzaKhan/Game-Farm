using System.Collections.Generic;
using FarmQuest.Core.Save;
using FarmQuest.Core.Services;
using FarmQuest.Systems.Economy;

namespace FarmQuest.Systems.Story
{
    /// <summary>
    /// Story mode (§30): ordered chapters with objectives tracked via GameEvents.
    /// Chapters are guides, not gates — ignoring them loses only chapter rewards.
    /// </summary>
    public class StoryService
    {
        private readonly List<StoryChapterData> _chapters;
        private readonly List<int> _progress = new List<int>();

        public int CurrentChapterIndex { get; private set; }
        public StoryChapterData CurrentChapter =>
            CurrentChapterIndex < _chapters.Count ? _chapters[CurrentChapterIndex] : null;
        public bool IsComplete => CurrentChapterIndex >= _chapters.Count;

        public StoryService(List<StoryChapterData> chapters)
        {
            _chapters = chapters ?? new List<StoryChapterData>();
            _chapters.Sort((a, b) => a.chapterIndex.CompareTo(b.chapterIndex));
            ResetProgress();

            GameEvents.CropHarvested += (crop, qty) => AddObjective(ObjectiveType.HarvestAny, qty);
            GameEvents.CoinsEarned += amount => AddObjective(ObjectiveType.EarnCoins, (int)amount);
            GameEvents.LevelUp += level => SetObjective(ObjectiveType.ReachLevel, level);
            GameEvents.RegionUnlocked += id => AddObjective(ObjectiveType.UnlockLand, 1);
            GameEvents.ItemsSold += (id, qty) => AddObjective(ObjectiveType.SellAny, qty);
            GameEvents.MissionClaimed += id => AddObjective(ObjectiveType.CompleteMissions, 1);
        }

        private void ResetProgress()
        {
            _progress.Clear();
            var chapter = CurrentChapter;
            if (chapter != null)
                foreach (var o in chapter.objectives)
                {
                    // ReachLevel/UnlockLand are absolute: seed from current state.
                    int seed = 0;
                    if (o.type == ObjectiveType.ReachLevel &&
                        ServiceLocator.TryGet(out ProgressionService p)) seed = p.Level;
                    else if (o.type == ObjectiveType.UnlockLand &&
                        ServiceLocator.TryGet(out Farming.FarmService farm))
                        seed = farm.UnlockedRegionCount();
                    _progress.Add(seed);
                }
        }

        private void AddObjective(ObjectiveType type, int amount)
        {
            var chapter = CurrentChapter;
            if (chapter == null || amount <= 0) return;
            bool changed = false;
            for (int i = 0; i < chapter.objectives.Count; i++)
            {
                if (chapter.objectives[i].type != type) continue;
                int target = chapter.objectives[i].targetCount;
                int before = _progress[i];
                _progress[i] = System.Math.Min(target, before + amount);
                if (_progress[i] != before) changed = true;
            }
            if (changed) CheckComplete();
        }

        private void SetObjective(ObjectiveType type, int value)
        {
            var chapter = CurrentChapter;
            if (chapter == null) return;
            bool changed = false;
            for (int i = 0; i < chapter.objectives.Count; i++)
            {
                if (chapter.objectives[i].type != type) continue;
                int target = chapter.objectives[i].targetCount;
                int v = System.Math.Min(target, value);
                if (_progress[i] != v) { _progress[i] = v; changed = true; }
            }
            if (changed) CheckComplete();
        }

        private void CheckComplete()
        {
            var chapter = CurrentChapter;
            if (chapter == null) return;
            for (int i = 0; i < chapter.objectives.Count; i++)
                if (_progress[i] < chapter.objectives[i].targetCount) return;

            // Chapter complete!
            var economy = ServiceLocator.Get<EconomyService>();
            economy.AddCoins(chapter.rewardCoins, "story");
            economy.AddXp(chapter.rewardXp, "story");
            int finished = chapter.chapterIndex;
            GameEvents.RaiseStoryChapterCompleted(finished);
            GameEvents.RaiseToast($"📖 Chapter complete: {chapter.title}!");

            CurrentChapterIndex++;
            ResetProgress();
            // Chain: the progress events that just fired shouldn't double-count,
            // but objectives are generous — acceptable for a cozy game.
            CheckComplete(); // in case next chapter is trivially complete
        }

        public IReadOnlyList<int> ObjectiveProgress => _progress;

        // ---------- save ----------
        public StorySaveData CaptureState()
        {
            return new StorySaveData
            {
                chapterIndex = CurrentChapterIndex,
                objectiveProgress = new List<int>(_progress)
            };
        }

        public void RestoreState(StorySaveData data)
        {
            if (data == null) return;
            CurrentChapterIndex = UnityEngine.Mathf.Clamp(data.chapterIndex, 0, _chapters.Count);
            ResetProgress();
            if (data.objectiveProgress != null)
                for (int i = 0; i < data.objectiveProgress.Count && i < _progress.Count; i++)
                    _progress[i] = data.objectiveProgress[i];
        }
    }
}
