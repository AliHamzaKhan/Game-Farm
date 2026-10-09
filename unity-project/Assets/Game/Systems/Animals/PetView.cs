using FarmQuest.Core.Services;
using FarmQuest.Gameplay.Player;
using UnityEngine;

namespace FarmQuest.Systems.Animals
{
    /// <summary>
    /// VIEW: the active pet follows the player, idles, sleeps at night.
    /// Tap the pet for a happy bounce. Pure presentation (§22).
    /// </summary>
    public class PetView : MonoBehaviour, IInteractable
    {
        public float followDistance = 2.5f;
        public float moveSpeed = 4.5f;
        public GameObject sleepIcon;

        private Transform _player;
        private float _bounceT = -1f;
        private Vector3 _baseScale;

        public string InteractLabel => "Pet";

        private void Start()
        {
            _baseScale = transform.localScale;
            if (ServiceLocator.TryGet(out PlayerController player))
                _player = player.transform;
            if (sleepIcon != null) sleepIcon.SetActive(false);
        }

        public void Interact()
        {
            _bounceT = 0f; // happy bounce
            GameEvents.RaiseToast("💕");
        }

        private static bool IsNightNow()
        {
            int h = System.DateTime.Now.Hour;
            return h < 6 || h >= 21;
        }

        private void Update()
        {
            bool sleeping = IsNightNow();
            if (sleepIcon != null) sleepIcon.SetActive(sleeping);
            if (sleeping || _player == null) return;

            Vector3 toPlayer = _player.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;

            if (dist > followDistance)
            {
                Vector3 dir = toPlayer / dist;
                transform.position += dir * moveSpeed * Time.deltaTime;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(dir), 8f * Time.deltaTime);
                // little hop while running
                transform.position += Vector3.up * Mathf.Abs(Mathf.Sin(Time.time * 10f)) * 0.15f * Time.deltaTime * 10f;
            }

            if (_bounceT >= 0f)
            {
                _bounceT += Time.deltaTime * 3f;
                float s = 1f + Mathf.Sin(Mathf.Min(1f, _bounceT) * Mathf.PI) * 0.3f;
                transform.localScale = _baseScale * s;
                if (_bounceT >= 1f) { _bounceT = -1f; transform.localScale = _baseScale; }
            }
        }
    }
}
