using System;
using UnityEngine;

namespace FarmQuest.Input
{
    public struct TapInfo
    {
        public Vector2 ScreenPosition;
    }

    /// <summary>
    /// Input abstraction (§52): gameplay NEVER touches Unity Touch APIs directly.
    /// Providers: touch (mobile) and mouse/keyboard (desktop).
    /// </summary>
    public interface IInputService
    {
        event Action<TapInfo> Tapped;
        event Action<Vector2> Dragged;      // screen-space delta
        event Action<float> Pinched;        // >1 zoom in, <1 zoom out (multiplier)
        event Action<TapInfo> LongPressed;
        /// <summary>WASD / arrows / virtual joystick. Zero on pure-touch.</summary>
        Vector2 MoveInput { get; }
        /// <summary>Mouse wheel delta; consumed per-frame by camera.</summary>
        float ScrollDelta { get; }
    }
}
