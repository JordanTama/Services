using System;
using System.Collections;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Services;
using UnityEngine;
using UnityEngine.TestTools;

namespace JordanTama.Services.Editor.Tests
{
    public class LocatorRegistrationTests : LocatorFixture
    {
        [UnityTest]
        public IEnumerator Register_NewService_ReturnsTrue() => UniTask.ToCoroutine(async () =>
        {
            Assert.IsTrue(await Locator.Register(new RootService()));
        });

        [UnityTest]
        public IEnumerator Register_NewService_InvokesOnRegisteredOnce() => UniTask.ToCoroutine(async () =>
        {
            var service = new RootService();

            await Locator.Register(service);

            Assert.AreEqual(1, service.RegisteredCalls);
            Assert.AreEqual(0, service.UnregisteredCalls);
        });

        [UnityTest]
        public IEnumerator Register_NewService_IsRetrievableByItsType() => UniTask.ToCoroutine(async () =>
        {
            var service = new RootService();

            await Locator.Register(service);

            Assert.IsTrue(Locator.IsRegistered<RootService>());
            Assert.AreSame(service, Locator.Get<RootService>());
            Assert.IsTrue(Locator.Get(out RootService retrieved));
            Assert.AreSame(service, retrieved);
        });

        [UnityTest]
        public IEnumerator Register_NewService_AppearsInAllServices() => UniTask.ToCoroutine(async () =>
        {
            var service = new RootService();

            await Locator.Register(service);

            CollectionAssert.Contains(Locator.AllServices, service);
        });

        [UnityTest]
        public IEnumerator AllServices_ModifyingReturnedList_DoesNotAffectLocator() => UniTask.ToCoroutine(async () =>
        {
            var service = new RootService();
            await Locator.Register(service);

            Locator.AllServices.Clear();

            CollectionAssert.Contains(Locator.AllServices, service);
            Assert.IsTrue(Locator.IsRegistered<RootService>());
        });

        [UnityTest]
        public IEnumerator Register_NewService_RaisesServiceRegisteredWithInstance() => UniTask.ToCoroutine(async () =>
        {
            var service = new RootService();

            await Locator.Register(service);

            CollectionAssert.AreEqual(new IService[] { service }, Registered);
        });

        [UnityTest]
        public IEnumerator Register_NewService_IsRetrievableWhenServiceRegisteredIsRaised() => UniTask.ToCoroutine(async () =>
        {
            bool retrievableDuringEvent = false;
            void CheckRetrievable(IService _) => retrievableDuringEvent = Locator.IsRegistered<RootService>();

            Locator.ServiceRegistered += CheckRetrievable;
            try
            {
                await Locator.Register(new RootService());
            }
            finally
            {
                Locator.ServiceRegistered -= CheckRetrievable;
            }

            Assert.IsTrue(retrievableDuringEvent);
        });

        [UnityTest]
        public IEnumerator Register_Null_ReturnsFalse() => UniTask.ToCoroutine(async () =>
        {
            Assert.IsFalse(await Locator.Register<RootService>(null));
            Assert.IsFalse(Locator.IsRegistered<RootService>());
            CollectionAssert.IsEmpty(Registered);
        });

        [UnityTest]
        public IEnumerator Register_TypeAlreadyRegistered_ReturnsFalseAndKeepsOriginal() => UniTask.ToCoroutine(async () =>
        {
            var original = new RootService();
            await Locator.Register(original);

            Assert.IsFalse(await Locator.Register(new RootService()));
            Assert.AreSame(original, Locator.Get<RootService>());
        });

        [UnityTest]
        public IEnumerator Register_TypeAlreadyRegistered_DoesNotInitialiseOrAnnounceDuplicate() => UniTask.ToCoroutine(async () =>
        {
            var original = new RootService();
            var duplicate = new RootService();
            await Locator.Register(original);

            await Locator.Register(duplicate);

            Assert.AreEqual(0, duplicate.RegisteredCalls);
            CollectionAssert.AreEqual(new IService[] { original }, Registered);
        });

