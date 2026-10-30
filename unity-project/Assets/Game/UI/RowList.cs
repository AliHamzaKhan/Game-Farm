using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>
    /// Builds simple list rows from the shared row prefab (children "Name", "Price",
    /// "Button"). Rows without an action hide their button. Long text shrinks to fit.
    /// </summary>
    public static class RowList
    {
        public static void Clear(Transform listContent)
        {
            if (listContent == null) return;
            foreach (Transform child in listContent) Object.Destroy(child.gameObject);
        }

        public static GameObject Add(Transform listContent, GameObject rowPrefab, string name, string price,
            string buttonLabel = null, UnityEngine.Events.UnityAction action = null)
        {
            if (rowPrefab == null || listContent == null) return null;
            var row = Object.Instantiate(rowPrefab, listContent);
            SetText(row.transform, "Name", name);
            SetText(row.transform, "Price", price);
            var buttonT = FindChild(row.transform, "Button");
            var button = buttonT != null ? buttonT.GetComponent<Button>() : null;
            if (button == null) return row;
            button.gameObject.SetActive(action != null);
            if (action == null) return row;
            var label = button.GetComponentInChildren<Text>();
            if (label != null) label.text = buttonLabel;
            button.onClick.AddListener(action);
            return row;
        }

        private static void SetText(Transform row, string child, string value)
        {
            var t = FindChild(row, child);
            var text = t != null ? t.GetComponent<Text>() : null;
            if (text == null) return;
            text.supportRichText = true;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 18;
            text.resizeTextMaxSize = 36;
            text.text = value;
        }

        public static Transform FindChild(Transform parent, string name)
        {
            foreach (Transform c in parent)
            {
                if (c.name == name) return c;
                var deep = FindChild(c, name);
                if (deep != null) return deep;
            }
            return null;
        }
    }
}
