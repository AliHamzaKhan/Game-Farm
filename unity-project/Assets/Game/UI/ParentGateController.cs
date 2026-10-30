using FarmQuest.Core.Services;
using FarmQuest.Systems.ParentMode;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>
    /// Parent gate (§43): arithmetic challenge. On success, opens the target
    /// panel (parent dashboard). Assign buttons[3] in the inspector.
    /// </summary>
    public class ParentGateController : MonoBehaviour
    {
        public Text questionText;
        public Button[] answerButtons = new Button[3];
        public Button cancelButton;
        public GameObject dashboardPanel;

        private ParentModeService _parents;
        private ParentGateChallenge _challenge;
        private System.Action _onPassed;

        /// <summary>
        /// Asks a grown-up before onPassed runs (used before every real-money purchase).
        /// Cancel or a wrong answer never runs it.
        /// </summary>
        public void Request(System.Action onPassed)
        {
            _onPassed = onPassed;
            transform.SetAsLastSibling(); // above whichever panel asked
            gameObject.SetActive(true);
            NewChallenge();
        }

        private void Awake()
        {
            cancelButton?.onClick.AddListener(() => { _onPassed = null; gameObject.SetActive(false); });
            for (int i = 0; i < answerButtons.Length; i++)
            {
                int idx = i;
                if (answerButtons[i] != null)
                    answerButtons[i].onClick.AddListener(() => Answer(idx));
            }
        }

        private void OnEnable()
        {
            _parents = ServiceLocator.Get<ParentModeService>();
            NewChallenge();
        }

        private void OnDisable() => _onPassed = null;

        private void NewChallenge()
        {
            _challenge = _parents.GenerateChallenge();
            if (questionText != null) questionText.text = _challenge.Question;
            for (int i = 0; i < answerButtons.Length && i < 3; i++)
            {
                var label = answerButtons[i]?.GetComponentInChildren<Text>();
                if (label != null) label.text = _challenge.Options[i].ToString();
            }
        }

        private void Answer(int picked)
        {
            if (_parents.VerifyAnswer(_challenge, picked))
            {
                gameObject.SetActive(false);
                var action = _onPassed;
                _onPassed = null;
                if (action != null) action();
                else if (dashboardPanel != null) dashboardPanel.SetActive(true);
            }
            else
            {
                GameEvents.RaiseToast("That's not right — ask a grown-up for help! 👨‍👩‍👧");
                NewChallenge();
            }
        }
    }
}
