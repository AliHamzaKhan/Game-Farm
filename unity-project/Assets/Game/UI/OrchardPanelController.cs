using FarmQuest.Core.Services;
using FarmQuest.Systems.Machines;
using FarmQuest.Systems.Orchard;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>Orchard panel: buy saplings, see planted trees + free spots.</summary>
    public class OrchardPanelController : MonoBehaviour
    {
        public Transform listContent;
        public GameObject rowPrefab; // children: "Name", "Price", "Button"
        public Button closeButton;
        public Text statusText;

        private OrchardService _orchard;
        private MachineDatabase _database;

        private void Awake()
        {
            closeButton?.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void OnEnable()
        {
            _orchard = ServiceLocator.Get<OrchardService>();
            _database = ServiceLocator.Get<MachineDatabase>();
            GameEvents.OrchardChanged += Rebuild;
            Rebuild();
        }

        private void OnDisable()
        {
            GameEvents.OrchardChanged -= Rebuild;
        }

        private void Rebuild()
        {
            if (listContent == null || rowPrefab == null) return;
            foreach (Transform child in listContent) Destroy(child.gameObject);

            if (statusText != null)
                statusText.text = $"🌳 {_orchard.Trees.Count}/{OrchardService.MaxSpots} trees planted";

            var progression = ServiceLocator.Get<Systems.Economy.ProgressionService>();
            foreach (var tree in _database.trees)
            {
                if (tree == null || !progression.IsLevelUnlocked(tree.unlockLevel)) continue;
                string id = tree.treeId;
                AddRow($"🌱 {tree.displayName} (sapling)",
                    $"🪙{tree.saplingCost}\nFruit every {tree.regrowIntervalSeconds / 60f:F0} min",
                    "PLANT", () => _orchard.PlantTree(id));
            }
            foreach (var tree in _orchard.Trees)
            {
                AddRow($"🌳 {tree.Data.displayName}",
                    tree.FruitReady ? "Ready! 🍎" : $"Growing… {(int)(tree.RegrowProgress * 100)}%",
                    "", null);
            }
        }

        private void AddRow(string name, string info, string buttonLabel, UnityEngine.Events.UnityAction action)
        {
            var row = Instantiate(rowPrefab, listContent);
            var nameT = row.transform.Find("Name")?.GetComponent<Text>();
            if (nameT != null) nameT.text = name;
            var priceT = row.transform.Find("Price")?.GetComponent<Text>();
            if (priceT != null) priceT.text = info;
            var button = row.transform.Find("Button")?.GetComponent<Button>()
                         ?? row.GetComponentInChildren<Button>();
            if (button == null) return;
            if (action == null) { button.gameObject.SetActive(false); return; }
            var label = button.GetComponentInChildren<Text>();
            if (label != null) label.text = buttonLabel;
            button.onClick.AddListener(action);
        }
    }
}
