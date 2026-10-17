using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Services;
using UnityEngine.TestTools;

namespace JordanTama.Services.Editor.Tests
{
    public class LocatorUnregistrationTests : LocatorFixture
    {
        [UnityTest]
        public IEnumerator Unregister_ByType_RemovesService() => UniTask.ToCoroutine(async () =>
        {
            var service = new RootService();
            await Locator.Register(service);

            await Locator.Unregister<RootService>();

            Assert.IsFalse(Locator.IsRegistered<RootService>());
            Assert.IsFalse(Locator.Get(out RootService _));
            CollectionAssert.DoesNotContain(Locator.AllServices, service);
        });

        [UnityTest]
        public IEnumerator Unregister_ByInstance_RemovesService() => UniTask.ToCoroutine(async () =>
        {
            var service = new RootService();
            await Locator.Register(service);

            await Locator.Unregister(service);

            Assert.IsFalse(Locator.IsRegistered<RootService>());
        });

        [UnityTest]
        public IEnumerator Unregister_RegisteredService_InvokesOnUnregisteredOnce() => UniTask.ToCoroutine(async () =>
        {
            var service = new RootService();
            await Locator.Register(service);

            await Locator.Unregister(service);

            Assert.AreEqual(1, service.UnregisteredCalls);
        });

        [UnityTest]
        public IEnumerator Unregister_RegisteredService_RaisesServiceUnregisteredWithType() => UniTask.ToCoroutine(async () =>
        {
            await Locator.Register(new RootService());

            await Locator.Unregister<RootService>();

            CollectionAssert.AreEqual(new[] { typeof(RootService) }, Unregistered);
        });

        [UnityTest]
        public IEnumerator Unregister_RegisteredService_IsRemovedWhenServiceUnregisteredIsRaised() => UniTask.ToCoroutine(async () =>
        {
            await Locator.Register(new RootService());
            bool registeredDuringEvent = true;
            void CheckRegistered(Type _) => registeredDuringEvent = Locator.IsRegistered<RootService>();

            Locator.ServiceUnregistered += CheckRegistered;
            try
            {
                await Locator.Unregister<RootService>();
            }
            finally
            {
                Locator.ServiceUnregistered -= CheckRegistered;
            }

            Assert.IsFalse(registeredDuringEvent);
        });

        [UnityTest]
        public IEnumerator Unregister_WhileOnUnregisteredIsPending_ServiceRemainsRetrievable() => UniTask.ToCoroutine(async () =>
        {
            var gate = Gate();
            var service = new RootService { Unregistering = () => gate.Task };
            await Locator.Register(service);

            var unregistration = Locator.Unregister(service);

            Assert.AreSame(service, Locator.Get<RootService>());
            CollectionAssert.IsEmpty(Unregistered);

            gate.TrySetResult();
            await unregistration;

            Assert.IsFalse(Locator.IsRegistered<RootService>());
        });

        [UnityTest]
        public IEnumerator Unregister_TypeNotRegistered_DoesNothing() => UniTask.ToCoroutine(async () =>
        {
            await Locator.Unregister<RootService>();

            CollectionAssert.IsEmpty(Unregistered);
        });

        [UnityTest]
        public IEnumerator Unregister_Null_DoesNothing() => UniTask.ToCoroutine(async () =>
        {
            await Locator.Register(new RootService());

            await Locator.Unregister<RootService>(null);

            Assert.IsTrue(Locator.IsRegistered<RootService>());
            CollectionAssert.IsEmpty(Unregistered);
        });

        [UnityTest]
        public IEnumerator Unregister_InstanceThatWasNeverRegistered_LeavesRegisteredInstance() => UniTask.ToCoroutine(async () =>
        {
            var registered = new RootService();
            var other = new RootService();
            await Locator.Register(registered);

            await Locator.Unregister(other);

            Assert.IsTrue(Locator.Get(out RootService retrieved));
            Assert.AreSame(registered, retrieved);
            Assert.AreEqual(0, registered.UnregisteredCalls);
            Assert.AreEqual(0, other.UnregisteredCalls);
        });

        [UnityTest]
        public IEnumerator Unregister_CalledConcurrently_UnregistersOnce() => UniTask.ToCoroutine(async () =>
        {
            var gate = Gate();
            var service = new RootService { Unregistering = () => gate.Task };
            await Locator.Register(service);

            var first = Locator.Unregister(service);
            var second = Locator.Unregister(service);
            gate.TrySetResult();
            await UniTask.WhenAll(first, second);

            Assert.AreEqual(1, service.UnregisteredCalls);
            CollectionAssert.AreEqual(new[] { typeof(RootService) }, Unregistered);
        });

        [UnityTest]
        public IEnumerator Unregister_ThenRegisterSameType_RegistersNewInstance() => UniTask.ToCoroutine(async () =>
        {
            await Locator.Register(new RootService());
            await Locator.Unregister<RootService>();
            var replacement = new RootService();

            Assert.IsTrue(await Locator.Register(replacement));
            Assert.AreSame(replacement, Locator.Get<RootService>());
        });

        [UnityTest]
        public IEnumerator Unregister_Dependency_UnregistersDependantsInReverseOrder() => UniTask.ToCoroutine(async () =>
        {
            await Locator.Register(new RootService());
            await Locator.Register(new DependentService { Unregistering = async () => await UniTask.Yield() });
            await Locator.Register(new TransitiveService { Unregistering = async () => await UniTask.Yield() });

            await Locator.Unregister<RootService>();

            CollectionAssert.AreEqual(
                new[] { typeof(TransitiveService), typeof(DependentService), typeof(RootService) },
                Unregistered);
        });

        [UnityTest]
        public IEnumerator Unregister_Dependency_DependantsAreGoneBeforeItsOnUnregistered() => UniTask.ToCoroutine(async () =>
        {
            bool dependantRegisteredDuringTeardown = true;
            await Locator.Register(new RootService
            {
                Unregistering = () =>
                {
                    dependantRegisteredDuringTeardown = Locator.IsRegistered<DependentService>();
                    return UniTask.CompletedTask;
                }
            });
            await Locator.Register(new DependentService());

            await Locator.Unregister<RootService>();

            Assert.IsFalse(dependantRegisteredDuringTeardown);
        });

        [UnityTest]
        public IEnumerator Unregister_Dependency_LeavesUnrelatedServicesRegistered() => UniTask.ToCoroutine(async () =>
        {
            await Locator.Register(new RootService());
            await Locator.Register(new IndependentService());
            await Locator.Register(new DependentService());

            await Locator.Unregister<RootService>();

            Assert.IsTrue(Locator.IsRegistered<IndependentService>());
        });

        [UnityTest]
        public IEnumerator Unregister_Dependant_LeavesDependencyRegistered() => UniTask.ToCoroutine(async () =>
        {
            await Locator.Register(new RootService());
            await Locator.Register(new DependentService());

            await Locator.Unregister<DependentService>();

            Assert.IsTrue(Locator.IsRegistered<RootService>());
        });

        [UnityTest]
        public IEnumerator Unregister_OneOfSeveralDependencies_UnregistersDependant() => UniTask.ToCoroutine(async () =>
        {
            await Locator.Register(new RootService());
            await Locator.Register(new IndependentService());
            await Locator.Register(new MultiDependentService());

            await Locator.Unregister<IndependentService>();

            Assert.IsFalse(Locator.IsRegistered<MultiDependentService>());
            Assert.IsTrue(Locator.IsRegistered<RootService>());
        });
    }
}
