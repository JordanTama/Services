using System.Collections;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Services;
using UnityEngine;
using UnityEngine.TestTools;

namespace JordanTama.Services.Editor.Tests
{
    public class LocatorDependencyTests : LocatorFixture
    {
        [UnityTest]
        public IEnumerator Register_DependencyMissing_ReturnsFalseAndLogsMissingDependency() => UniTask.ToCoroutine(async () =>
        {
            var dependant = new DependentService();
            LogAssert.Expect(LogType.Error, MissingDependencies(nameof(RootService)));

            Assert.IsFalse(await Locator.Register(dependant));
            Assert.IsFalse(Locator.IsRegistered<DependentService>());
            Assert.AreEqual(0, dependant.RegisteredCalls);
            CollectionAssert.IsEmpty(Registered);
        });

        [UnityTest]
        public IEnumerator Register_DependencyRegistered_Succeeds() => UniTask.ToCoroutine(async () =>
        {
            await Locator.Register(new RootService());

            Assert.IsTrue(await Locator.Register(new DependentService()));
        });

        [UnityTest]
        public IEnumerator Register_DependencyChainInOrder_Succeeds() => UniTask.ToCoroutine(async () =>
        {
            Assert.IsTrue(await Locator.Register(new RootService()));
            Assert.IsTrue(await Locator.Register(new DependentService()));
            Assert.IsTrue(await Locator.Register(new TransitiveService()));
        });

        [UnityTest]
        public IEnumerator Register_SomeDependenciesMissing_LogsOnlyMissingOnes() => UniTask.ToCoroutine(async () =>
        {
            await Locator.Register(new RootService());
            LogAssert.Expect(LogType.Error, MissingDependencies(nameof(IndependentService)));

            Assert.IsFalse(await Locator.Register(new MultiDependentService()));
        });

        [UnityTest]
        public IEnumerator Register_AllDependenciesRegistered_Succeeds() => UniTask.ToCoroutine(async () =>
        {
            await Locator.Register(new RootService());
            await Locator.Register(new IndependentService());

            Assert.IsTrue(await Locator.Register(new MultiDependentService()));
        });

        [UnityTest]
        public IEnumerator Register_DependencyStillRegistering_IsRejected() => UniTask.ToCoroutine(async () =>
        {
            var gate = Gate();
            var registration = Locator.Register(new RootService { Registering = () => gate.Task });
            LogAssert.Expect(LogType.Error, MissingDependencies(nameof(RootService)));

            Assert.IsFalse(await Locator.Register(new DependentService()));

            gate.TrySetResult();
            await registration;
        });

        [UnityTest]
        public IEnumerator Register_DependencyDeclaredOnInterface_IsEnforced() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.Expect(LogType.Error, MissingDependencies(nameof(RootService)));

            Assert.IsFalse(await Locator.Register(new InterfaceDependentService()));
        });

        private static Regex MissingDependencies(string typeName)
        {
            return new Regex($@"missing dependencies: \S*\.{typeName}$");
        }
    }
}
