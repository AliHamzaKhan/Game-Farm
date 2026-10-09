using FarmQuest.Core.Services;
using FarmQuest.Systems.Machines;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>Equipment shop (§23): tractors, attachments, irrigation.</summary>
    public class EquipmentPanelController : MonoBehaviour
    {
        public Transform listContent;
        public GameObject rowPrefab; // children: "Name", "Price", "Button"
        public Button closeButton;

        private EquipmentService _equipment;
        private MachineDatabase _database;

        private void Awake()
        {
            closeButton?.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void OnEnable()
        {
            _equipment = ServiceLocator.Get<EquipmentService>();
            _database = ServiceLocator.Get<MachineDatabase>();
            GameEvents.EquipmentChanged += Rebuild;
            Rebuild();
        }

        private void OnDisable()
        {
            GameEvents.EquipmentChanged -= Rebuild;
        }

        private void Rebuild()
        {
            if (listContent == null || rowPrefab == null) return;
            foreach (Transform child in listContent) Destroy(child.gameObject);

            var progression = ServiceLocator.Get<Systems.Economy.ProgressionService>();
            foreach (var data in _database.equipment)
            {
                if (data == null || !progression.IsLevelUnlocked(data.unlockLevel)) continue;
                bool owned = _equipment.Has(data.equipmentId);
                string id = data.equipmentId;
                AddRow($"🚜 {data.displayName}", owned ? "OWNED ✅" : $"🪙{data.cost}\n{data.description}",
                    owned ? "" : "BUY",
                    owned ? null : (UnityEngine.Events.UnityAction)(() => _equipment.BuyEquipment(id)));
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
            if (action == null)
            {
                button.gameObject.SetActive(false);
                return;
            }
            var label = button.GetComponentInChildren<Text>();
            if (label != null) label.text = buttonLabel;
            button.onClick.AddListener(action);
        }
    }
}
