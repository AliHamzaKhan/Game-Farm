using FarmQuest.Core.Services;
using FarmQuest.Systems.Missions;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>
    /// Daily missions panel (§29): progress bars + claim buttons.
    /// Row prefab children: "Name", "Price" (progress), "Button".
    /// </summary>
    public class MissionPanelController : MonoBehaviour
    {
        public Transform listContent;
        public GameObject rowPrefab;
        public Button closeButton;

        private MissionService _missions;

        private void Awake()
        {
            closeButton?.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void OnEnable()
        {
            _missions = ServiceLocator.Get<MissionService>();
            GameEvents.MissionsChanged += Rebuild;
            Rebuild();
        }

        private void OnDisable()
        {
            GameEvents.MissionsChanged -= Rebuild;
        }

        private void Rebuild()
        {
            if (listContent == null || rowPrefab == null || _missions == null) return;
            foreach (Transform child in listContent) Destroy(child.gameObject);

            foreach (var mission in _missions.Active)
            {
                var row = Instantiate(rowPrefab, listContent);
                SetText(row, "Name", mission.Data.GetTitle());
                SetText(row, "Price", $"{mission.Progress}/{mission.Data.targetCount}");

                var button = FindButton(row);
                if (button == null) continue;
                var label = button.GetComponentInChildren<Text>();
                string id = mission.Data.missionId;

                if (mission.Claimed)
                {
                    button.gameObject.SetActive(false);
                }
                else if (mission.Completed)
                {
                    if (label != null) label.text = $"CLAIM 🎁";
                    button.onClick.AddListener(() => _missions.Claim(id));
                }
                else
                {
                    button.interactable = false;
                    if (label != null) label.text = "…";
                }
            }
        }

        private static Button FindButton(GameObject row)
        {
            foreach (Transform c in row.transform)
            {
                if (c.name == "Button") return c.GetComponent<Button>();
                var deep = c.GetComponentInChildren<Button>();
                if (deep != null) return deep;
            }
            return null;
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
