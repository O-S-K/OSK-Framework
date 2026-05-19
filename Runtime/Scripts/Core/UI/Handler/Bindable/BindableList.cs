using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    public enum ECollectionOperation { Add, Remove, Update, Clear }

    public class CollectionChangedEventArgs<T>
    {
        public ECollectionOperation Operation { get; private set; }
        public int Index { get; private set; }
        public T Item { get; private set; }

        public CollectionChangedEventArgs(ECollectionOperation op, int index, T item)
        {
            Operation = op;
            Index = index;
            Item = item;
        }
    }

    [Serializable]
    public class BindableList<T> : IList<T>
    {
        [SerializeField]
        private List<T> _list = new List<T>();

        private Action<IList<T>> _onListChanged;
        
        /// <summary>
        /// Sự kiện bắn ra chính xác phần tử nào bị thêm/xóa/sửa, rất quan trọng để tối ưu UI (không cần Instantiate lại toàn bộ list)
        /// </summary>
        private Action<CollectionChangedEventArgs<T>> _onItemChanged;

        public BindableList() { }

        public BindableList(IEnumerable<T> collection)
        {
            _list = new List<T>(collection);
        }

        public void Bind(Action<IList<T>> action, bool triggerImmediately = true)
        {
            _onListChanged += action;
            if (triggerImmediately)
            {
                action?.Invoke(_list);
            }
        }

        public void BindDetailed(Action<CollectionChangedEventArgs<T>> action)
        {
            _onItemChanged += action;
        }

        public void Unbind(Action<IList<T>> action)
        {
            _onListChanged -= action;
        }

        public void UnbindDetailed(Action<CollectionChangedEventArgs<T>> action)
        {
            _onItemChanged -= action;
        }

        public void UnbindAll()
        {
            _onListChanged = null;
            _onItemChanged = null;
        }

        public void ForceUpdate()
        {
            _onListChanged?.Invoke(_list);
        }

        // --- IList<T> Implementation ---

        public T this[int index]
        {
            get => _list[index];
            set
            {
                if (!EqualityComparer<T>.Default.Equals(_list[index], value))
                {
                    _list[index] = value;
                    _onListChanged?.Invoke(_list);
                    _onItemChanged?.Invoke(new CollectionChangedEventArgs<T>(ECollectionOperation.Update, index, value));
                }
            }
        }

        public int Count => _list.Count;

        public bool IsReadOnly => ((ICollection<T>)_list).IsReadOnly;

        public void Add(T item)
        {
            _list.Add(item);
            _onListChanged?.Invoke(_list);
            _onItemChanged?.Invoke(new CollectionChangedEventArgs<T>(ECollectionOperation.Add, _list.Count - 1, item));
        }

        public void Clear()
        {
            if (_list.Count > 0)
            {
                _list.Clear();
                _onListChanged?.Invoke(_list);
                _onItemChanged?.Invoke(new CollectionChangedEventArgs<T>(ECollectionOperation.Clear, -1, default));
            }
        }

        public bool Contains(T item) => _list.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

        public int IndexOf(T item) => _list.IndexOf(item);

        public void Insert(int index, T item)
        {
            _list.Insert(index, item);
            _onListChanged?.Invoke(_list);
            _onItemChanged?.Invoke(new CollectionChangedEventArgs<T>(ECollectionOperation.Add, index, item));
        }

        public bool Remove(T item)
        {
            int index = _list.IndexOf(item);
            if (index >= 0)
            {
                _list.RemoveAt(index);
                _onListChanged?.Invoke(_list);
                _onItemChanged?.Invoke(new CollectionChangedEventArgs<T>(ECollectionOperation.Remove, index, item));
                return true;
            }
            return false;
        }

        public void RemoveAt(int index)
        {
            T item = _list[index];
            _list.RemoveAt(index);
            _onListChanged?.Invoke(_list);
            _onItemChanged?.Invoke(new CollectionChangedEventArgs<T>(ECollectionOperation.Remove, index, item));
        }

        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
    }
}
