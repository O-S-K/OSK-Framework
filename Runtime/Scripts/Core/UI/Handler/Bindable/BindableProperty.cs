using System;
using System.Collections.Generic;
using UnityEngine;

namespace OSK
{ 
    [Serializable]
    public class BindableProperty<T>
    {
        [SerializeField]
        private T _value;
        private Action<T> _onValueChanged;

#if UNITY_EDITOR
        [NonSerialized] private T _lastValue;
#endif

        public T Value
        {
            get => _value;
            set
            { 
                if (!EqualityComparer<T>.Default.Equals(_value, value))
                {
                    _value = value;
#if UNITY_EDITOR
                    _lastValue = value;
#endif
                    _onValueChanged?.Invoke(_value);
                }
            }
        }

        public BindableProperty(T initialValue = default)
        {
            _value = initialValue;
#if UNITY_EDITOR
            _lastValue = initialValue;
            RegisterInstance(this);
#endif
        } 
        
        public void Bind(Action<T> action, bool triggerImmediately = true)
        {
            _onValueChanged += action;
            if (triggerImmediately)
            {
                action?.Invoke(_value);
            }
        }  
        
        public void Unbind(Action<T> action)
        {
            _onValueChanged -= action;
        }
        
        public void UnbindAll()
        {
            _onValueChanged = null;
        }
        
        public static implicit operator T(BindableProperty<T> prop) => prop.Value;
        public override string ToString() => _value != null ? _value.ToString() : "null";

        public void ForceUpdate()
        {
            _onValueChanged?.Invoke(_value);
        }

#if UNITY_EDITOR
        private static bool _isUpdaterRegistered = false;
        private static readonly List<WeakReference> _instances = new List<WeakReference>();

        private static void RegisterInstance(BindableProperty<T> instance)
        {
            _instances.Add(new WeakReference(instance));
            if (!_isUpdaterRegistered)
            {
                UnityEditor.EditorApplication.update += GlobalEditorUpdate;
                _isUpdaterRegistered = true;
            }
        }

        private static void GlobalEditorUpdate()
        {
            if (!Application.isPlaying) return;

            for (int i = _instances.Count - 1; i >= 0; i--)
            {
                var weakRef = _instances[i];
                if (weakRef.IsAlive)
                {
                    var prop = weakRef.Target as BindableProperty<T>;
                    if (prop != null)
                    {
                        prop.CheckEditorChange();
                    }
                }
                else
                {
                    _instances.RemoveAt(i);
                }
            }
        }

        private void CheckEditorChange()
        {
            if (!EqualityComparer<T>.Default.Equals(_value, _lastValue))
            {
                _lastValue = _value;
                _onValueChanged?.Invoke(_value);
            }
        }
#endif
    }
}
