using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using Logger = Logging.Logger;

namespace Services
{
    public class ServiceTests
    {
        [UnitySetUp]
        public IEnumerator Setup()
        {
            Locator.Register(new TestServiceA());
            
            yield return Locator.RegisterAsync(new TestServiceB()).ToCoroutine();
            
            Locator.Register(new TestServiceC());
        }

        [UnityTest]
        public IEnumerator Test()
        {
            Assert.IsTrue(Locator.IsRegistered<TestServiceA>());
            Assert.IsTrue(Locator.IsRegistered<TestServiceB>());
            Assert.IsTrue(Locator.IsRegistered<TestServiceC>());
            
            yield break;
        }
        
        private class TestServiceA : IServiceStandard
        {
            public void OnRegistered()
            {
                Logger.Log(this, "Registered!");
            }

            public void OnUnregistered()
            {
                Logger.Log(this, "Unregistered!");
            }
        }
        
        [DependsOnService(typeof(TestServiceA))]
        private class TestServiceB : IServiceAsync
        {
            public async UniTask OnRegistered()
            {
                Logger.Log(this, "Registering for two seconds...");
                await UniTask.Delay(2000);
                Logger.Log(this, "Registered!");
            }

            public async UniTask OnDeregistered()
            {
                Logger.Log(this, "Unregistering for two seconds...");
                await UniTask.Delay(2000);
                Logger.Log(this, "Unregistered!");
            }
        }

        [DependsOnService(typeof(TestServiceB))]
        private class TestServiceC : IServiceStandard
        {
            public void OnRegistered()
            {
                Logger.Log(this, "Registered!");
            }

            public void OnUnregistered()
            {
                Logger.Log(this, "Unregistered!");
            }
        }
    }
}