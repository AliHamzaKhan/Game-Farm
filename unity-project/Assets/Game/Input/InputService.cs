using System;
using UnityEngine;

namespace FarmQuest.Input
{
    /// <summary>
    /// Router: attaches the right provider for the platform and forwards its
    /// events. Register in Bootstrapper (or place in scene). Gameplay code only
    /// ever sees IInputService.
    /// </summary>
    public class InputService : MonoBehaviour, IInputService
    {
        public event Action<TapInfo> Tapped;
        public event Action<Vector2> Dragged;
        public event Action<float> Pinched;
        public event Action<TapInfo> LongPressed;

        private IInputService _provider;

        public Vector2 MoveInput => _provider != null ? _provider.MoveInput : Vector2.zero;
        public float ScrollDelta => _provider != null ? _provider.ScrollDelta : 0f;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
#if UNITY_ANDROID || UNITY_IOS
            _provider = gameObject.AddComponent<TouchInputProvider>();
#else
            // Editor/desktop default: mouse+keyboard. Touch can be forced via inspector in future.
            _provider = gameObject.AddComponent<StandaloneInputProvider>();
#endif
            _provider.Tapped += t => Tapped?.Invoke(t);
            _provider.Dragged += d => Dragged?.Invoke(d);
            _provider.Pinched += p => Pinched?.Invoke(p);
            _provider.LongPressed += t => LongPressed?.Invoke(t);

            Core.Services.ServiceLocator.Register<IInputService>(this);
        }
    }
}
