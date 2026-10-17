using System;
using Cysharp.Threading.Tasks;
using Services;

namespace JordanTama.Services.Tests
{
    public class BehaviourService : MonoBehaviourService
    {
        public bool IsReady => Ready;

        public Func<UniTask> Registering { get; set; } = () => UniTask.CompletedTask;

        public override async UniTask OnRegistered()
        {
            await base.OnRegistered();
            await Registering();
        }
    }
}
