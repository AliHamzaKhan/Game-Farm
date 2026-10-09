using FarmQuest.Gameplay.Player;
using UnityEngine;

namespace FarmQuest.Systems.Machines
{
    /// <summary>Tap the parked tractor to start driving (spec §60: visible ownership).</summary>
    public class TractorView : MonoBehaviour, IInteractable
    {
        public string InteractLabel => "Drive Tractor 🚜";

        public void Interact()
        {
            var tractor = FindObjectOfType<TractorController>();
            if (tractor != null && !tractor.IsDriving)
                tractor.StartDriving();
        }
    }
}
