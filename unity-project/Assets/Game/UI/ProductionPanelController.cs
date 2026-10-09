using System.Text;
using FarmQuest.Core.Services;
using FarmQuest.Systems.Machines;
using FarmQuest.Systems.Processing;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>Production buildings panel (§20): queues + recipes + collect.</summary>
    public class ProductionPanelController : MonoBehaviour
    {
        public Transform listContent;
        public GameObject rowPrefab; // children: "Name", "Price", "Button"
        public Button closeButton;

        private ProductionService _production;
        private MachineDatabase _database;

        private void Awake()
        {
            closeButton?.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void OnEnable()
        {
            _production = ServiceLocator.Get<ProductionService>();
            _database = ServiceLocator.Get<MachineDatabase>();
            GameEvents.ProductionChanged += Rebuild;
            Rebuild();
        }

        private void OnDisable()
        {
            GameEvents.ProductionChanged -= Rebuild;
        }

        private void Rebuild()
        {
            if (listContent == null || rowPrefab == null) return;
            foreach (Transform child in listContent) Destroy(child.gameObject);

            var progression = ServiceLocator.Get<Systems.Economy.ProgressionService>();

            foreach (var kvp in ProductionService.BuildingNames)
            {
                string buildingId = kvp.Key;
                AddHeader($"⚙️ {kvp.Value}");

                // Queue
                foreach (var batch in _production.QueueFor(buildingId))
                {
                    var b = batch;
                    string status = b.IsReady ? "Ready! ✅" : $"{(int)(b.Progress * 100)}%";
                    AddRow($"  📦 {b.Recipe.displayName}", status,
                        b.IsReady ? "" : "CANCEL",
                        b.IsReady ? null : (UnityEngine.Events.UnityAction)(() => _production.CancelProduction(b)));
                }

                int ready = _production.ReadyCount(buildingId);
                if (ready > 0)
                    AddRow($"  🎁 Collect {ready} batch(es)", "", "COLLECT",
                        () => _production.CollectBuilding(buildingId));

                // Recipes
                foreach (var recipe in _database.recipes)
                {
                    if (recipe == null || recipe.buildingId != buildingId) continue;
                    if (!progression.IsLevelUnlocked(recipe.unlockLevel)) continue;
                    string id = recipe.recipeId;
                    AddRow($"  ➕ {recipe.displayName}", DescribeRecipe(recipe), "START",
                        () => _production.StartProduction(id));
                }
            }
        }

        private static string DescribeRecipe(RecipeData recipe)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < recipe.inputs.Count; i++)
            {
                if (i > 0) sb.Append(" + ");
                sb.Append($"{recipe.inputs[i].count}×{recipe.inputs[i].itemId}");
            }
            sb.Append($" → {recipe.outputCount}×{recipe.outputItemId}");
            sb.Append($" ({recipe.durationSeconds / 60f:F0}m)");
            return sb.ToString();
        }

        private void AddHeader(string text)
        {
            var row = Instantiate(rowPrefab, listContent);
            var nameT = row.transform.Find("Name")?.GetComponent<Text>();
            if (nameT != null) { nameT.text = text; nameT.fontStyle = FontStyle.Bold; }
            var button = row.transform.Find("Button")?.GetComponent<Button>()
                         ?? row.GetComponentInChildren<Button>();
            if (button != null) button.gameObject.SetActive(false);
            var priceT = row.transform.Find("Price")?.GetComponent<Text>();
            if (priceT != null) priceT.text = "";
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
