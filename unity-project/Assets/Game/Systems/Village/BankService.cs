using FarmQuest.Core.Save;
using FarmQuest.Core.Services;
using FarmQuest.Core.Time;
using FarmQuest.Systems.Economy;
using UnityEngine;

namespace FarmQuest.Systems.Village
{
    /// <summary>
    /// LOGIC: village bank (§20) — a gentle savings lesson. Deposits earn 2%
    /// daily interest (capped). No fees, no loans, no risk.
    /// </summary>
    public class BankService
    {
        private const float DailyInterestRate = 0.02f;
        private const int MaxDailyInterest = 200;

        public int Balance { get; private set; }

        public BankService(ITimeService time)
        {
            time.DayChanged += OnDayChanged;
        }

        private void OnDayChanged()
        {
            if (Balance <= 0) return;
            int interest = Mathf.Min(MaxDailyInterest, Mathf.FloorToInt(Balance * DailyInterestRate));
            if (interest > 0)
            {
                Balance += interest;
                GameEvents.RaiseBankChanged();
                GameEvents.RaiseToast($"🏦 Bank interest: +{interest} coins!");
            }
        }

        public bool Deposit(int amount)
        {
            if (amount <= 0) return false;
            var economy = ServiceLocator.Get<EconomyService>();
            if (!economy.TrySpend(amount))
            {
                GameEvents.RaiseToast("Not enough coins!");
                return false;
            }
            Balance += amount;
            GameEvents.RaiseBankChanged();
            return true;
        }

        public bool Withdraw(int amount)
        {
            if (amount <= 0 || amount > Balance) return false;
            Balance -= amount;
            ServiceLocator.Get<EconomyService>().AddCoins(amount, "bank_withdraw");
            GameEvents.RaiseBankChanged();
            return true;
        }

        // ---------- save ----------
        public BankSaveData CaptureState() => new BankSaveData { balance = Balance };

        public void RestoreState(BankSaveData data)
        {
            Balance = data != null ? data.balance : 0;
        }
    }
}
