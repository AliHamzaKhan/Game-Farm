using UnityEngine;

namespace FarmQuest.Systems.Animals
{
    /// <summary>Pet definition (§15/§22): companionship only, never progression.</summary>
    [CreateAssetMenu(fileName = "Pet_New", menuName = "FarmQuest/Pet Data")]
    public class PetData : ScriptableObject
    {
        public string petId = "dog";
        public string displayName = "Dog";
        [Tooltip("Level at which this pet unlocks (0 = granted by story/event).")]
        public int unlockLevel;
        [TextArea] public string unlockDescription = "A gift from Grandpa!";
        public Sprite icon;
        public GameObject prefab;
    }
}
