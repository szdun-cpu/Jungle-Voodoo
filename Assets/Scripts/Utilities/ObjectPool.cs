using System.Collections.Generic;
using UnityEngine;

namespace JungleVoodoo.Utilities
{
    /// <summary>
    /// Generic object pool that recycles GameObjects to avoid GC spikes on mobile.
    /// Use for frequently spawned/despawned objects: troops, projectiles, VFX, map markers.
    ///
    /// Usage:
    ///   var pool = new ObjectPool(prefab, initialSize: 20, parent: poolRoot);
    ///   var obj  = pool.Get(position, rotation);
    ///   pool.Return(obj);
    /// </summary>
    public class ObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform  _parent;
        private readonly Stack<GameObject> _available = new();

        public int AvailableCount => _available.Count;

        public ObjectPool(GameObject prefab, int initialSize = 10, Transform parent = null)
        {
            _prefab = prefab;
            _parent = parent;

            for (int i = 0; i < initialSize; i++)
                _available.Push(CreateInstance());
        }

        public GameObject Get(Vector3 position = default, Quaternion rotation = default)
        {
            var obj = _available.Count > 0 ? _available.Pop() : CreateInstance();
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            return obj;
        }

        public T Get<T>(Vector3 position = default, Quaternion rotation = default) where T : Component
        {
            return Get(position, rotation).GetComponent<T>();
        }

        public void Return(GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.SetParent(_parent);
            _available.Push(obj);
        }

        public void WarmUp(int count)
        {
            for (int i = 0; i < count; i++)
                _available.Push(CreateInstance());
        }

        private GameObject CreateInstance()
        {
            var obj = Object.Instantiate(_prefab, _parent);
            obj.SetActive(false);
            return obj;
        }
    }
}
