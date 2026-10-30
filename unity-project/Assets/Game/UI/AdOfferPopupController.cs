using FarmQuest.Core.Services;
using FarmQuest.Systems.Store;
using UnityEngine;
using UnityEngine.UI;

namespace FarmQuest.UI
{
    /// <summary>
    /// Small, dismissible card for optional ad offers at natural moments
    /// (good harvest, delivery, mission, out of supplies). Hides itself after a
    /// few seconds; ignoring it is the same as "No thanks".
    /// </summary>
    public class AdOfferPopupController : MonoBehaviour
    {
        public GameObject root;
        public Text messageText;
        public Button watchButton;
        public Button noThanksButton;
        public float autoHideSeconds = 8f;

        private AdOfferService _offers;
        private float _hideAt;

        private void Start()
        {
            if (root != null) root.SetActive(false);
            if (!ServiceLocator.TryGet(out _offers)) return;
            _offers.OfferAvailable += Show;
            watchButton?.onClick.AddListener(() =>
            {
                Hide(false);
                _offers.Accept(ok => GameEvents.RaiseToast(ok ? "Bonus added!" : "Ad not finished — no bonus this time."));
            });
            noThanksButton?.onClick.AddListener(() => Hide(true));
        }

        private void OnDestroy()
        {
            if (_offers != null) _offers.OfferAvailable -= Show;
        }

        private void Show(AdOffer offer)
        {
            if (root == null) return;
            if (messageText != null) messageText.text = offer.Message;
            root.SetActive(true);
            _hideAt = Time.unscaledTime + autoHideSeconds;
        }

        private void Hide(bool decline)
        {
            if (decline) _offers?.Decline();
            if (root != null) root.SetActive(false);
        }

        private void Update()
        {
            if (root != null && root.activeSelf && Time.unscaledTime >= _hideAt) Hide(true);
        }
    }
}
