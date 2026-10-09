using FarmQuest.Core.Services;
using FarmQuest.Gameplay.Player;
using UnityEngine;

namespace FarmQuest.Systems.Npcs
{
    /// <summary>
    /// VIEW: a villager standing in the world. Tap → dialogue panel.
    /// Implements IInteractable for the interaction system.
    /// </summary>
    public class NpcView : MonoBehaviour, IInteractable
    {
        public string npcId = "grandpa";

        public string InteractLabel
        {
            get
            {
                var npc = ServiceLocator.TryGet(out NpcService service) ? service.Get(npcId) : null;
                return npc != null ? $"Talk to {npc.displayName}" : "Talk";
            }
        }

        public void Interact()
        {
            if (!ServiceLocator.TryGet(out NpcService service)) return;
            var npc = service.Get(npcId);
            if (npc == null) return;
            var dialogue = FindObjectOfType<UI.DialoguePanelController>();
            if (dialogue != null) dialogue.Show(npc);
            else GameEvents.RaiseToast(npc.dialogueLines.Count > 0 ? npc.dialogueLines[0] : "...");
        }
    }
}
