using System.Collections.Generic;
using UnityEngine;

namespace FarmQuest.Systems.Npcs
{
    /// <summary>Data-driven villager (§31). Predefined dialogue only — never open chat.</summary>
    [CreateAssetMenu(fileName = "Npc_New", menuName = "FarmQuest/NPC Data")]
    public class NpcData : ScriptableObject
    {
        public string npcId = "grandpa";
        public string displayName = "Grandpa Hamza";
        public string role = "Mentor";
        public Sprite portrait;
        [TextArea(2, 4)]
        public List<string> dialogueLines = new List<string>
        {
            "Welcome to your new farm!"
        };
    }
}
