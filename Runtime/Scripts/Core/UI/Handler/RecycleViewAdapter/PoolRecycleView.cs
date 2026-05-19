using System.Collections.Generic;
using UnityEngine;

namespace OSK 
{
    // Use for target PoolRecycleView
    public class PoolRecycleView<T> where T : Component
    {
        private readonly Stack<T> _stack = new Stack<T>();
        private readonly T _prefab;
        private readonly Transform _parent;

        public int CountAll { get; private set; }
        public int CountInactive => _stack.Count;

        public PoolRecycleView(T prefab, Transform parent = null)
        {
            _prefab = prefab;
            _parent = parent;
        }

        public T Get()
        {
            T element;
            if (_stack.Count == 0)
            {
                element = Object.Instantiate(_prefab, _parent);
                element.gameObject.SetActive(true);
                CountAll++;
            }
            else
            {
                element = _stack.Pop();
                if (element) 
                {
                    element.transform.SetParent(_parent, false);
                    element.gameObject.SetActive(true);
                }
                else
                {
                    return Get();
                }
            }
            return element;
        }

        public void Release(T element)
        {
            if (element == null) return; 
            
            element.gameObject.SetActive(false);
            element.transform.SetParent(_parent, false);
            _stack.Push(element);
        }

        public void Clear()
        {
            while (_stack.Count > 0)
            {
                var e = _stack.Pop();
                if (e != null) Object.Destroy(e.gameObject);
            }
            CountAll = 0;
        }
    }
}