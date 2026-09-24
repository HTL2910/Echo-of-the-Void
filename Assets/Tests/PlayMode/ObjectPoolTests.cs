using NUnit.Framework;
using UnityEngine;
using EchoOfTheVoid.Core;

namespace EchoOfTheVoid.Tests
{
    [Category("Performance")]
    public class ObjectPoolTests
    {
        private GameObject _testPrefab;
        private Transform _container;

        [SetUp]
        public void Setup()
        {
            _container = new GameObject("PoolContainer").transform;
            _testPrefab = new GameObject("PoolTest");
            _testPrefab.AddComponent<BoxCollider>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.Destroy(_container.gameObject);
            Object.Destroy(_testPrefab);
        }

        [Test]
        public void Pool_CreatesInitialInstances()
        {
            var pool = new ObjectPool<BoxCollider>(_testPrefab, 5, _container);
            Assert.That(pool.AvailableCount, Is.EqualTo(5));
            pool.Clear();
        }

        [Test]
        public void Get_ReturnsPooledInstance()
        {
            var pool = new ObjectPool<BoxCollider>(_testPrefab, 3, _container);
            var instance = pool.Get();
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.gameObject.activeSelf, Is.True);
            Assert.That(pool.AvailableCount, Is.EqualTo(2));
            pool.Clear();
        }

        [Test]
        public void Return_DisablesAndRecyclesInstance()
        {
            var pool = new ObjectPool<BoxCollider>(_testPrefab, 2, _container);
            var instance1 = pool.Get();
            Assert.That(pool.AvailableCount, Is.EqualTo(1));

            pool.Return(instance1);
            Assert.That(instance1.gameObject.activeSelf, Is.False);
            Assert.That(pool.AvailableCount, Is.EqualTo(2));
            pool.Clear();
        }

        [Test]
        public void Get_AfterReturn_ReusesInstance()
        {
            var pool = new ObjectPool<BoxCollider>(_testPrefab, 1, _container);
            var instance1 = pool.Get();
            pool.Return(instance1);
            var instance2 = pool.Get();

            Assert.That(instance2, Is.EqualTo(instance1), "Should reuse the same instance");
            pool.Clear();
        }

        [Test]
        public void Get_BeyondCapacity_CreatesNew()
        {
            var pool = new ObjectPool<BoxCollider>(_testPrefab, 2, _container);
            var i1 = pool.Get();
            var i2 = pool.Get();
            var i3 = pool.Get(); // Beyond initial size

            Assert.That(i3, Is.Not.Null);
            Assert.That(i3, Is.Not.EqualTo(i1));
            Assert.That(i3, Is.Not.EqualTo(i2));
            pool.Clear();
        }

        [Test]
        public void ActiveCount_TracksGetAndReturn()
        {
            var pool = new ObjectPool<BoxCollider>(_testPrefab, 3, _container);
            Assert.That(pool.ActiveCount, Is.EqualTo(0));

            var i1 = pool.Get();
            Assert.That(pool.ActiveCount, Is.EqualTo(1));

            var i2 = pool.Get();
            Assert.That(pool.ActiveCount, Is.EqualTo(2));

            pool.Return(i1);
            Assert.That(pool.ActiveCount, Is.EqualTo(1));

            pool.Clear();
        }

        [Test]
        public void Return_NullInstance_IsNoop()
        {
            var pool = new ObjectPool<BoxCollider>(_testPrefab, 1, _container);
            pool.Return(null);
            Assert.That(pool.AvailableCount, Is.EqualTo(1));
            pool.Clear();
        }

        [Test]
        public void Clear_DestroysAllInstances()
        {
            var pool = new ObjectPool<BoxCollider>(_testPrefab, 3, _container);
            var i1 = pool.Get();
            var i2 = pool.Get();
            pool.Return(i1);

            pool.Clear();
            Assert.That(pool.AvailableCount, Is.EqualTo(0));
            Assert.That(pool.ActiveCount, Is.EqualTo(0));
        }
    }
}
