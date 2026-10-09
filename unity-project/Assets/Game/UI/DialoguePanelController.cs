using FarmQuest.Systems.Npcs;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>Simple tap-through dialogue panel (§31). Short lines, big text.</summary>
    public class DialoguePanelController : MonoBehaviour
    {
        public Text nameText;
        public Text lineText;
        public Button nextButton;
        public Button closeButton;
        public Image portraitImage;

        private NpcData _npc;
        private int _lineIndex;

        private void Awake()
        {
            nextButton?.onClick.AddListener(NextLine);
            closeButton?.onClick.AddListener(Hide);
        }

        public void Show(NpcData npc)
        {
            _npc = npc;
            _lineIndex = 0;
            gameObject.SetActive(true);
            Refresh();
        }

        public void Hide() => gameObject.SetActive(false);

        private void Refresh()
        {
            if (_npc == null) return;
            if (nameText != null) nameText.text = $"{_npc.displayName} — {_npc.role}";
            if (lineText != null)
                lineText.text = _npc.dialogueLines.Count > 0
                    ? _npc.dialogueLines[_lineIndex % _npc.dialogueLines.Count]
                    : "...";
            if (portraitImage != null)
            {
                portraitImage.sprite = _npc.portrait;
                portraitImage.gameObject.SetActive(_npc.portrait != null);
            }
            if (nextButton != null)
                nextButton.gameObject.SetActive(_npc.dialogueLines.Count > 1);
        }

        private void NextLine()
        {
            _lineIndex++;
            if (_lineIndex >= _npc.dialogueLines.Count) Hide();
            else Refresh();
        }
    }
}
