using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Services;
using UnityEngine.TestTools;

namespace JordanTama.Services.Editor.Tests
{
    public abstract class LocatorFixture
    {
        private readonly List<UniTaskCompletionSource> _gates = new();

        protected readonly List<IService> Registered = new();
        protected readonly List<Type> Unregistered = new();

        [SetUp]
        public void RecordLocatorEvents()
        {
            Locator.ServiceRegistered += Registered.Add;
            Locator.ServiceUnregistered += Unregistered.Add;
        }

        [UnityTearDown]
        public IEnumerator ResetLocator()
        {
            yield return UniTask.ToCoroutine(UnregisterTestServices);
        }

        private async UniTask UnregisterTestServices()
        {
            Locator.ServiceRegistered -= Registered.Add;
            Locator.ServiceUnregistered -= Unregistered.Add;

            foreach (var gate in _gates)
                gate.TrySetResult();

            _gates.Clear();
            await UniTask.Yield();

            var services = Locator.AllServices.Where(IsTestService).Reverse().ToList();
            foreach (var fake in services.OfType<FakeService>())
                fake.Unregistering = () => UniTask.CompletedTask;

            foreach (var service in services)
                await Locator.Unregister(service);

            Registered.Clear();
            Unregistered.Clear();
        }

        protected UniTaskCompletionSource Gate()
        {
            var gate = new UniTaskCompletionSource();
            _gates.Add(gate);
            return gate;
        }

        protected static async UniTask<Exception> Capture(Func<UniTask> action)
        {
            try
            {
                await action();
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        private static bool IsTestService(IService service)
        {
            return service.GetType().Assembly == typeof(LocatorFixture).Assembly;
        }
    }
}
