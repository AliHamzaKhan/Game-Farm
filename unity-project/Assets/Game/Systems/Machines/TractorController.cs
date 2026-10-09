using FarmQuest.Core.Services;
using FarmQuest.Gameplay.Player;
using FarmQuest.Input;
using UnityEngine;

namespace FarmQuest.Systems.Machines
{
    /// <summary>
    /// The tractor milestone (§23): arcade driving (tap-to-move + WASD, no
    /// vehicle sim). While driving, work is applied to tiles under the tractor
    /// in the current mode's radius. Camera follows; player controls pause.
    /// </summary>
    public class TractorController : MonoBehaviour
    {
        public float baseSpeed = 6f;
        public LayerMask groundLayer;

        public bool IsDriving { get; private set; }
        public TractorMode Mode { get; private set; } = TractorMode.Plow;
        public string SelectedSeedCropId { get; set; } = "carrot";

        private IInputService _input;
        private Vector3 _moveTarget;
        private bool _hasTarget;
        private Vector2Int _lastCell = new Vector2Int(int.MinValue, int.MinValue);

        private void Awake()
        {
            // Placeholder body if no art assigned yet.
            if (transform.childCount == 0)
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Box);
                body.transform.SetParent(transform, false);
                body.transform.localScale = new Vector3(1.6f, 1f, 2.4f);
                body.transform.localPosition = Vector3.up * 0.5f;
                body.GetComponent<Renderer>().material.color = new Color(0.85f, 0.25f, 0.2f);
            }
        }

        private void Start()
        {
            _input = ServiceLocator.Get<IInputService>();
            _input.Tapped += OnTapped;
        }

        private void OnDestroy()
        {
            if (_input != null) _input.Tapped -= OnTapped;
        }

        public void StartDriving()
        {
            var equipment = ServiceLocator.Get<EquipmentService>();
            if (!equipment.HasTractor())
            {
                GameEvents.RaiseToast("You don't own a tractor yet!");
                return;
            }
            IsDriving = true;
            _hasTarget = false;
            var player = FindObjectOfType<PlayerController>();
            if (player != null) player.SetControlsEnabled(false);
            var cam = FindObjectOfType<Camera.FarmCameraController>();
            if (cam != null) cam.Follow(transform);
            var panel = FindObjectOfType<UI.TractorPanelController>();
            if (panel != null) panel.Show();
            GameEvents.RaiseToast("🚜 Driving! Tap the ground to steer.");
        }

        public void StopDriving()
        {
            IsDriving = false;
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                player.SetControlsEnabled(true);
                var cam = FindObjectOfType<Camera.FarmCameraController>();
                if (cam != null) cam.Follow(player.transform);
            }
            var panel = FindObjectOfType<UI.TractorPanelController>();
            if (panel != null) panel.Hide();
        }

        public bool SetMode(TractorMode mode)
        {
            var equipment = ServiceLocator.Get<EquipmentService>();
            if (!equipment.IsModeUnlocked(mode))
            {
                GameEvents.RaiseToast($"Mode locked — check the equipment shop! (needs {mode})");
                return false;
            }
            Mode = mode;
            GameEvents.RaiseToast($"🚜 Mode: {mode}");
            return true;
        }

        private void OnTapped(Input.TapInfo tap)
        {
            if (!IsDriving) return;
            var cam = UnityEngine.Camera.main;
            if (cam == null) return;
            var ray = cam.ScreenPointToRay(tap.ScreenPosition);
            if (Physics.Raycast(ray, out var hit, 500f, groundLayer))
            {
                _moveTarget = hit.point;
                _moveTarget.y = transform.position.y;
                _hasTarget = true;
            }
        }

        private void Update()
        {
            if (!IsDriving) return;

            var equipment = ServiceLocator.Get<EquipmentService>();
            var tractor = equipment.GetBestTractor();
            float speed = baseSpeed * (tractor != null ? tractor.speedMultiplier : 1f);

            Vector3 dir = Vector3.zero;
            Vector2 keys = _input != null ? _input.MoveInput : Vector2.zero;
            if (keys.sqrMagnitude > 0.01f)
            {
                _hasTarget = false;
                dir = new Vector3(keys.x, 0f, keys.y).normalized;
            }
            else if (_hasTarget)
            {
                Vector3 to = _moveTarget - transform.position;
                to.y = 0f;
                if (to.magnitude < 0.4f) _hasTarget = false;
                else dir = to.normalized;
            }

            if (dir.sqrMagnitude > 0.001f)
            {
                transform.position += dir * speed * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(dir), 6f * Time.deltaTime);
            }

            // Apply work when entering a new grid cell.
            var farm = ServiceLocator.Get<Farming.FarmService>();
            var cell = farm.WorldToGrid(transform.position);
            if (cell != _lastCell)
            {
                _lastCell = cell;
                var work = new TractorWorkService(farm);
                int radius = equipment.ModeRadius(Mode);
                var result = work.DoWork(cell, radius, Mode, SelectedSeedCropId);
                if (result.harvestQty > 0)
                    GameEvents.RaiseToast($"🚜 Harvested {result.harvestQty}!");
            }
        }
    }
}
