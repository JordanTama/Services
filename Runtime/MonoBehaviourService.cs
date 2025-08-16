using UnityEngine;

namespace Services
{
    public abstract class MonoBehaviourService : MonoBehaviour, IServiceStandard
    {
        protected virtual void Awake()
        {
            if (Locator.IsRegistered(this))
            {
                Destroy(gameObject);
                return;
            }
            
            DontDestroyOnLoad(gameObject);
            Locator.Register(this);
        }

        public abstract void OnRegistered();

        public abstract void OnUnregistered();
    }
}