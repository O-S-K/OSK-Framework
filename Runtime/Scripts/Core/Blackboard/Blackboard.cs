using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OSK
{
    public class Blackboard
    {
        [System.Serializable]
        public class BlackboardValue
        {
            public object value;
            public int priority;
            public bool isReadOnly;

            public BlackboardValue(object value, int priority, bool isReadOnly = false)
            {
                this.value = value;
                this.priority = priority;
                this.isReadOnly = isReadOnly;
            }
        }

        private Dictionary<string, BlackboardValue> k_DataBlackboard = new Dictionary<string, BlackboardValue>();
        private Dictionary<string, List<Action<object>>> k_Callbacks = new Dictionary<string, List<Action<object>>>();

        private BlackboardData _defaultData;
        private GameObject _pingObject;

        public void SetData(BlackboardData defaultData, GameObject pingObject)
        {
            _defaultData = defaultData;
            defaultData.SetDefaultValues(this);
            PingObject(pingObject);
        }

        public string GetKey<T>(string key)
        {
            return key;
        }

        public void SetValue<T>(string key, T value, int priority = 0, bool isReadOnly = false)
        {
            if (k_DataBlackboard.TryGetValue(key, out var existingValue))
            {
                if (existingValue.isReadOnly)
                {
                    OSKLogger.LogWarning("Blackboard",$"Cannot modify read-only value for key: {key}");
                    return;
                }

                // Only update if new priority is higher or equal
                if (priority < existingValue.priority)
                {
                    OSKLogger.LogWarning("Blackboard",$"Cannot override value for key: {key} with lower priority");
                    return;
                }
            }

            k_DataBlackboard[key] = new BlackboardValue(value, priority, isReadOnly);

            if (k_Callbacks.TryGetValue(key, out var callbackList))
            {
                foreach (var callback in callbackList)
                {
                    callback?.Invoke(value);
                }
            }
        }

        public T GetValue<T>(string key)
        {
            if (k_DataBlackboard.TryGetValue(key, out var value))
            {
                if (value.value is T typedValue)
                {
                    return typedValue;
                }

                OSKLogger.LogError("Blackboard",$"Value for key '{key}' cannot be cast to type {typeof(T)}");
            }

            OSKLogger.LogError("Blackboard",$"Key '{key}' not found in blackboard");
            return default;
        }

        public bool TryGetValue<T>(string key, out T value)
        {
            value = default;
            if (k_DataBlackboard.TryGetValue(key, out var rawValue) && rawValue.value is T typedValue)
            {
                value = typedValue;
                return true;
            }

            return false;
        }

        public void Subscribe(string key, Action callback)
        {
            if (!k_Callbacks.ContainsKey(key))
            {
                k_Callbacks[key] = new List<Action<object>>();
            }

            k_Callbacks[key].Add((obj) => { callback?.Invoke(); });
        }

        public void Subscribe<T>(string key, Action<T> callback)
        {
            if (!k_Callbacks.ContainsKey(key))
            {
                k_Callbacks[key] = new List<Action<object>>();
            }

            k_Callbacks[key].Add((obj) =>
            {
                if (obj is T typedValue)
                {
                    callback?.Invoke(typedValue);
                }
            });
        }

        public void Unsubscribe(string key)
        {
            if (k_Callbacks.ContainsKey(key))
            {
                k_Callbacks.Remove(key);
            }
        }

        public void Clear()
        {
            var readOnlyEntries = k_DataBlackboard.Where(kvp => kvp.Value.isReadOnly).ToList();
            k_DataBlackboard.Clear();
            k_Callbacks.Clear();

            // Restore read-only entries
            foreach (var entry in readOnlyEntries)
            {
                k_DataBlackboard[entry.Key] = entry.Value;
            }

            // Reapply default values if they exist
            if (_defaultData != null)
            {
                _defaultData.SetDefaultValues(this);
            }
        }

        public bool HasKey(string key)
        {
            return k_DataBlackboard.ContainsKey(key);
        }

        public void RemoveKey(string key)
        {
            if (k_DataBlackboard.TryGetValue(key, out var value) && value.isReadOnly)
            {
                OSKLogger.LogWarning("Blackboard",$"Cannot remove read-only key: {key}");
                return;
            }

            k_DataBlackboard.Remove(key);
            k_Callbacks.Remove(key);
        }

        public int GetPriority(string key)
        {
            return k_DataBlackboard.TryGetValue(key, out var value) ? value.priority : -1;
        }

        public bool IsReadOnly(string key)
        {
            return k_DataBlackboard.TryGetValue(key, out var value) && value.isReadOnly;
        }

        public Dictionary<string, T> GetAllValuesOfType<T>()
        {
            return k_DataBlackboard
                .Where(kvp => kvp.Value.value is T)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => (T)kvp.Value.value
                );
        }

        public IEnumerable<string> GetKeysByPriority(int priority)
        {
            return k_DataBlackboard
                .Where(kvp => kvp.Value.priority == priority)
                .Select(kvp => kvp.Key);
        }

        public Dictionary<string, BlackboardValue> GetAllValues()
        {
            return k_DataBlackboard;
        }

        // ping object when click on ping button in inspector
        private void PingObject(GameObject pingObject)
        {
            this._pingObject = pingObject;
        }

        public GameObject GetPingObject()
        {
            return _pingObject;
        }
 
    }
}