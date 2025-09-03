using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Services
{
    public abstract class MonoBehaviourService : MonoBehaviour, IServiceAsync
    {
        [SerializeField] private bool selfRegister;
        
        private bool _selfRegistered;
        
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

        protected static async UniTask<T> LoadSettings<T>() where T : ServiceSettings
        {
            return await Utility.GetSettings<T>();
        }
    }
}