        [UnityTest]
        public IEnumerator Register_SameInstanceTwice_InitialisesOnce() => UniTask.ToCoroutine(async () =>
        {
            var service = new RootService();

            Assert.IsTrue(await Locator.Register(service));
            Assert.IsFalse(await Locator.Register(service));

            Assert.AreEqual(1, service.RegisteredCalls);
        });

        [UnityTest]
        public IEnumerator Register_WhileOnRegisteredIsPending_IsNotYetRegistered() => UniTask.ToCoroutine(async () =>
        {
            var gate = Gate();
            var service = new RootService { Registering = () => gate.Task };

            var registration = Locator.Register(service);

            Assert.IsFalse(Locator.IsRegistered<RootService>());
            CollectionAssert.IsEmpty(Registered);

            gate.TrySetResult();

            Assert.IsTrue(await registration);
            Assert.IsTrue(Locator.IsRegistered<RootService>());
        });

        [UnityTest]
        public IEnumerator Register_SameTypeWhilePending_ReturnsFalse() => UniTask.ToCoroutine(async () =>
        {
            var gate = Gate();
            var first = new RootService { Registering = () => gate.Task };
            var duplicate = new RootService();
            var registration = Locator.Register(first);

            Assert.IsFalse(await Locator.Register(duplicate));

            gate.TrySetResult();
            await registration;

            Assert.AreEqual(0, duplicate.RegisteredCalls);
            Assert.AreSame(first, Locator.Get<RootService>());
        });

        [UnityTest]
        public IEnumerator Register_AsynchronousOnRegistered_CompletesRegistration() => UniTask.ToCoroutine(async () =>
        {
            var service = new RootService { Registering = async () => await UniTask.Yield() };

            Assert.IsTrue(await Locator.Register(service));
            Assert.AreSame(service, Locator.Get<RootService>());
        });

        [UnityTest]
        public IEnumerator Register_OnRegisteredThrows_PropagatesException() => UniTask.ToCoroutine(async () =>
        {
            var service = new RootService { Registering = () => UniTask.FromException(new InvalidOperationException()) };

            var exception = await Capture(() => Locator.Register(service));

            Assert.IsInstanceOf<InvalidOperationException>(exception);
        });

        [UnityTest]
        public IEnumerator Register_OnRegisteredThrows_DoesNotRegisterOrAnnounce() => UniTask.ToCoroutine(async () =>
        {
            var service = new RootService { Registering = () => UniTask.FromException(new InvalidOperationException()) };

            await Capture(() => Locator.Register(service));

            Assert.IsFalse(Locator.IsRegistered<RootService>());
            CollectionAssert.IsEmpty(Registered);
        });

        [UnityTest]
        public IEnumerator Register_AfterFailedRegistration_CanRegisterAgain() => UniTask.ToCoroutine(async () =>
        {
            var service = new RootService { Registering = () => UniTask.FromException(new InvalidOperationException()) };
            await Capture(() => Locator.Register(service));

            service.Registering = () => UniTask.CompletedTask;

            Assert.IsTrue(await Locator.Register(service));
        });

        [Test]
        public void IsRegistered_UnregisteredType_ReturnsFalse()
        {
            Assert.IsFalse(Locator.IsRegistered<RootService>());
        }

        [Test]
        public void Get_UnregisteredType_ReturnsNullAndLogsError()
        {
            LogAssert.Expect(LogType.Error, new Regex($@"Tried to get \S*\.{nameof(RootService)} but it was not registered"));

            Assert.IsNull(Locator.Get<RootService>());
        }

        [Test]
        public void TryGet_UnregisteredType_ReturnsFalseWithoutLogging()
        {
            Assert.IsFalse(Locator.Get(out RootService service));
            Assert.IsNull(service);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
