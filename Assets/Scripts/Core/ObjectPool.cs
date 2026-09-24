using System.Collections.Generic;
using UnityEngine;

namespace EchoOfTheVoid.Core
{
    /// <summary>
    /// Generic object pool for reusing GameObjects without allocating new ones (L14 Performance).
    /// Pre-allocates N instances and reuses via SetActive(true/false).
    /// </summary>
    public class ObjectPool<T> where T : MonoBehaviour
    {
        private readonly Queue<T> _available = new Queue<T>();
        private readonly HashSet<T> _active = new HashSet<T>();
        private readonly GameObject _prefab;
        private readonly Transform _container;
        private readonly int _initialSize;

        public int AvailableCount => _available.Count;
        public int ActiveCount => _active.Count;

        public ObjectPool(GameObject prefab, int initialSize = 5, Transform container = null)
        {
            _prefab = prefab;
            _container = container;
            _initialSize = initialSize;

            for (int i = 0; i < initialSize; i++)
            {
                var instance = Object.Instantiate(prefab, container);
                var component = instance.GetComponent<T>();
                if (component == null) component = instance.AddComponent<T>();
                instance.SetActive(false);
                _available.Enqueue(component);
            }
        }

        public T Get()
        {
            T instance;
            if (_available.Count > 0)
            {
                instance = _available.Dequeue();
            }
            else
            {
                var go = Object.Instantiate(_prefab, _container);
                instance = go.GetComponent<T>();
                if (instance == null) instance = go.AddComponent<T>();
            }

            instance.gameObject.SetActive(true);
            _active.Add(instance);
            return instance;
        }

        public void Return(T instance)
        {
            if (instance == null) return;
            if (!_active.Remove(instance)) return;

            instance.gameObject.SetActive(false);
            _available.Enqueue(instance);
        }

        public void Clear()
        {
            foreach (var instance in _active)
            {
                if (instance != null) Object.Destroy(instance.gameObject);
            }
            _active.Clear();

            foreach (var instance in _available)
            {
                if (instance != null) Object.Destroy(instance.gameObject);
            }
            _available.Clear();
        }
    }
}
