using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Services;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine.AddressableAssets;
using UnityEngine.TestTools;

namespace JordanTama.Services.Editor.Tests
{
    public class LocatorSettingsTests : LocatorFixture
    {
        private const string GeneratedGroupName = "GeneratedAssets";

        private static bool _addressablesInitialised;

        private bool _generatedGroupExisted;

        [OneTimeSetUp]
        public void RecordAddressablesState()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            _generatedGroupExisted = settings && settings.FindGroup(GeneratedGroupName);
            DeleteSettingsAsset();
        }

        [UnitySetUp]
        public IEnumerator InitialiseAddressables()
        {
            if (_addressablesInitialised)
                yield break;

            LogAssert.ignoreFailingMessages = true;
            yield return Addressables.InitializeAsync();
            LogAssert.ignoreFailingMessages = false;

            _addressablesInitialised = true;
        }

        [TearDown]
        public void DeleteSettingsAsset()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            string guid = AssetDatabase.AssetPathToGUID(FakeSettings.AssetPath);
            if (settings && !string.IsNullOrEmpty(guid))
                settings.RemoveAssetEntry(guid);

            AssetDatabase.DeleteAsset(FakeSettings.Folder);
        }

        [OneTimeTearDown]
        public void RemoveGeneratedGroup()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (_generatedGroupExisted || !settings)
                return;

            var group = settings.FindGroup(GeneratedGroupName);
            if (group)
                settings.RemoveGroup(group);
        }

        [UnityTest]
        public IEnumerator Register_SettingsService_AssignsSettingsBeforeOnRegistered() => UniTask.ToCoroutine(async () =>
        {
            var service = new SettingsService();
            FakeSettings settingsDuringRegistration = null;
            service.Registering = () =>
            {
                settingsDuringRegistration = service.Settings;
                return UniTask.CompletedTask;
            };

            Assert.IsTrue(await Locator.Register<FakeSettings>(service));
            Assert.IsNotNull(settingsDuringRegistration);
        });

        [UnityTest]
        public IEnumerator Register_SettingsServiceWithInferredType_AssignsSettings() => UniTask.ToCoroutine(async () =>
        {
            var service = new SettingsService();

            await Locator.Register(service);

            Assert.IsNotNull(service.Settings);
        });

        [UnityTest]
        public IEnumerator Register_SettingsService_PersistsSettingsAtConfiguredPath() => UniTask.ToCoroutine(async () =>
        {
            var service = new SettingsService();

            await Locator.Register<FakeSettings>(service);

            Assert.AreEqual(FakeSettings.AssetPath, AssetDatabase.GetAssetPath(service.Settings));
        });

        [UnityTest]
        public IEnumerator Register_SettingsService_MakesSettingsAddressableByKey() => UniTask.ToCoroutine(async () =>
        {
            await Locator.Register<FakeSettings>(new SettingsService());

            string guid = AssetDatabase.AssetPathToGUID(FakeSettings.AssetPath);
            var entry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid);
            Assert.IsNotNull(entry);
            Assert.AreEqual(FakeSettings.Key, entry.address);
            Assert.AreEqual(GeneratedGroupName, entry.parentGroup.Name);
        });

        [UnityTest]
        public IEnumerator Register_SettingsServiceAgain_ReusesPersistedSettings() => UniTask.ToCoroutine(async () =>
        {
            var first = new SettingsService();
            await Locator.Register<FakeSettings>(first);
            await Locator.Unregister(first);
            var second = new SettingsService();

            await Locator.Register<FakeSettings>(second);

            Assert.AreSame(first.Settings, second.Settings);
        });

        [UnityTest]
        public IEnumerator Register_SettingsWithoutAttribute_ThrowsAndDoesNotRegister() => UniTask.ToCoroutine(async () =>
        {
            var service = new UnattributedSettingsService();

            var exception = await Capture(() => Locator.Register<UnattributedSettings>(service));

            Assert.IsNotNull(exception);
            StringAssert.Contains(nameof(ServiceSettingsAttribute), exception.Message);
            Assert.IsFalse(Locator.IsRegistered<UnattributedSettingsService>());
            Assert.AreEqual(0, service.RegisteredCalls);
        });
    }
}
