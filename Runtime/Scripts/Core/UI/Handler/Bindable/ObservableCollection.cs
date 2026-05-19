using System;
using System.Collections;
using System.Collections.Generic;

namespace OSK
{
    /// <summary>
    /// ObservableCollection<T>
    /// Sự kiện: OnAdd(index, item), OnRemove(index, item), OnReplace(index, old, new), OnReset()
    /// Thread-affine: designed to be used on Unity main thread.
    /// </summary>
    public class ObservableCollection<T> : IList<T>, IReadOnlyList<T>
    {
        private readonly List<T> _items = new List<T>();

        public event Action<int, T> OnAdd;
        public event Action<int, T> OnRemove;
        public event Action<int, T, T> OnReplace; // index, old, @new
        public event Action OnReset;
        public event Action<int> OnMove; // optional: from->to encoded as combined? (not used now)

        public int Count => _items.Count;
        public bool IsReadOnly => false;

        public T this[int index]
        {
            get => _items[index];
            set => Replace(index, value);
        }

        public void Add(T item)
        {
            _items.Add(item);
            OnAdd?.Invoke(_items.Count - 1, item);
        }

        public void Insert(int index, T item)
        {
            _items.Insert(index, item);
            OnAdd?.Invoke(index, item);
        }

        public bool Remove(T item)
        {
            var idx = _items.IndexOf(item);
            if (idx < 0) return false;
            var removed = _items[idx];
            _items.RemoveAt(idx);
            OnRemove?.Invoke(idx, removed);
            return true;
        }

        public void RemoveAt(int index)
        {
            var removed = _items[index];
            _items.RemoveAt(index);
            OnRemove?.Invoke(index, removed);
        }

        public void Clear()
        {
            _items.Clear();
            OnReset?.Invoke();
        }

        public bool Contains(T item) => _items.Contains(item);
        public int IndexOf(T item) => _items.IndexOf(item);

        public void Replace(int index, T item)
        {
            var old = _items[index];
            _items[index] = item;
            OnReplace?.Invoke(index, old, item);
        }

        public void Move(int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex) return;
            var item = _items[fromIndex];
            _items.RemoveAt(fromIndex);
            _items.Insert(toIndex, item);
            OnMove?.Invoke(fromIndex); // you can adapt to more detailed event signature
            // For adapters you may want a dedicated OnMove(from,to) signature.
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var it in items) Add(it);
        }

        public T[] ToArray() => _items.ToArray();

        #region IList / IEnumerable impl

        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

        public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

        #endregion
    }
}