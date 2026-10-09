using FarmQuest.Core.Services;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>One-line contextual hint bar for the FTUE. Empty hint = hidden.</summary>
    public class TutorialPanelController : MonoBehaviour
    {
        public Text hintText;
        public Button skipButton;

        private void Awake()
        {
            skipButton?.onClick.AddListener(() =>
            {
                if (ServiceLocator.TryGet(out Systems.Story.TutorialService tutorial))
                    tutorial.Skip();
            });
        }

        private void Start()
        {
            GameEvents.TutorialHintChanged += OnHint;
            // Publish current hint in case we loaded mid-tutorial.
            if (ServiceLocator.TryGet(out Systems.Story.TutorialService tutorial) && !tutorial.IsComplete)
                OnHint(Systems.Story.TutorialService.GetHint(tutorial.Step));
        }

        private void OnDestroy()
        {
            GameEvents.TutorialHintChanged -= OnHint;
        }

        private void OnHint(string hint)
        {
            bool show = !string.IsNullOrEmpty(hint);
            gameObject.SetActive(show);
            if (show && hintText != null) hintText.text = hint;
        }
    }
}
