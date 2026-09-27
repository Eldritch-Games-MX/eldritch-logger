using System;
using EldritchGames.EldritchLogger.Console.Registry;
using NUnit.Framework;

namespace EldritchGames.EldritchLogger.Console.Tests.EditMode.Registry
{
    [TestFixture]
    public class ServiceRegistryTests
    {
        private class DummyService { }
        private class AnotherService { }

        [SetUp]
        public void SetUp()
        {
            ServiceRegistry.Clear();
        }

        [Test]
        public void Register_And_Resolve_ReturnsInstance()
        {
            var service = new DummyService();
            ServiceRegistry.Register(service);

            var resolved = ServiceRegistry.Resolve(typeof(DummyService));

            Assert.NotNull(resolved);
            Assert.AreSame(service, resolved);
        }

        [Test]
        public void Resolve_StrictMode_ThrowsIfNotRegistered()
        {
            Assert.Throws<InvalidOperationException>(() =>
                ServiceRegistry.Resolve(typeof(AnotherService), strict: true));
        }

        [Test]
        public void Resolve_NonStrictMode_ReturnsNullIfNotRegistered()
        {
            var resolved = ServiceRegistry.Resolve(typeof(AnotherService), strict: false);

            Assert.IsNull(resolved);
        }

        [Test]
        public void Register_OverwritesExistingInstance()
        {
            var first = new DummyService();
            var second = new DummyService();

            ServiceRegistry.Register(first);
            ServiceRegistry.Register(second);

            var resolved = ServiceRegistry.Resolve(typeof(DummyService));

            Assert.AreSame(second, resolved);
        }

        [Test]
        public void Clear_RemovesAllServices()
        {
            ServiceRegistry.Register(new DummyService());
            ServiceRegistry.Register(new AnotherService());

            ServiceRegistry.Clear();

            Assert.IsNull(ServiceRegistry.Resolve(typeof(DummyService), strict: false));
            Assert.IsNull(ServiceRegistry.Resolve(typeof(AnotherService), strict: false));
        }
    }
}
