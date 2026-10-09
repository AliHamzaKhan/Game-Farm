using FarmQuest.Core.Services;
using FarmQuest.Input;
using FarmQuest.Systems.Animals;
using FarmQuest.Systems.Decor;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>
    /// Decoration shop + placement (§25). Buy → owned → PLACE → tap ground.
    /// Row prefab children: "Name", "Price", "Button".
    /// </summary>
    public class DecorPanelController : MonoBehaviour
    {
        public Transform listContent;
        public GameObject rowPrefab;
        public Button closeButton;
        public LayerMask groundLayer;

        private DecorService _decor;
        private AnimalDatabase _database;
        private string _placingDecorId;
        private IInputService _input;
        private UnityEngine.Camera _cam;

        private void Awake()
        {
            closeButton?.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void OnEnable()
        {
            _decor = ServiceLocator.Get<DecorService>();
            _database = ServiceLocator.Get<AnimalDatabase>();
            GameEvents.DecorChanged += Rebuild;
            Rebuild();
        }

        private void OnDisable()
        {
            GameEvents.DecorChanged -= Rebuild;
            StopPlacing();
        }

        private void Rebuild()
        {
            if (listContent == null || rowPrefab == null) return;
            foreach (Transform child in listContent) Destroy(child.gameObject);

            var progression = ServiceLocator.Get<Systems.Economy.ProgressionService>();

            // Shop: buyable decorations
            foreach (var data in _database.decorations)
            {
                if (data == null || !progression.IsLevelUnlocked(data.unlockLevel)) continue;
                string id = data.decorId;
                AddRow($"🛒 {data.displayName}", $"🪙{data.cost}", "BUY",
                    () => _decor.BuyDecor(id));
            }
            // Owned: place them
            foreach (var data in _decor.OwnedDecorations())
            {
                string id = data.decorId;
                AddRow($"📦 {data.displayName} ×{_decor.OwnedCount(id)}", data.category, "PLACE",
                    () => BeginPlacing(id));
            }
        }

        private void BeginPlacing(string decorId)
        {
            _placingDecorId = decorId;
            gameObject.SetActive(false);
            GameEvents.RaiseToast("Tap the ground to place it! (Tap the decor button again to cancel)");
            if (_input == null)
            {
                _input = ServiceLocator.Get<IInputService>();
                _input.Tapped += OnTapWhilePlacing;
            }
            else
            {
                _input.Tapped += OnTapWhilePlacing;
            }
            if (_cam == null) _cam = UnityEngine.Camera.main;
        }

        private void StopPlacing()
        {
            _placingDecorId = null;
            if (_input != null) _input.Tapped -= OnTapWhilePlacing;
        }

        private void OnTapWhilePlacing(TapInfo tap)
        {
            if (string.IsNullOrEmpty(_placingDecorId) || _cam == null) return;
            var ray = _cam.ScreenPointToRay(tap.ScreenPosition);
            if (!Physics.Raycast(ray, out var hit, 500f, groundLayer)) return;

            // Snap to 1m grid — tidy farms, happy kids.
            float x = Mathf.Round(hit.point.x);
            float z = Mathf.Round(hit.point.z);
            if (_decor.PlaceDecor(_placingDecorId, x, z))
            {
                GameEvents.RaiseToast("Looking good! 🌷");
                if (_decor.OwnedCount(_placingDecorId) <= 0) StopPlacing();
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
            if (button != null)
            {
                var label = button.GetComponentInChildren<Text>();
                if (label != null) label.text = buttonLabel;
                button.onClick.AddListener(action);
            }
        }
    }
}
