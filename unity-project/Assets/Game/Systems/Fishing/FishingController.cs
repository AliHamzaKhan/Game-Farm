using System.Collections.Generic;
using FarmQuest.Core.Services;
using FarmQuest.Gameplay.Player;
using FarmQuest.Systems.Economy;
using FarmQuest.Systems.Inventory;
using UnityEngine;

namespace FarmQuest.Systems.Fishing
{
    /// <summary>
    /// Fishing spot minigame (§26): cast → wait for a bite → tap in time!
    /// Timing-based, no fail state beyond "it got away". Fish are items.
    /// Attach to a pond-side spot; assign optional biteIndicator.
    /// </summary>
    public class FishingController : MonoBehaviour, IInteractable
    {
        public List<FishData> fishSpecies = new List<FishData>();
        public GameObject biteIndicator;
        public float minBiteDelay = 5f;
        public float maxBiteDelay = 15f;
        public float biteWindow = 2.5f;

        private enum State { Idle, Waiting, Biting }
        private State _state = State.Idle;
        private float _biteAt;
        private float _windowEnd;

        public string InteractLabel => _state switch
        {
            State.Idle => "Cast Line 🎣",
            State.Waiting => "Waiting…",
            State.Biting => "PULL! 🐟",
            _ => "Fish"
        };

        public void Interact()
        {
            switch (_state)
            {
                case State.Idle:
                    Cast();
                    break;
                case State.Biting:
                    Catch();
                    break;
                // Tapping while waiting does nothing — patience, little farmer.
            }
        }

        private void Cast()
        {
            if (fishSpecies.Count == 0)
            {
                GameEvents.RaiseToast("No fish data — assign species in the inspector!");
                return;
            }
            _state = State.Waiting;
            _biteAt = Time.time + Random.Range(minBiteDelay, maxBiteDelay);
            SetIndicator(false);
            GameEvents.RaiseToast("🎣 Cast! Wait for a bite…");
        }

        private void Update()
        {
            if (_state == State.Waiting && Time.time >= _biteAt)
            {
                _state = State.Biting;
                _windowEnd = Time.time + biteWindow;
                SetIndicator(true);
                GameEvents.RaiseToast("❗ BITE! Tap NOW!");
            }
            else if (_state == State.Biting && Time.time >= _windowEnd)
            {
                _state = State.Idle;
                SetIndicator(false);
                GameEvents.RaiseToast("It got away… try again! 🐟");
            }
        }

        private void Catch()
        {
            var fish = RollFish();
            _state = State.Idle;
            SetIndicator(false);
            if (fish == null) return;
            ServiceLocator.Get<InventoryService>().Add("fish_" + fish.fishId, 1);
            ServiceLocator.Get<EconomyService>().AddXp(fish.xpReward, "fishing");
            string stars = fish.rarity == FishRarity.Legendary ? "✨ LEGENDARY! ✨"
                : fish.rarity == FishRarity.Rare ? "⭐ Rare!" : "";
            GameEvents.RaiseToast($"🐟 Caught a {fish.displayName}! {stars}");
        }

        private FishData RollFish()
        {
            float roll = Random.value;
            FishRarity target = roll < 0.60f ? FishRarity.Common
                : roll < 0.85f ? FishRarity.Uncommon
                : roll < 0.97f ? FishRarity.Rare
                : FishRarity.Legendary;
            var matches = fishSpecies.FindAll(f => f.rarity == target);
            if (matches.Count == 0) matches = fishSpecies;
            return matches[Random.Range(0, matches.Count)];
        }

        private void SetIndicator(bool on)
        {
            if (biteIndicator != null) biteIndicator.SetActive(on);
        }
    }
}
