using System.Collections.Generic;

namespace FarmQuest.Systems.Npcs
{
    /// <summary>Registry for NPC data (§31). Quest hooks live in StoryService.</summary>
    public class NpcService
    {
        private readonly Dictionary<string, NpcData> _npcs = new Dictionary<string, NpcData>();

        public NpcService(List<NpcData> npcs)
        {
            if (npcs == null) return;
            foreach (var npc in npcs)
                if (npc != null && !_npcs.ContainsKey(npc.npcId))
                    _npcs.Add(npc.npcId, npc);
        }

        public NpcData Get(string npcId)
        {
            _npcs.TryGetValue(npcId, out var npc);
            return npc;
        }

        public IEnumerable<NpcData> All() => _npcs.Values;
    }
}
