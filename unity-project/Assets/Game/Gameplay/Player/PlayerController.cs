using FarmQuest.Core.Save;
using FarmQuest.Core.Services;
using FarmQuest.Input;
using UnityEngine;

namespace FarmQuest.Gameplay.Player
{
    /// <summary>
    /// The farmer avatar (§5 spec): tap-to-move + WASD, simple and readable.
    /// Interactables are handled by dedicated controllers (e.g. FarmInteractionController).
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 5f;
        public float turnSpeed = 10f;
        public LayerMask groundLayer;

        private IInputService _input;
        private Vector3 _moveTarget;
        private bool _hasMoveTarget;
        private Camera.FarmCameraController _camera;
        private bool _controlsEnabled = true;

        /// <summary>Disabled while driving the tractor (Phase 4).</summary>
        public void SetControlsEnabled(bool enabled)
        {
            _controlsEnabled = enabled;
            if (!enabled) _hasMoveTarget = false;
        }

        private void Awake()
        {
            // Register early so PetView and others can find the player in their Start().
            Core.Services.ServiceLocator.Register(this);
        }

        private void Start()
        {
            _input = ServiceLocator.Get<IInputService>();
            _input.Tapped += OnTapped;
            ServiceLocator.Register(this);
            // Bootstrapper restores the save in Awake(), before this Start()
            // runs — so pull our saved position here instead of relying on
            // the coordinator's restore pass.
            if (ServiceLocator.TryGet(out Core.Save.SaveCoordinator coordinator)
                && coordinator.HadSave && coordinator.LastData != null)
            {
                RestoreState(coordinator.LastData.player);
            }
        }

        private void OnDestroy()
        {
            if (_input != null) _input.Tapped -= OnTapped;
        }

        private void OnTapped(TapInfo tap)
        {
            if (!_controlsEnabled) return;
            var cam = _camera != null ? _camera.GetComponent<UnityEngine.Camera>() : UnityEngine.Camera.main;
            if (cam == null) return;
            var ray = cam.ScreenPointToRay(tap.ScreenPosition);
            // Interactables (NPCs, tractor, pond, pet) take priority over movement.
            if (Physics.Raycast(ray, out var hitInteract, 500f))
            {
                var interactable = hitInteract.collider.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    interactable.Interact();
                    return;
                }
            }
            if (Physics.Raycast(ray, out var hit, 500f, groundLayer))
            {
                _moveTarget = hit.point;
                _moveTarget.y = transform.position.y;
                _hasMoveTarget = true;
            }
        }

        private void Update()
        {
            if (!_controlsEnabled) return;
            Vector3 dir = Vector3.zero;
            Vector2 keys = _input != null ? _input.MoveInput : Vector2.zero;
            if (keys.sqrMagnitude > 0.01f)
            {
                _hasMoveTarget = false;
                dir = new Vector3(keys.x, 0f, keys.y).normalized;
            }
            else if (_hasMoveTarget)
            {
                Vector3 to = _moveTarget - transform.position;
                to.y = 0f;
                if (to.magnitude < 0.15f) { _hasMoveTarget = false; }
                else dir = to.normalized;
            }

            if (dir.sqrMagnitude > 0.001f)
            {
                transform.position += dir * moveSpeed * Time.deltaTime;
                Quaternion look = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
            }
        }

        public void BindCamera(Camera.FarmCameraController cam) => _camera = cam;

        // ---------- save ----------
        public PlayerSaveData CaptureState()
        {
            int tutorialStep = 0;
            if (ServiceLocator.TryGet(out Systems.Story.TutorialService tutorial))
                tutorialStep = (int)tutorial.Step;
            return new PlayerSaveData
            {
                playerName = "Farmer",
                posX = transform.position.x,
                posZ = transform.position.z,
                tutorialStep = tutorialStep
                // customization indices persisted in PlayerCustomization
            };
        }

        public void RestoreState(PlayerSaveData data)
        {
            if (data == null) return;
            transform.position = new Vector3(data.posX, 0f, data.posZ);
        }
    }

    /// <summary>Marker for anything the player can tap-to-interact with (NPCs, doors…).</summary>
    public interface IInteractable
    {
        string InteractLabel { get; }
        void Interact();
    }
}
