using FarmQuest.Core.Services;
using FarmQuest.Systems.Animals;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>
    /// Barn panel (§21): owned animals (feed/pet/collect) + buy new animals.
    /// Row prefab children: "Name", "Price", "Button".
    /// </summary>
    public class AnimalPanelController : MonoBehaviour
    {
        [Header("List mode")]
        public Transform listContent;
        public GameObject rowPrefab;

        [Header("Detail mode")]
        public GameObject detailGroup;
        public Text detailTitle;
        public Text detailState;
        public Button feedButton;
        public Button petButton;
        public Button collectButton;
        public Button backButton;

        public Button closeButton;

        private AnimalService _animals;
        private AnimalInstance _selected;

        private void Awake()
        {
            closeButton?.onClick.AddListener(() => gameObject.SetActive(false));
            backButton?.onClick.AddListener(() => { _selected = null; Refresh(); });
            feedButton?.onClick.AddListener(() => { _animals.FeedAnimal(_selected); Refresh(); });
            petButton?.onClick.AddListener(() => { _animals.PetAnimal(_selected); Refresh(); });
            collectButton?.onClick.AddListener(() => { _animals.CollectProducts(_selected); Refresh(); });
        }

        private void OnEnable()
        {
            _animals = ServiceLocator.Get<AnimalService>();
            GameEvents.AnimalsChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.AnimalsChanged -= Refresh;
        }

        public void Show(AnimalInstance animal)
        {
            _selected = animal;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            else Refresh();
        }

        private void Refresh()
        {
            if (_animals == null) return;
            bool detail = _selected != null;
            if (detailGroup != null) detailGroup.SetActive(detail);
            if (listContent != null) listContent.gameObject.SetActive(!detail);

            if (detail) RefreshDetail();
            else RefreshList();
        }

        private void RefreshDetail()
        {
            var a = _selected;
            if (detailTitle != null) detailTitle.text = $"🐾 {a.Data.displayName}";
            if (detailState != null)
                detailState.text = $"State: {a.CurrentState}\n" +
                    $"Happiness: {(int)a.Happiness}%\n" +
                    (a.Produces ? $"Products ready: {a.ProductsReady}/{a.Data.maxStoredProducts}\n" : "A loyal companion — no products.\n") +
                    (a.IsHungry ? "Hungry! Feed soon." : "Well fed. 😊");
            SetButton(feedButton, "Feed 🥕", true);
            SetButton(petButton, "Pet 💕", true);
            bool canCollect = a.Produces && a.ProductsReady > 0;
            SetButton(collectButton, canCollect ? $"Collect ({a.ProductsReady}) 🥚" : "Collect", canCollect);
        }

        private static void SetButton(Button b, string label, bool interactable)
        {
            if (b == null) return;
            b.interactable = interactable;
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = label;
        }

        private void RefreshList()
        {
            if (listContent == null || rowPrefab == null) return;
            foreach (Transform child in listContent) Destroy(child.gameObject);

            // Owned animals
            foreach (var animal in _animals.Animals)
            {
                var a = animal;
                string state = a.CurrentState.ToString();
                AddRow($"🐾 {a.Data.displayName}", state, "VIEW", () => Show(a));
            }

            // Buyable animals
            var dbAnimals = ServiceLocator.Get<AnimalDatabase>();
            var progression = ServiceLocator.Get<Systems.Economy.ProgressionService>();
            foreach (var data in dbAnimals.animals)
            {
                if (data == null || !progression.IsLevelUnlocked(data.unlockLevel)) continue;
                string d = data.animalId;
                int free = _animals.FreeSlots(data.housing);
                AddRow($"➕ {data.displayName}", $"🪙{data.buyPrice} ({data.housing} {free} free)",
                    "BUY", () => _animals.BuyAnimal(d));
            }
        }

        private void AddRow(string name, string info, string buttonLabel, UnityEngine.Events.UnityAction action)
        {
            var row = Instantiate(rowPrefab, listContent);
            SetRowText(row, "Name", name);
            SetRowText(row, "Price", info);
            var button = row.transform.Find("Button")?.GetComponent<Button>()
                         ?? row.GetComponentInChildren<Button>();
            if (button != null)
            {
                var label = button.GetComponentInChildren<Text>();
                if (label != null) label.text = buttonLabel;
                button.onClick.AddListener(action);
            }
        }

        private static void SetRowText(GameObject row, string child, string value)
        {
            var t = row.transform.Find(child)?.GetComponent<Text>();
            if (t != null) t.text = value;
        }
    }
}
