using FarmQuest.Core.Services;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Village;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>Village bank (§20): deposits earn 2% daily interest. No risk.</summary>
    public class BankPanelController : MonoBehaviour
    {
        public Text balanceText;
        public Button deposit100Button;
        public Button deposit500Button;
        public Button depositAllButton;
        public Button withdraw100Button;
        public Button withdrawAllButton;
        public Button closeButton;

        private BankService _bank;

        private void Awake()
        {
            closeButton?.onClick.AddListener(() => gameObject.SetActive(false));
            deposit100Button?.onClick.AddListener(() => Do(() => _bank.Deposit(100)));
            deposit500Button?.onClick.AddListener(() => Do(() => _bank.Deposit(500)));
            depositAllButton?.onClick.AddListener(() =>
                Do(() => _bank.Deposit(ServiceLocator.Get<EconomyService>().Coins)));
            withdraw100Button?.onClick.AddListener(() => Do(() => _bank.Withdraw(100)));
            withdrawAllButton?.onClick.AddListener(() => Do(() => _bank.Withdraw(_bank.Balance)));
        }

        private void OnEnable()
        {
            _bank = ServiceLocator.Get<BankService>();
            GameEvents.BankChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.BankChanged -= Refresh;
        }

        private void Do(System.Func<bool> action)
        {
            if (action()) Refresh();
        }

        private void Refresh()
        {
            if (balanceText != null)
                balanceText.text = $"🏦 Balance: 🪙{_bank.Balance}\nEarns 2% interest daily!";
        }
    }
}
