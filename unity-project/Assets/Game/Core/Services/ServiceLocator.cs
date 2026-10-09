using System;
using System.Collections.Generic;

namespace FarmQuest.Core.Services
{
    /// <summary>
    /// Minimal service locator. Preferred over MonoBehaviour singletons:
    /// services are plain C# classes, constructed in Bootstrapper, and fully
    /// testable without a scene. See ARCHITECTURE.md for rationale.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public static void Register<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            _services[typeof(T)] = service;
        }

        public static void RegisterAs<TInterface>(object implementation) where TInterface : class
        {
            if (implementation == null) throw new ArgumentNullException(nameof(implementation));
            if (!(implementation is TInterface))
                throw new ArgumentException($"{implementation.GetType().Name} does not implement {typeof(TInterface).Name}");
            _services[typeof(TInterface)] = implementation;
        }

        public static T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
                return (T)service;
            throw new InvalidOperationException($"Service '{typeof(T).Name}' is not registered. Was Bootstrapper run?");
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var obj))
            {
                service = (T)obj;
                return true;
            }
            service = null;
            return false;
        }

        /// <summary>For unit tests: wipes all registrations between tests.</summary>
        public static void Clear()
        {
            _services.Clear();
            GameEvents.Reset();
        }
    }
}
