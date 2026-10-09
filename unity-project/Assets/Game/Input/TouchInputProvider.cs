using System;
using UnityEngine;

namespace FarmQuest.Input
{
    /// <summary>Mobile provider: tap, drag, pinch, long-press from raw touches.</summary>
    public class TouchInputProvider : MonoBehaviour, IInputService
    {
        public event Action<TapInfo> Tapped;
        public event Action<Vector2> Dragged;
        public event Action<float> Pinched;
        public event Action<TapInfo> LongPressed;

        public Vector2 MoveInput => Vector2.zero;
        public float ScrollDelta => 0f;

        private const float TapMaxDuration = 0.35f;
        private const float TapMaxMovement = 24f; // px
        private const float LongPressDuration = 0.6f;

        private int _trackedFinger = -1;
        private Vector2 _downPos;
        private float _downTime;
        private bool _longPressFired;
        private float _pinchStartDist;

        private void Update()
        {
            if (UnityEngine.Input.touchCount == 2)
            {
                HandlePinch();
                _trackedFinger = -1;
                return;
            }
            if (UnityEngine.Input.touchCount != 1) { _trackedFinger = -1; return; }

            var touch = UnityEngine.Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    _trackedFinger = touch.fingerId;
                    _downPos = touch.position;
                    _downTime = Time.time;
                    _longPressFired = false;
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (touch.fingerId != _trackedFinger) break;
                    if (!_longPressFired && Time.time - _downTime > LongPressDuration &&
                        Vector2.Distance(touch.position, _downPos) < TapMaxMovement)
                    {
                        _longPressFired = true;
                        LongPressed?.Invoke(new TapInfo { ScreenPosition = touch.position });
                    }
                    if (Vector2.Distance(touch.position, _downPos) > TapMaxMovement)
                        Dragged?.Invoke(touch.deltaPosition);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (touch.fingerId != _trackedFinger) break;
                    float duration = Time.time - _downTime;
                    float moved = Vector2.Distance(touch.position, _downPos);
                    if (!_longPressFired && duration <= TapMaxDuration && moved <= TapMaxMovement)
                        Tapped?.Invoke(new TapInfo { ScreenPosition = touch.position });
                    _trackedFinger = -1;
                    break;
            }
        }

        private void HandlePinch()
        {
            var t0 = UnityEngine.Input.GetTouch(0);
            var t1 = UnityEngine.Input.GetTouch(1);
            float dist = Vector2.Distance(t0.position, t1.position);
            if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
            {
                _pinchStartDist = dist;
                return;
            }
            if (_pinchStartDist > 0f && dist > 0f)
                Pinched?.Invoke(dist / _pinchStartDist);
            _pinchStartDist = dist;
        }
    }
}
