using System.Text;
using FarmQuest.Core.Services;
using FarmQuest.Systems.Orders;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>Delivery order board (§20): active orders, fulfill, timers.</summary>
    public class OrderBoardPanelController : MonoBehaviour
    {
        public Transform listContent;
        public GameObject rowPrefab; // children: "Name", "Price", "Button"
        public Button closeButton;

        private OrderService _orders;

        private void Awake()
        {
            closeButton?.onClick.AddListener(() => gameObject.SetActive(false));
        }

        private void OnEnable()
        {
            _orders = ServiceLocator.Get<OrderService>();
            _orders.EnsureSeeded();
            GameEvents.OrdersChanged += Rebuild;
            Rebuild();
        }

        private void OnDisable()
        {
            GameEvents.OrdersChanged -= Rebuild;
        }

        private void Rebuild()
        {
            if (listContent == null || rowPrefab == null) return;
            foreach (Transform child in listContent) Destroy(child.gameObject);

            if (_orders.Orders.Count == 0)
            {
                var row = Instantiate(rowPrefab, listContent);
                var t = row.transform.Find("Name")?.GetComponent<Text>();
                if (t != null) t.text = "No orders right now — check back soon! 🚚";
                var b = row.transform.Find("Button")?.GetComponent<Button>()
                        ?? row.GetComponentInChildren<Button>();
                if (b != null) b.gameObject.SetActive(false);
                return;
            }

            foreach (var order in _orders.Orders)
            {
                var o = order;
                string title = o.IsPremium ? "⭐ PREMIUM Order" : "🚚 Order";
                var sb = new StringBuilder();
                foreach (var item in o.Items)
                    sb.AppendLine($"{item.count}× {item.itemId}");
                sb.Append($"Reward: 🪙{o.RewardCoins}  ✨{o.RewardXp} XP");
                sb.AppendLine().Append($"Expires in {o.HoursLeft(ServiceLocator.Get<Core.Time.ITimeService>()):F1}h");

                bool can = _orders.CanFulfill(o);
                AddRow(title, sb.ToString(), can ? "DELIVER" : "NEED ITEMS",
                    () => _orders.Fulfill(o), can);
            }
        }

        private void AddRow(string name, string info, string buttonLabel,
            UnityEngine.Events.UnityAction action, bool enabled)
        {
            var row = Instantiate(rowPrefab, listContent);
            var nameT = row.transform.Find("Name")?.GetComponent<Text>();
            if (nameT != null) nameT.text = name;
            var priceT = row.transform.Find("Price")?.GetComponent<Text>();
            if (priceT != null) priceT.text = info;
            var button = row.transform.Find("Button")?.GetComponent<Button>()
                         ?? row.GetComponentInChildren<Button>();
            if (button == null) return;
            var label = button.GetComponentInChildren<Text>();
            if (label != null) label.text = buttonLabel;
            button.interactable = enabled;
            if (enabled) button.onClick.AddListener(action);
        }
    }
}
