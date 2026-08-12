using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Services
{
    public class ServiceTests
    {
        [UnitySetUp]
        public IEnumerator Setup()
        {
            yield return Locator.Register(new TestServiceA());
            yield return Locator.Register(new TestServiceB());
            yield return Locator.Register(new TestServiceC());
        }

        [UnityTest]
        public IEnumerator Test()
        {
            Assert.IsTrue(Locator.IsRegistered<TestServiceA>());
            Assert.IsTrue(Locator.IsRegistered<TestServiceB>());
            Assert.IsTrue(Locator.IsRegistered<TestServiceC>());
            
            yield break;
        }
        
        private class TestServiceA : IService
        {
            public UniTask OnRegistered()
            {
                Debug.Log("Registered!");
                return UniTask.CompletedTask;
            }

            public UniTask OnUnregistered()
            {
                Debug.Log("Unregistered!");
                return UniTask.CompletedTask;
            }
        }
        
        [DependsOnService(typeof(TestServiceA))]
        private class TestServiceB : IService
        {
            public async UniTask OnRegistered()
            {
                Debug.Log("Registering for two seconds...");
                await UniTask.Delay(2000);
                Debug.Log("Registered!");
            }

            public async UniTask OnUnregistered()
            {
                Debug.Log("Unregistering for two seconds...");
                await UniTask.Delay(2000);
                Debug.Log("Unregistered!");
            }
        }

        [DependsOnService(typeof(TestServiceB))]
        private class TestServiceC : IService
        {
            public UniTask OnRegistered()
            {
                Debug.Log("Registered!");
                return UniTask.CompletedTask;
            }

            public UniTask OnUnregistered()
            {
                Debug.Log("Unregistered!");
                return UniTask.CompletedTask;
            }
        }
    }
}