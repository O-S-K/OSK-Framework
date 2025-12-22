using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OSK
{
    public class ObjectPool<T>
    {
        private List<ObjectPoolContainer<T>> _listPool;
        private Dictionary<T, ObjectPoolContainer<T>> _lookupDic;
        private Func<T> _factoryFunc;
        private int _lastIndex = 0;

        public int Count
        {
            get => _listPool.Count;
        }

        public int CountUsedItems
        {
            get => _lookupDic.Count;
        }

        public ObjectPool(Func<T> factoryFunc, int initialSize)
        {
            this._factoryFunc = factoryFunc;
            _listPool = new List<ObjectPoolContainer<T>>(initialSize);
            _lookupDic = new Dictionary<T, ObjectPoolContainer<T>>(initialSize);
            Warm(initialSize);
        }

        private void Warm(int capacity)
        {
            for (int i = 0; i < capacity; i++)
                CreateContainer();
        }

        private ObjectPoolContainer<T> CreateContainer()
        {
            var container = new ObjectPoolContainer<T>();
            container.Item = _factoryFunc();
            _listPool.Add(container);
            return container;
        }

        public T GetItem()
        {
            ObjectPoolContainer<T> container = null;

            int checkedCount = 0;
            while (checkedCount < _listPool.Count)
            {
                _lastIndex++;
                if (_lastIndex >= _listPool.Count) _lastIndex = 0;

                var temp = _listPool[_lastIndex];

                if (temp.Item == null || temp.Used)
                {
                    if (temp.Item == null)
                    {
                        _listPool.RemoveAt(_lastIndex);
                        _lastIndex--;
                    }

                    checkedCount++;
                    continue;
                }

                container = temp;
                break;
            }

            if (container == null)
            {
                MyLogger.LogWarning($"[Pool] No available item found in pool of type {typeof(T).Name}. Creating a new one.");
                container = CreateContainer();
            }

            container.Consume();

            if (container.Item == null)
            {
                MyLogger.LogWarning($"[Pool] Created item is null in pool of type {typeof(T).Name}. Returning default value.");
                return default;
            }

            _lookupDic[container.Item] = container;
            return container.Item;
        }

        public void AutoRefillIfNeeded()
        {
            int validCount = _listPool.Select(x => x.Item).Count(x => x != null);
            if (_listPool.Count == 0 || validCount * 1f / _listPool.Count < 0.2f)
            {
                int refillCount = Mathf.Max(1, _listPool.Count / 2);
                Refill(refillCount);
            }
        }

        public void Refill(int amount = 1)
        {
            for (int i = 0; i < amount; i++)
            {
                var container = CreateContainer();
                _listPool.Add(container);
            }

            MyLogger.Log($"[Pool] Refilled {amount} item(s) to pool of type {typeof(T).Name}.");
        }

        public List<T> GetAllItems()
        {
            List<T> items = new List<T>();
            foreach (var container in _listPool)
            {
                items.Add(container.Item);
            }

            return items;
        }


        public void ReleaseItem(T item)
        {
            if (_lookupDic.TryGetValue(item, out var container))
            {
                container.Release();
                _lookupDic.Remove(item);
            }
            else
            {
                MyLogger.LogWarning("This object pool does not contain the item provided: " + item);
            }
        }

        public void DestroyAndClean()
        {
            for (int i = _listPool.Count - 1; i >= 0; i--)
            {
                var container = _listPool[i];
                if (container.Item == null)
                {
                    _listPool.RemoveAt(i);
                    continue;
                }

                if (container.Item is GameObject go)
                    GameObject.Destroy(go);

                else if (container.Item is Component comp)
                    GameObject.Destroy(comp.gameObject);

                _listPool.RemoveAt(i);
                _lookupDic.Remove(container.Item);
            }

            var keysToRemove = _lookupDic.Where(kvp => kvp.Key == null).Select(kvp => kvp.Key).ToList();
            foreach (var key in keysToRemove)
            {
                _lookupDic.Remove(key);
            }

            MyLogger.Log($"[Pool] Cleaned and destroyed unused items. Remaining: {_listPool.Count}");
        }

        public void Clear()
        {
            foreach (var container in _listPool)
            {
                container.Release();
            }

            _listPool.Clear();
            _lookupDic.Clear();
        }
    }
}