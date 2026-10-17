using System;
using Cysharp.Threading.Tasks;
using Services;

namespace JordanTama.Services.Editor.Tests
{
    public abstract class FakeService : IService
    {
        public int RegisteredCalls { get; private set; }
        public int UnregisteredCalls { get; private set; }

        public Func<UniTask> Registering { get; set; } = () => UniTask.CompletedTask;
        public Func<UniTask> Unregistering { get; set; } = () => UniTask.CompletedTask;

        public UniTask OnRegistered()
        {
            RegisteredCalls++;
            return Registering();
        }

        public UniTask OnUnregistered()
        {
            UnregisteredCalls++;
            return Unregistering();
        }
    }

    public sealed class RootService : FakeService { }

    public sealed class IndependentService : FakeService { }

    [DependsOnService(typeof(RootService))]
    public sealed class DependentService : FakeService { }

    [DependsOnService(typeof(DependentService))]
    public sealed class TransitiveService : FakeService { }

    [DependsOnService(typeof(RootService), typeof(IndependentService))]
    public sealed class MultiDependentService : FakeService { }

    [DependsOnService(typeof(RootService))]
    public interface IDependsOnRoot : IService { }

    public sealed class InterfaceDependentService : FakeService, IDependsOnRoot { }

    public sealed class SettingsService : FakeService, ISettingsService<FakeSettings>
    {
        public FakeSettings Settings { get; set; }
    }

    public sealed class UnattributedSettings : ServiceSettings { }

    public sealed class UnattributedSettingsService : FakeService, ISettingsService<UnattributedSettings>
    {
        public UnattributedSettings Settings { get; set; }
    }
}
