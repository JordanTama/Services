using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Services;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace JordanTama.Services.Tests
{
    public class MonoBehaviourServiceTests
    {
        [UnityTearDown]
        public IEnumerator ResetLocator()
        {
            yield return UniTask.ToCoroutine(UnregisterTestServices);
        }

        private static async UniTask UnregisterTestServices()
        {
            var services = Locator.AllServices.Where(IsTestService).Reverse().ToList();
            foreach (var service in services)
                await Locator.Unregister(service);

            var behaviours = Object.FindObjectsByType<MonoBehaviourService>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var behaviour in behaviours.Where(IsTestService))
                Object.Destroy(behaviour.gameObject);

            await UniTask.NextFrame();
        }

        [UnityTest]
        public IEnumerator RegisterMonoBehaviourByType_CreatesGameObjectNamedAfterService() => UniTask.ToCoroutine(async () =>
        {
            Assert.IsTrue(await Locator.RegisterMonoBehaviour<BehaviourService>());

            var service = Locator.Get<BehaviourService>();
            Assert.IsTrue(service);
            Assert.AreEqual(nameof(BehaviourService), service.gameObject.name);
        });

        [UnityTest]
        public IEnumerator RegisterMonoBehaviour_MovesGameObjectToDontDestroyOnLoad() => UniTask.ToCoroutine(async () =>
        {
            var service = new GameObject().AddComponent<BehaviourService>();

            await Locator.RegisterMonoBehaviour(service);

            Assert.AreEqual("DontDestroyOnLoad", service.gameObject.scene.name);
        });

        [UnityTest]
        public IEnumerator RegisterMonoBehaviour_MarksServiceReady() => UniTask.ToCoroutine(async () =>
        {
            await Locator.RegisterMonoBehaviour<BehaviourService>();

            Assert.IsTrue(Locator.Get<BehaviourService>().IsReady);
        });

        [UnityTest]
        public IEnumerator RegisterMonoBehaviour_TypeAlreadyRegistered_DestroysDuplicateAndKeepsOriginal() => UniTask.ToCoroutine(async () =>
        {
            await Locator.RegisterMonoBehaviour<BehaviourService>();
            var original = Locator.Get<BehaviourService>();
            var duplicate = new GameObject().AddComponent<BehaviourService>();

            Assert.IsFalse(await Locator.RegisterMonoBehaviour(duplicate));
            await UniTask.NextFrame();

            Assert.IsTrue(duplicate == null);
            Assert.IsTrue(original);
            Assert.AreSame(original, Locator.Get<BehaviourService>());
        });

        [UnityTest]
        public IEnumerator RegisterMonoBehaviour_DependencyMissing_ReturnsFalseAndDestroysGameObject() => UniTask.ToCoroutine(async () =>
        {
            var dependant = new GameObject().AddComponent<DependentBehaviourService>();
            LogAssert.Expect(LogType.Error, new Regex($@"missing dependencies: \S*\.{nameof(BehaviourService)}$"));

            Assert.IsFalse(await Locator.RegisterMonoBehaviour(dependant));
            await UniTask.NextFrame();

            Assert.IsTrue(dependant == null);
        });

        [UnityTest]
        public IEnumerator RegisterMonoBehaviour_OnRegisteredThrows_RethrowsAndDestroysGameObject() => UniTask.ToCoroutine(async () =>
        {
            var service = new GameObject().AddComponent<BehaviourService>();
            service.Registering = () => UniTask.FromException(new InvalidOperationException());

            Exception exception = null;
            try
            {
                await Locator.RegisterMonoBehaviour(service);
            }
            catch (Exception caught)
            {
                exception = caught;
            }
            await UniTask.NextFrame();

            Assert.IsInstanceOf<InvalidOperationException>(exception);
            Assert.IsFalse(Locator.IsRegistered<BehaviourService>());
            Assert.IsTrue(service == null);
        });

        [UnityTest]
        public IEnumerator Unregister_MonoBehaviourService_ClearsReadyAndDestroysGameObject() => UniTask.ToCoroutine(async () =>
        {
            await Locator.RegisterMonoBehaviour<BehaviourService>();
            var service = Locator.Get<BehaviourService>();

            await Locator.Unregister(service);

            Assert.IsFalse(service.IsReady);
            await UniTask.NextFrame();
            Assert.IsTrue(service == null);
        });

        [UnityTest]
        public IEnumerator UnregisterMonoBehaviourByType_UnregistersAndDestroysGameObject() => UniTask.ToCoroutine(async () =>
        {
            await Locator.RegisterMonoBehaviour<BehaviourService>();
            var service = Locator.Get<BehaviourService>();

            await Locator.UnregisterMonoBehaviour<BehaviourService>();

            Assert.IsFalse(Locator.IsRegistered<BehaviourService>());
            Assert.IsTrue(service == null);
        });

        [UnityTest]
        public IEnumerator UnregisterMonoBehaviourByType_TypeNotRegistered_DoesNothing() => UniTask.ToCoroutine(async () =>
        {
            await Locator.UnregisterMonoBehaviour<BehaviourService>();

            Assert.IsFalse(Locator.IsRegistered<BehaviourService>());
        });

        [UnityTest]
        public IEnumerator UnregisterMonoBehaviour_Dependency_DestroysDependant() => UniTask.ToCoroutine(async () =>
        {
            await Locator.RegisterMonoBehaviour<BehaviourService>();
            await Locator.RegisterMonoBehaviour<DependentBehaviourService>();
            var dependant = Locator.Get<DependentBehaviourService>();

            await Locator.UnregisterMonoBehaviour<BehaviourService>();

            Assert.IsFalse(Locator.IsRegistered<DependentBehaviourService>());
            Assert.IsTrue(dependant == null);
        });

        [UnityTest]
        public IEnumerator SelfRegister_Enabled_RegistersOnAwake() => UniTask.ToCoroutine(async () =>
        {
            var service = CreateSelfRegistering<BehaviourService>();
            await UniTask.NextFrame();

            Assert.AreSame(service, Locator.Get<BehaviourService>());
        });

        [UnityTest]
        public IEnumerator SelfRegister_Disabled_DoesNotRegisterOnAwake() => UniTask.ToCoroutine(async () =>
        {
            new GameObject().AddComponent<BehaviourService>();
            await UniTask.NextFrame();

            Assert.IsFalse(Locator.IsRegistered<BehaviourService>());
        });

        [UnityTest]
        public IEnumerator SelfRegister_GameObjectDestroyed_Unregisters() => UniTask.ToCoroutine(async () =>
        {
            var service = CreateSelfRegistering<BehaviourService>();
            await UniTask.NextFrame();

            Object.Destroy(service.gameObject);
            await UniTask.NextFrame();

            Assert.IsFalse(Locator.IsRegistered<BehaviourService>());
        });

        [UnityTest]
        public IEnumerator SelfRegister_DuplicateInstance_IsDestroyedWhileOriginalStaysRegistered() => UniTask.ToCoroutine(async () =>
        {
            var original = CreateSelfRegistering<BehaviourService>();
            await UniTask.NextFrame();

            var duplicate = CreateSelfRegistering<BehaviourService>();
            await UniTask.NextFrame();
            await UniTask.NextFrame();

            Assert.IsTrue(duplicate == null);
            Assert.IsTrue(original);
            Assert.IsTrue(Locator.Get(out BehaviourService registered));
            Assert.AreSame(original, registered);
        });

        private static T CreateSelfRegistering<T>() where T : MonoBehaviourService
        {
            var gameObject = new GameObject(typeof(T).Name);
            gameObject.SetActive(false);

            var service = gameObject.AddComponent<T>();
            JsonUtility.FromJsonOverwrite("{\"selfRegister\":true}", service);

            gameObject.SetActive(true);
            return service;
        }

        private static bool IsTestService(object service)
        {
            return service.GetType().Assembly == typeof(MonoBehaviourServiceTests).Assembly;
        }
    }
}
