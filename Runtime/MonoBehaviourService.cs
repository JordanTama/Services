using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Services
{
    public abstract class MonoBehaviourService : MonoBehaviour, IService
    {
        [SerializeField] private bool selfRegister;
        
        private bool _selfRegistered;
        private readonly List<AsyncOperationHandle> _settingsHandles = new();
        
        protected bool Ready { get; private set; }
        
        protected virtual void Awake()
        {
            if (!selfRegister)
                return;

            _selfRegistered = true;
            Locator.RegisterMonoBehaviour(this).Forget();
        }

        private void OnDestroy()
        {
            if (_selfRegistered)
                Locator.UnregisterMonoBehaviour(this).Forget();

            foreach (var handle in _settingsHandles)
                Addressables.Release(handle);

            _settingsHandles.Clear();
        }

        public virtual UniTask OnRegistered()
        {
            Ready = true;
            return UniTask.CompletedTask;
        }

        public virtual UniTask OnUnregistered()
        {
            Ready = false;
            return UniTask.CompletedTask;
        }

        protected async UniTask<T> LoadSettings<T>() where T : ServiceSettings
        {
            var (settings, handle) = await Utility.GetSettings<T>();
            if (handle.IsValid())
                _settingsHandles.Add(handle);

            return settings;
        }
    }
}