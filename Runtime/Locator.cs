using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Logging;

namespace Services
{
    public static class Locator
    {
        private static readonly Dictionary<string, IService> Services = new();
        
        #region Public Methods

        public static bool Register<T>(T service) where T : IServiceStandard
        {
            if (!RegisterServiceInternal(service))
                return false;
            
            service.OnRegistered();
            return true;
        }

        public static async UniTask<bool> RegisterAsync<T>(T service) where T : IServiceAsync
        {
            if (!RegisterServiceInternal(service))
                return false;
            
            await service.OnRegistered();
            return true;
        }

        public static async UniTask Unregister<T>() where T : IService
        {
            var service = Get<T>();
            await Unregister(service);
        }

        public static async UniTask Unregister<T>(T service) where T : IService
        {
            if (service == null || !IsRegistered(service))
                return;

            // Service is no longer registered, recursively deregister services directly depending on this one
            int numDeregistered = 0;
            do
            {
                bool DependsOn(IService dependant)
                {
                    var attribute = dependant.GetType().GetCustomAttribute<DependsOnServiceAttribute>();
                    return attribute != null && attribute.Dependencies.Any(d => d == service.GetType());
                }
                
                foreach (var other in Services.Values.Where(DependsOn))
                {
                    await Unregister(other);
                    numDeregistered++;
                }
                
            } while (numDeregistered > 0);

            // Now that all dependant services have been deregistered, we can unregister this service
            switch (service)
            {
                case IServiceStandard standard:
                    standard.OnUnregistered();
                    break;
                
                case IServiceAsync async:
                    await async.OnDeregistered();
                    break;
            }

            string key = GetKey(service);
            Services.Remove(key);
        }
        
        public static bool IsRegistered<T>() where T : IService
        {
            string key = GetKey<T>();
            return IsRegistered(key);
        }

        public static bool Get<T>(out T service) where T : IService
        {
            if (!IsRegistered<T>())
            {
                service = default;
                return false;
            }

            service = Get<T>();
            return true;
        }

        public static T Get<T>() where T : IService
        {
            string key = GetKey<T>();
            
            if (IsRegistered(key)) 
                return (T) Services[key];
            
            Logger.Error(nameof(Locator), $"Tried to get {key} but it was not registered.");
            return default;
        }
        
        #endregion

        #region Private Methods

        private static bool RegisterServiceInternal<T>(T service) where T : IService
        {
            if (service == null)
                return false;
            
            if (IsRegistered(service))
                return false;
            
            string key = GetKey(service);

            // Ensure all service dependencies are already registered
            var serviceAttribute = service.GetType().GetCustomAttribute<DependsOnServiceAttribute>();
            if (serviceAttribute != null && !serviceAttribute.Dependencies.Select(GetKey).All(Services.ContainsKey))
            {
                var missing = serviceAttribute.Dependencies.Where(dependency => !IsRegistered(dependency)).ToArray();
                if (missing.Length > 0)
                {
                    Logger.Error(typeof(Locator), $"Tried to register {key} but it was missing dependencies: {string.Join(", ", missing.Select(GetKey))}");
                    return false;
                }
            }

            Services.Add(key, service);
            return true;
        }

        private static string GetKey(IService service)
        {
            return GetKey(service.GetType());
        }

        private static string GetKey<T>() where T : IService
        {
            return GetKey(typeof(T));
        }

        private static string GetKey(Type type)
        {
            return type.AssemblyQualifiedName;
        }

        private static bool IsRegistered(IService service)
        {
            return IsRegistered(service.GetType());
        }
        
        private static bool IsRegistered(Type type)
        {
            string key = GetKey(type);
            return IsRegistered(key);
        }

        private static bool IsRegistered(string key)
        {
            return Services.ContainsKey(key);
        }
        
        #endregion
    }
}