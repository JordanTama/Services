using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Services
{
    public static class Locator
    {
        private static readonly Dictionary<string, IService> Services = new();
        private static readonly HashSet<string> Pending = new();
        private static readonly List<string> RegistrationOrder = new();
        private static readonly Dictionary<string, AsyncOperationHandle> SettingsHandles = new();

        public static event Action<IService> ServiceRegistered;
        public static event Action<Type> ServiceUnregistered;

        public static List<IService> AllServices => Services.Values.ToList();
        
        #region Public Methods

        public static UniTask<bool> Register<T>(T service) where T : IService
        {
            return RegisterInternal(service, () => service.OnRegistered());
        }

        public static UniTask<bool> Register<TSettings>(ISettingsService<TSettings> service) where TSettings : ServiceSettings
        {
            return RegisterInternal(service, async () =>
            {
                var (settings, handle) = await Utility.GetSettings<TSettings>();
                if (handle.IsValid())
                    SettingsHandles[GetKey(service)] = handle;

                service.Settings = settings;
                await service.OnRegistered();
            });
        }

        public static UniTask<bool> RegisterMonoBehaviour<T>() where T : MonoBehaviourService
        {
            var gameObject = new GameObject(typeof(T).Name);
            var service = (T) gameObject.AddComponent(typeof(T));
            return RegisterMonoBehaviour(service);
        }

        public static async UniTask<bool> RegisterMonoBehaviour<T>(T service) where T : MonoBehaviourService
        {
            bool registered = false;
            try
            {
                registered = await RegisterInternal(service, () =>
                {
                    Object.DontDestroyOnLoad(service.gameObject);
                    return service.OnRegistered();
                });
            }
            finally
            {
                if (!registered && service)
                    Object.Destroy(service.gameObject);
            }

            return registered;
        }

        public static async UniTask UnregisterMonoBehaviour<T>() where T : MonoBehaviourService
        {
            if (Get<T>(out var service))
                await UnregisterMonoBehaviour(service);
        }

        public static async UniTask UnregisterMonoBehaviour<T>(T service) where T : MonoBehaviourService
        {
            await Unregister(service);

            if (service)
                Object.Destroy(service.gameObject);

            await UniTask.NextFrame();
        }

        public static async UniTask Unregister<T>() where T : IService
        {
            if (Get<T>(out var service))
                await Unregister(service);
        }

        public static async UniTask Unregister<T>(T service) where T : IService
        {
            if (service == null || !IsRegistered(service.GetType()))
                return;

            // Service is no longer registered, recursively deregister services directly depending on this one
            IService dependant;
            while ((dependant = Services.Values.FirstOrDefault(DependsOn)) != null)
                await Unregister(dependant);

            // Now that all dependant services have been deregistered, we can unregister this service
            await service.OnUnregistered();

            string key = GetKey(service);
            Services.Remove(key);
            RegistrationOrder.Remove(key);
            ReleaseSettings(key);

            if (service is MonoBehaviourService behaviour && behaviour)
                Object.Destroy(behaviour.gameObject);

            ServiceUnregistered?.Invoke(service.GetType());
            return;

            bool DependsOn(IService other)
            {
                var attribute = other.GetType().GetCustomAttribute<DependsOnServiceAttribute>();
                return attribute != null && attribute.Dependencies.Any(d => d == service.GetType());
            }
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
            
            Debug.LogError($"Tried to get {key} but it was not registered.");
            return default;
        }
        
        #endregion

        #region Lifecycle

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            foreach (var handle in SettingsHandles.Values.Where(handle => handle.IsValid()))
                Addressables.Release(handle);

            Services.Clear();
            Pending.Clear();
            RegistrationOrder.Clear();
            SettingsHandles.Clear();
        }

        private static async UniTask UnregisterAll()
        {
            while (RegistrationOrder.Count > 0)
                await Unregister(Services[RegistrationOrder[^1]]);
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void ListenForPlayModeExit()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode && !ReloadsDomainOnPlay())
                UnregisterAll().Forget();
        }

        private static bool ReloadsDomainOnPlay()
        {
            return !EditorSettings.enterPlayModeOptionsEnabled
                   || (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) == 0;
        }
#endif

        #endregion

        #region Private Methods

        private static async UniTask<bool> RegisterInternal(IService service, Func<UniTask> initialise)
        {
            if (!TryReserve(service))
                return false;

            string key = GetKey(service);
            try
            {
                await initialise();
            }
            catch
            {
                ReleaseSettings(key);
                throw;
            }
            finally
            {
                Pending.Remove(key);
            }

            Services.Add(key, service);
            RegistrationOrder.Add(key);
            ServiceRegistered?.Invoke(service);
            return true;
        }

        private static bool TryReserve(IService service)
        {
            if (service == null)
                return false;

            string key = GetKey(service);
            if (IsRegistered(key) || Pending.Contains(key))
                return false;

            // Ensure all service dependencies are already registered
            var serviceAttribute = service.GetType().GetCustomAttribute<DependsOnServiceAttribute>();
            var missing = serviceAttribute?.Dependencies.Where(dependency => !IsRegistered(dependency)).ToArray();
            if (missing is { Length: > 0 })
            {
                Debug.LogError($"Tried to register {key} but it was missing dependencies: {string.Join(", ", missing.Select(GetKey))}");
                return false;
            }

            Pending.Add(key);
            return true;
        }

        private static void ReleaseSettings(string key)
        {
            if (SettingsHandles.Remove(key, out var handle))
                Addressables.Release(handle);
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
            return type.FullName;
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
