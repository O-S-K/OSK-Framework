using System;
using System.Collections.Generic;

namespace OSK
{
    public interface IComponentPool
    {
        void Remove(int entityId);
        bool Has(int entityId);
        void CopyTo(int sourceEntityId, int destEntityId);
    }

    /// <summary>
    /// Lưu trữ Component bằng cấu trúc Sparse Set.
    /// Giúp mảng Data luôn liền kề nhau trên RAM (Dense Array) để tối ưu CPU Cache.
    /// </summary>
    public class ComponentPool<T> : IComponentPool where T : struct, IComponentData
    {
        public T[] Components = new T[128];
        private int[] _sparse = new int[128]; // Map từ Entity ID sang Index của mảng Data
        private int[] _dense = new int[128];  // Lưu Entity ID tương ứng với từng Index của mảng Data
        public int Count { get; private set; }

        public ComponentPool()
        {
            Array.Fill(_sparse, -1);
        }

        private void EnsureCapacity(int entityId)
        {
            if (entityId >= _sparse.Length)
            {
                int newSize = Math.Max(entityId + 1, _sparse.Length * 2);
                var newSparse = new int[newSize];
                Array.Fill(newSparse, -1);
                Array.Copy(_sparse, newSparse, _sparse.Length);
                _sparse = newSparse;
            }
            if (Count >= Components.Length)
            {
                int newSize = Components.Length * 2;
                Array.Resize(ref Components, newSize);
                Array.Resize(ref _dense, newSize);
            }
        }

        public void Add(int entityId, T component)
        {
            EnsureCapacity(entityId);
            if (_sparse[entityId] != -1)
            {
                Components[_sparse[entityId]] = component;
                return;
            }

            int index = Count;
            _sparse[entityId] = index;
            _dense[index] = entityId;
            Components[index] = component;
            Count++;
        }

        public ref T Get(int entityId)
        {
            if (entityId < 0 || entityId >= _sparse.Length || _sparse[entityId] == -1)
            {
                throw new Exception($"ComponentPool<{typeof(T).Name}>: Entity {entityId} does not have this component or has been destroyed!");
            }
            return ref Components[_sparse[entityId]];
        }

        public bool Has(int entityId)
        {
            if (entityId >= _sparse.Length) return false;
            return _sparse[entityId] != -1;
        }

        public void Remove(int entityId)
        {
            if (!Has(entityId)) return;

            int indexToRemove = _sparse[entityId];
            int lastIndex = Count - 1;

            if (indexToRemove < lastIndex)
            {
                // Đưa phần tử cuối cùng đè lên phần tử bị xoá để mảng luôn liền kề (Zero-GC, O(1))
                int lastEntityId = _dense[lastIndex];
                Components[indexToRemove] = Components[lastIndex];
                _dense[indexToRemove] = lastEntityId;
                _sparse[lastEntityId] = indexToRemove;
            }

            _sparse[entityId] = -1;
            Count--;
        }

        public void CopyTo(int sourceEntityId, int destEntityId)
        {
            if (Has(sourceEntityId)) Add(destEntityId, Get(sourceEntityId));
        }
    }
}
