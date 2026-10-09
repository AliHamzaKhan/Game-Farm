using FarmQuest.Core.Services;
using FarmQuest.Gameplay.Player;
using UnityEngine;

namespace FarmQuest.Systems.Orchard
{
    /// <summary>VIEW: one orchard tree. Fruit indicator when ready; tap to harvest.</summary>
    public class TreeView : MonoBehaviour, IInteractable
    {
        public GameObject fruitIndicator;

        public TreeInstance Tree { get; private set; }

        public string InteractLabel =>
            Tree != null && Tree.FruitReady ? $"Harvest {Tree.Data.displayName} 🍎" : "Growing…";

        public void Bind(TreeInstance tree)
        {
            Tree = tree;
            Refresh();
        }

        public void Interact()
        {
            if (Tree == null || !Tree.FruitReady)
            {
                GameEvents.RaiseToast("Still growing… 🌱");
                return;
            }
            if (ServiceLocator.TryGet(out OrchardService orchard))
            {
                orchard.HarvestTree(Tree);
                Refresh();
            }
        }

        private void Update()
        {
            // Cheap readiness check a few times per second via shared tick would be
            // nicer; per-tree Update is fine at orchard scale (≤12 trees).
            if (Tree != null && fruitIndicator != null)
                fruitIndicator.SetActive(Tree.FruitReady);
        }

        private void Refresh()
        {
            if (fruitIndicator != null && Tree != null)
                fruitIndicator.SetActive(Tree.FruitReady);
        }
    }
}
