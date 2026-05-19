using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    // Dictionary can't be easily serialized natively in Unity, so we provide an IDictionary interface
    // but without [Serializable] unless you have a custom dictionary serializer (like Odin).
    public class BindableDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private Dictionary<TKey, TValue> _dict = new Dictionary<TKey, TValue>();

        private Action<IDictionary<TKey, TValue>> _onDictChanged;

        public BindableDictionary() { }

        public BindableDictionary(IDictionary<TKey, TValue> dictionary)
        {
            _dict = new Dictionary<TKey, TValue>(dictionary);
        }

        public void Bind(Action<IDictionary<TKey, TValue>> action, bool triggerImmediately = true)
        {
            _onDictChanged += action;
            if (triggerImmediately)
            {
                action?.Invoke(_dict);
            }
        }

        public void Unbind(Action<IDictionary<TKey, TValue>> action)
        {
            _onDictChanged -= action;
        }

        public void UnbindAll()
        {
            _onDictChanged = null;
        }

        public void ForceUpdate()
        {
            _onDictChanged?.Invoke(_dict);
        }

        // --- IDictionary Implementation ---

        public TValue this[TKey key]
        {
            get => _dict[key];
            set
            {
                if (_dict.TryGetValue(key, out TValue existing))
                {
                    if (!EqualityComparer<TValue>.Default.Equals(existing, value))
                    {
                        _dict[key] = value;
                        _onDictChanged?.Invoke(_dict);
                    }
                }
                else
                {
                    _dict[key] = value;
                    _onDictChanged?.Invoke(_dict);
                }
            }
        }

        public ICollection<TKey> Keys => _dict.Keys;

        public ICollection<TValue> Values => _dict.Values;

        public int Count => _dict.Count;

        public bool IsReadOnly => ((IDictionary<TKey, TValue>)_dict).IsReadOnly;

        public void Add(TKey key, TValue value)
        {
            _dict.Add(key, value);
            _onDictChanged?.Invoke(_dict);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ((IDictionary<TKey, TValue>)_dict).Add(item);
            _onDictChanged?.Invoke(_dict);
        }

        public void Clear()
        {
            if (_dict.Count > 0)
            {
                _dict.Clear();
                _onDictChanged?.Invoke(_dict);
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) => ((IDictionary<TKey, TValue>)_dict).Contains(item);

        public bool ContainsKey(TKey key) => _dict.ContainsKey(key);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((IDictionary<TKey, TValue>)_dict).CopyTo(array, arrayIndex);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dict.GetEnumerator();

        public bool Remove(TKey key)
        {
            bool removed = _dict.Remove(key);
            if (removed)
            {
                _onDictChanged?.Invoke(_dict);
            }
            return removed;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            bool removed = ((IDictionary<TKey, TValue>)_dict).Remove(item);
            if (removed)
            {
                _onDictChanged?.Invoke(_dict);
            }
            return removed;
        }

        public bool TryGetValue(TKey key, out TValue value) => _dict.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => _dict.GetEnumerator();
    }
}
