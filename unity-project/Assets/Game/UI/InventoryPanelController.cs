using FarmQuest.Core.Services;
using FarmQuest.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>Simple readable inventory list (§18), grouped by category.</summary>
    public class InventoryPanelController : MonoBehaviour
    {
        public Transform listContent;
        public GameObject rowPrefab; // children: "Name", "Price"(count), no button
        public Button closeButton;
        public Button sortButton;

        private void Awake()
        {
            closeButton?.onClick.AddListener(() => gameObject.SetActive(false));
            sortButton?.onClick.AddListener(() =>
            {
                ServiceLocator.Get<Systems.Inventory.InventoryService>().Sort();
                Rebuild();
            });
        }

        private void OnEnable() => Rebuild();

        private void Rebuild()
        {
            if (listContent == null || rowPrefab == null) return;
            foreach (Transform child in listContent) Destroy(child.gameObject);

            var inventory = ServiceLocator.Get<Systems.Inventory.InventoryService>();
            var db = ServiceLocator.Get<CropDatabase>();

            foreach (var slot in inventory.Slots)
            {
                var item = db.GetItem(slot.ItemId);
                string name = item != null ? item.displayName : slot.ItemId;
                var row = Instantiate(rowPrefab, listContent);
                SetText(row, "Name", $"{name} ×{slot.Count}");
                SetText(row, "Price", slot.Quality.ToString());
            }
        }

        private static void SetText(GameObject row, string child, string value)
        {
            foreach (Transform c in row.transform)
            {
                if (c.name == child)
                {
                    var t = c.GetComponent<Text>();
                    if (t != null) t.text = value;
                    return;
                }
            }
        }
    }
}
