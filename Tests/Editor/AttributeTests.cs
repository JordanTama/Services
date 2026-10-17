using System;
using NUnit.Framework;
using Services;

namespace JordanTama.Services.Editor.Tests
{
    public class AttributeTests
    {
        private const string DefaultSettingsPath = "Assets/ScriptableObjects/ServiceSettings";

        [Test]
        public void DependsOnService_WithServiceTypes_ExposesThemInOrder()
        {
            var attribute = new DependsOnServiceAttribute(typeof(RootService), typeof(IndependentService));

            CollectionAssert.AreEqual(new[] { typeof(RootService), typeof(IndependentService) }, attribute.Dependencies);
        }

        [Test]
        public void DependsOnService_WithServiceInterface_IsAccepted()
        {
            var attribute = new DependsOnServiceAttribute(typeof(IDependsOnRoot));

            CollectionAssert.AreEqual(new[] { typeof(IDependsOnRoot) }, attribute.Dependencies);
        }

        [Test]
        public void DependsOnService_WithNoTypes_HasNoDependencies()
        {
            var attribute = new DependsOnServiceAttribute();

            CollectionAssert.IsEmpty(attribute.Dependencies);
        }

        [Test]
        public void DependsOnService_WithNonServiceType_Throws()
        {
            Assert.Throws<ArgumentException>(() => _ = new DependsOnServiceAttribute(typeof(RootService), typeof(string)));
        }

        [Test]
        public void ServiceSettings_WithKeyAndPath_ExposesThem()
        {
            var attribute = new ServiceSettingsAttribute("Key", "Assets/Path");

            Assert.AreEqual("Key", attribute.Key);
            Assert.AreEqual("Assets/Path", attribute.FilePath);
        }

        [Test]
        public void ServiceSettings_WithoutPath_UsesDefaultPath()
        {
            var attribute = new ServiceSettingsAttribute("Key");

            Assert.AreEqual(DefaultSettingsPath, attribute.FilePath);
        }

        [TestCase(null)]
        [TestCase("")]
        public void ServiceSettings_WithInvalidKey_Throws(string key)
        {
            Assert.Throws<ArgumentException>(() => _ = new ServiceSettingsAttribute(key));
        }

        [TestCase(null)]
        [TestCase("")]
        public void ServiceSettings_WithInvalidPath_Throws(string path)
        {
            Assert.Throws<ArgumentException>(() => _ = new ServiceSettingsAttribute("Key", path));
        }
    }
}
