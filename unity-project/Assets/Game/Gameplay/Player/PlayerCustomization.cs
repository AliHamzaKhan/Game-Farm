using System.Collections.Generic;
using UnityEngine;

namespace FarmQuest.Gameplay.Player
{
    /// <summary>
    /// Data-driven customization options (§3 GDD). All free, never monetized.
    /// The applier swaps materials/colors on named renderer slots.
    /// </summary>
    [CreateAssetMenu(fileName = "CustomizationOptions", menuName = "FarmQuest/Customization Options")]
    public class CustomizationDatabase : ScriptableObject
    {
        public List<string> bodyTypes = new List<string> { "A", "B", "C" };
        public List<Color> skinTones = new List<Color>();
        public List<string> hairStyles = new List<string>();
        public List<Color> hairColors = new List<Color>();
        public List<Color> shirtColors = new List<Color>();
        public List<Color> pantsColors = new List<Color>();
        public List<string> hats = new List<string> { "None", "Straw Hat", "Cap", "Beanie" };
    }

    [System.Serializable]
    public class CustomizationSelection
    {
        public int bodyType; public int skinTone; public int hairStyle; public int hairColor;
        public int shirt; public int pants; public int hat = 1;
    }

    /// <summary>
    /// Applies a CustomizationSelection to renderer slots on the player rig.
    /// Slot objects are assigned in the inspector (body, hair, shirt, pants, hat).
    /// </summary>
    public class PlayerCustomization : MonoBehaviour
    {
        public CustomizationDatabase database;
        public Renderer bodyRenderer;
        public Renderer hairRenderer;
        public Renderer shirtRenderer;
        public Renderer pantsRenderer;
        public GameObject[] hatPrefabs;

        public CustomizationSelection Current = new CustomizationSelection();

        public void Apply(CustomizationSelection selection)
        {
            Current = selection;
            if (database == null) return;
            SetColor(bodyRenderer, Pick(database.skinTones, selection.skinTone));
            SetColor(hairRenderer, Pick(database.hairColors, selection.hairColor));
            SetColor(shirtRenderer, Pick(database.shirtColors, selection.shirt));
            SetColor(pantsRenderer, Pick(database.pantsColors, selection.pants));
            // Hats: activate chosen prefab, hide others.
            if (hatPrefabs != null)
                for (int i = 0; i < hatPrefabs.Length; i++)
                    if (hatPrefabs[i] != null)
                        hatPrefabs[i].SetActive(i == selection.hat);
        }

        private static void SetColor(Renderer r, Color c)
        {
            if (r != null) r.material.color = c;
        }

        private static Color Pick(List<Color> list, int index)
        {
            if (list == null || list.Count == 0) return Color.white;
            return list[Mathf.Clamp(index, 0, list.Count - 1)];
        }
    }
}
