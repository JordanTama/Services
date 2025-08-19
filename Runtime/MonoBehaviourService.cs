using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Services
{
    public abstract class MonoBehaviourService : MonoBehaviour, IServiceAsync
    {
        [SerializeField] private bool selfRegister;
        
        private bool _selfRegistered;
        
        protected virtual void Awake()
        {
            if (!selfRegister)
                return;

            _selfRegistered = true;
            enabled = false;
            Locator.RegisterMonoBehaviour(this).Forget();
        }

        private void OnDestroy()
        {
            if (_selfRegistered)
                Locator.UnregisterMonoBehaviour(this).Forget();
        }

        public virtual UniTask OnRegistered()
        {
            enabled = true;
            return UniTask.CompletedTask;
        }

        public virtual UniTask OnUnregistered()
        {
            enabled = false;
            return UniTask.CompletedTask;
        }

        protected static async UniTask<T> LoadSettings<T>() where T : ServiceSettings
        {
            return await Utility.GetSettings<T>();
        }
    }
}