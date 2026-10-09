using FarmQuest.Core.Services;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>
    /// Top HUD (§39): coins, XP/level, clock. Bottom nav opens panels.
    /// Bind Text/Button references in the inspector.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        [Header("Top bar")]
        public Text coinsText;
        public Text levelText;
        public Slider xpSlider;
        public Text clockText;

        [Header("Panels")]
        public GameObject marketPanel;
        public GameObject inventoryPanel;
        public GameObject seedShopPanel;
        public GameObject missionPanel;
        public GameObject animalPanel;
        public GameObject decorPanel;
        public GameObject housePanel;
        public GameObject equipmentPanel;
        public GameObject orchardPanel;
        public GameObject productionPanel;
        public GameObject orderPanel;
        public GameObject villagePanel;
        public GameObject parentGatePanel;
        public Text toastText;

        private float _toastTimer;

        private void Start()
        {
            GameEvents.CoinsChanged += OnCoins;
            GameEvents.LevelChanged += OnLevel;
            GameEvents.LevelUp += OnLevelUp;
            GameEvents.ToastRequested += ShowToast;

            var economy = ServiceLocator.Get<Systems.Economy.EconomyService>();
            var progression = ServiceLocator.Get<Systems.Economy.ProgressionService>();
            OnCoins(economy.Coins);
            OnLevel(progression.Level, progression.Xp);
        }

        private void OnDestroy()
        {
            GameEvents.CoinsChanged -= OnCoins;
            GameEvents.LevelChanged -= OnLevel;
            GameEvents.LevelUp -= OnLevelUp;
            GameEvents.ToastRequested -= ShowToast;
        }

        private void OnCoins(long coins)
        {
            if (coinsText != null) coinsText.text = $"🪙 {coins:N0}";
        }

        private void OnLevel(int level, int xp)
        {
            if (levelText != null) levelText.text = $"Lv {level}";
            // xpSlider fill can use ProgressionData thresholds; simplified here.
        }

        private void OnLevelUp(int newLevel)
        {
            ShowToast($"🎉 Level {newLevel}!");
            var audio = FindObjectOfType<Systems.Audio.AudioService>();
            if (audio != null) audio.Play("levelup");
        }

        public void ShowToast(string message)
        {
            if (toastText == null) return;
            toastText.text = message;
            toastText.gameObject.SetActive(true);
            _toastTimer = 2.5f;
        }

        private void Update()
        {
            if (_toastTimer > 0f)
            {
                _toastTimer -= Time.deltaTime;
                if (_toastTimer <= 0f && toastText != null)
                    toastText.gameObject.SetActive(false);
            }
            if (clockText != null)
                clockText.text = System.DateTime.Now.ToString("HH:mm");
        }

        // Wired to bottom-nav buttons:
        public void ToggleMarket() => Toggle(marketPanel);
        public void ToggleInventory() => Toggle(inventoryPanel);
        public void ToggleSeedShop() => Toggle(seedShopPanel);
        public void ToggleMissions() => Toggle(missionPanel);
        public void ToggleAnimals() => Toggle(animalPanel);
        public void ToggleDecor() => Toggle(decorPanel);
        public void ToggleHouse() => Toggle(housePanel);
        public void ToggleEquipment() => Toggle(equipmentPanel);
        public void ToggleOrchard() => Toggle(orchardPanel);
        public void ToggleProduction() => Toggle(productionPanel);
        public void ToggleOrders() => Toggle(orderPanel);
        public void ToggleVillage() => Toggle(villagePanel);
        public void ToggleParentGate() => Toggle(parentGatePanel);

        private static void Toggle(GameObject go)
        {
            if (go != null) go.SetActive(!go.activeSelf);
        }
    }
}
