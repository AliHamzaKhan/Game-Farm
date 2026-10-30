using System.Collections;
using System.Collections.Generic;
using FarmQuest.Core.Services;
using FarmQuest.Systems.World;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>
    /// Full-screen celebration when a city or a country is completed: a short
    /// pop-in animation, the reward, and what was unlocked. Messages queue so a
    /// country completion follows its last city. Animation runs only while shown.
    /// </summary>
    public class CelebrationController : MonoBehaviour
    {
        public GameObject root;          // the overlay (hidden when idle)
        public RectTransform card;       // animated card
        public Text titleText;
        public Text bodyText;
        public Button continueButton;

        private readonly Queue<(string title, string body)> _queue = new Queue<(string, string)>();
        private bool _showing;

        private void Awake()
        {
            continueButton?.onClick.AddListener(Next);
            if (root != null) root.SetActive(false);
            GameEvents.CityCompleted += OnCityCompleted;
            GameEvents.CountryCompleted += OnCountryCompleted;
        }

        private void OnDestroy()
        {
            GameEvents.CityCompleted -= OnCityCompleted;
            GameEvents.CountryCompleted -= OnCountryCompleted;
        }

        private void OnCityCompleted(int index, string cityName)
        {
            ServiceLocator.TryGet(out WorldProgressionService world);
            var done = world?.GetCityLevel(index);
            var next = world?.GetCityLevel(index + 1);
            string reward = done != null
                ? $"+{done.City.rewardCoins:N0} coins, +{done.City.rewardXp} XP" +
                  (done.City.rewardGems > 0 ? $", +{done.City.rewardGems} gems" : "")
                : "";
            string unlocked = next != null ? $"\n\nNext stop: {next.CityName}, {next.CountryName}" : "";
            if (next != null && next.Lap == 0 && next.City.introducedCropIds.Count > 0)
            {
                var db = ServiceLocator.Get<Data.CropDatabase>();
                var names = new List<string>();
                foreach (var id in next.City.introducedCropIds)
                {
                    var crop = db.GetCrop(id);
                    names.Add(crop != null ? crop.displayName : id);
                }
                unlocked += $"\nNew crops: {string.Join(", ", names)}";
            }
            Enqueue($"{cityName} complete!", reward + unlocked);
        }

        private void OnCountryCompleted(string countryName)
        {
            ServiceLocator.TryGet(out WorldProgressionService world);
            // Raised before the tour advances, so the next country starts at the next level.
            var next = world?.GetCityLevel(world.CurrentCityIndex + 1);
            string body = "Country bonus collected!";
            if (next != null) body += $"\n\nA new country awaits: {next.CountryName}\nBigger contracts, bigger rewards.";
            Enqueue($"You farmed all of {countryName}!", body);
        }

        private void Enqueue(string title, string body)
        {
            _queue.Enqueue((title, body));
            if (!_showing) Next();
        }

        private void Next()
        {
            if (_queue.Count == 0)
            {
                _showing = false;
                if (root != null) root.SetActive(false);
                return;
            }
            var (title, body) = _queue.Dequeue();
            _showing = true;
            if (titleText != null) titleText.text = title;
            if (bodyText != null) bodyText.text = body;
            if (root != null) root.SetActive(true);
            if (card != null) StartCoroutine(PopIn());
            var audio = FindObjectOfType<Systems.Audio.AudioService>();
            if (audio != null) audio.Play("levelup");
        }

        private IEnumerator PopIn()
        {
            const float duration = 0.35f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                // Ease-out with a small overshoot.
                float s = 1f + 0.12f * Mathf.Sin(k * Mathf.PI) - (1f - k) * (1f - k) * 0.6f;
                card.localScale = Vector3.one * s;
                yield return null;
            }
            card.localScale = Vector3.one;
        }
    }
}
