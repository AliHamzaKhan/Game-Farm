using System;
using UnityEngine;

namespace FarmQuest.Input
{
    /// <summary>Desktop provider: mouse tap/drag/scroll + WASD/arrows.</summary>
    public class StandaloneInputProvider : MonoBehaviour, IInputService
    {
        public event Action<TapInfo> Tapped;
        public event Action<Vector2> Dragged;
        public event Action<float> Pinched;
        public event Action<TapInfo> LongPressed;

        public Vector2 MoveInput { get; private set; }
        public float ScrollDelta { get; private set; }

        private const float TapMaxMovement = 8f; // px
        private bool _mouseDown;
        private Vector2 _downPos;
        private Vector2 _lastPos;

        private void Update()
        {
            MoveInput = new Vector2(UnityEngine.Input.GetAxisRaw("Horizontal"),
                                    UnityEngine.Input.GetAxisRaw("Vertical"));
            ScrollDelta = UnityEngine.Input.mouseScrollDelta.y;

            // Pinch via Ctrl+wheel for parity testing.
            if (UnityEngine.Input.GetKey(KeyCode.LeftControl) && Mathf.Abs(ScrollDelta) > 0.01f)
                Pinched?.Invoke(1f + ScrollDelta * 0.05f);

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                _mouseDown = true;
                _downPos = UnityEngine.Input.mousePosition;
                _lastPos = _downPos;
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0) && _mouseDown)
            {
                _mouseDown = false;
                if (Vector2.Distance(UnityEngine.Input.mousePosition, _downPos) <= TapMaxMovement)
                    Tapped?.Invoke(new TapInfo { ScreenPosition = UnityEngine.Input.mousePosition });
            }

            if (_mouseDown && UnityEngine.Input.GetMouseButton(0))
            {
                Vector2 now = UnityEngine.Input.mousePosition;
                Vector2 delta = now - _lastPos;
                _lastPos = now;
                if (delta.sqrMagnitude > 0.01f &&
                    Vector2.Distance(now, _downPos) > TapMaxMovement)
                    Dragged?.Invoke(delta);
            }
        }
    }
}
