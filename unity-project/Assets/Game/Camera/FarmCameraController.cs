using FarmQuest.Core.Services;
using FarmQuest.Input;
using UnityEngine;

namespace FarmQuest.Camera
{
    /// <summary>
    /// Orthographic isometric-style camera (§53): drag pan, pinch/scroll zoom,
    /// optional player follow, hard bounds from unlocked farm area.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class FarmCameraController : MonoBehaviour
    {
        [Header("Setup")]
        public Transform followTarget;

        [Header("Tuning")]
        public float minZoom = 6f;
        public float maxZoom = 22f;
        public float panSpeed = 1f;
        public float zoomSpeed = 4f;
        public float smoothTime = 0.15f;
        public Rect worldBounds = new Rect(-20f, -20f, 40f, 40f);

        private UnityEngine.Camera _cam;
        private IInputService _input;
        private Vector3 _targetPos;
        private float _targetZoom;
        private Vector3 _velocity;
        // Height/angle offset kept while following (so the camera never
        // descends to the target's ground-level position).
        private Vector3 _followOffset;

        private void Awake()
        {
            _cam = GetComponent<UnityEngine.Camera>();
            _cam.orthographic = true;
            // Classic isometric-ish angle; tweak to taste.
            transform.rotation = Quaternion.Euler(50f, 45f, 0f);
            _targetZoom = _targetPos.z = 0f;
            _targetZoom = _cam.orthographicSize;
            _targetPos = transform.position;
        }

        private void Start()
        {
            _input = ServiceLocator.Get<IInputService>();
            _input.Dragged += OnDragged;
            _input.Pinched += OnPinched;
            if (followTarget != null)
                _followOffset = transform.position - followTarget.position;
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.Dragged -= OnDragged;
                _input.Pinched -= OnPinched;
            }
        }

        private void OnDragged(Vector2 delta)
        {
            if (followTarget != null) return; // follow mode: no manual pan
            // Screen delta → world delta on the ground plane.
            Vector3 a = _cam.ScreenToWorldPoint(new Vector3(0, 0, _cam.nearClipPlane));
            Vector3 b = _cam.ScreenToWorldPoint(new Vector3(delta.x, delta.y, _cam.nearClipPlane));
            Vector3 worldDelta = a - b;
            _targetPos += worldDelta * panSpeed;
            ClampTarget();
        }

        private void OnPinched(float multiplier)
        {
            _targetZoom = Mathf.Clamp(_targetZoom / multiplier, minZoom, maxZoom);
        }

        private void Update()
        {
            float scroll = _input != null ? _input.ScrollDelta : 0f;
            if (Mathf.Abs(scroll) > 0.01f)
                _targetZoom = Mathf.Clamp(_targetZoom - scroll * zoomSpeed * 0.1f, minZoom, maxZoom);

            if (followTarget != null)
            {
                _targetPos = followTarget.position + _followOffset;
                ClampTarget();
            }

            transform.position = Vector3.SmoothDamp(transform.position, _targetPos, ref _velocity, smoothTime);
            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetZoom, 1f - Mathf.Exp(-10f * Time.deltaTime));
        }

        private void ClampTarget()
        {
            _targetPos.x = Mathf.Clamp(_targetPos.x, worldBounds.xMin, worldBounds.xMax);
            _targetPos.z = Mathf.Clamp(_targetPos.z, worldBounds.yMin, worldBounds.yMax);
        }

        public void FocusOn(Vector3 worldPos)
        {
            followTarget = null;
            _targetPos = worldPos;
            ClampTarget();
        }

        public void Follow(Transform target)
        {
            followTarget = target;
            if (target != null)
                _followOffset = transform.position - target.position;
        }
    }
}
