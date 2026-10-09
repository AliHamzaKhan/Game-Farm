using FarmQuest.Core.Services;
using FarmQuest.Gameplay.Player;
using UnityEngine;

namespace FarmQuest.Systems.Animals
{
    /// <summary>
    /// VIEW: one animal in the world. Shows status icons (hungry / product ready /
    /// sleeping), gentle idle motion. Tap → opens the animal panel. No logic (§51).
    /// </summary>
    public class AnimalView : MonoBehaviour, IInteractable
    {
        public GameObject hungryIcon;
        public GameObject readyIcon;
        public GameObject sleepIcon;

        public AnimalInstance Animal { get; private set; }

        private Vector3 _home;
        private float _wanderT;

        public string InteractLabel => Animal != null ? Animal.Data.displayName : "Animal";

        public void Bind(AnimalInstance animal)
        {
            Animal = animal;
            _home = transform.position;
            GameEvents.AnimalsChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            GameEvents.AnimalsChanged -= Refresh;
        }

        public void Interact()
        {
            var panel = FindObjectOfType<UI.AnimalPanelController>();
            if (panel != null) panel.Show(Animal);
        }

        private static bool IsNightNow()
        {
            int h = System.DateTime.Now.Hour;
            return h < 6 || h >= 21;
        }

        private void Refresh()
        {
            if (Animal == null) return;
            bool night = IsNightNow();
            var state = Animal.CurrentState;
            if (hungryIcon != null) hungryIcon.SetActive(!night && state == AnimalState.Hungry);
            if (readyIcon != null) readyIcon.SetActive(!night && state == AnimalState.Ready);
            if (sleepIcon != null) sleepIcon.SetActive(night);
        }

        private void Update()
        {
            if (Animal == null || IsNightNow()) return;
            // Gentle wandering around home spot — the farm feels alive.
            _wanderT += Time.deltaTime * 0.25f;
            Vector3 offset = new Vector3(Mathf.Sin(_wanderT) * 1.2f, 0f, Mathf.Cos(_wanderT * 0.7f) * 1.2f);
            transform.position = Vector3.Lerp(transform.position, _home + offset, 2f * Time.deltaTime);
        }
    }
}
