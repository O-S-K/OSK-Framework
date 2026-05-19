using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace OSK
{
    public class UniversalBinder : MonoBehaviour
    {
        public enum BindingMode { OneWay, TwoWay, Event }
        public enum ParameterType { None, Int, Float, String, Bool, Dynamic }
        public enum SourceResolutionMode { AssignManual, FindInScene, SearchInParent }

        [Header("Source (Model/ViewModel)")]
        public Component sourceComponent;
        public string sourcePropertyName;

        [Header("Target (UI Component)")]
        public Component targetComponent;
        public string targetPropertyName;

        public BindingMode mode = BindingMode.OneWay;
        public ValueConverter converter;

        [Header("Event Settings")]
        public ParameterType parameterType = ParameterType.None;
        public int constIntParam;
        public float constFloatParam;
        public string constStringParam;
        public bool constBoolParam;

        [Header("Dynamic Resolution")]
        public SourceResolutionMode sourceResolution = SourceResolutionMode.AssignManual;
        public string sourceTypeName;

        private object _dynamicSource;
        private object _sourceBindable;
        private MethodInfo _sourceUnbindMethod;
        private Delegate _onValueChangedDelegate;

        private Delegate _eventListenerDelegate;
        private object _eventValue;

        // --- PERFORMANCE CACHING ---
        private static readonly Dictionary<string, Type> _resolvedTypeCache = new Dictionary<string, Type>();

        // Target cache
        private PropertyInfo _targetPropertyInfo;
        private FieldInfo _targetFieldInfo;
        private MethodInfo _targetMethodInfo;

        // Source cache
        private PropertyInfo _sourceValuePropertyInfo;
        private MethodInfo _cachedSourceMethod;
        private FieldInfo _cachedSourceField;
        private PropertyInfo _cachedSourceProperty;
        private object _cachedSourceMemberValue;
        private MethodInfo _cachedTriggerMethod;

        private void Awake()
        {
            ResolveSourceIfNeeded();
            InitializeBinding();
        }

        public void SetSource(object source)
        {
            UnbindCurrent();
            _dynamicSource = source;
            if (source is Component comp)
            {
                sourceComponent = comp;
            }
            InitializeBinding();
        }

        private object GetSource()
        {
            if (_dynamicSource != null) return _dynamicSource;
            return sourceComponent;
        }

        private void ResolveSourceIfNeeded()
        {
            if (sourceComponent != null || string.IsNullOrEmpty(sourceTypeName)) return;

            if (sourceResolution == SourceResolutionMode.FindInScene)
            {
                Type targetType = ResolveType(sourceTypeName);
                if (targetType != null)
                {
                    sourceComponent = FindObjectOfType(targetType) as Component;
                }
            }
            else if (sourceResolution == SourceResolutionMode.SearchInParent)
            {
                Type targetType = ResolveType(sourceTypeName);
                if (targetType != null)
                {
                    sourceComponent = GetComponentInParent(targetType);
                }
            }
        }

        private Type ResolveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;
            if (_resolvedTypeCache.TryGetValue(typeName, out var cachedType)) return cachedType;

            Type t = Type.GetType(typeName);
            if (t == null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    t = assembly.GetType(typeName);
                    if (t != null) break;

                    t = assembly.GetType("OSK." + typeName);
                    if (t != null) break;
                }
            }

            if (t != null)
            {
                _resolvedTypeCache[typeName] = t;
            }
            return t;
        }

        private void InitializeBinding()
        {
            object source = GetSource();
            if (source == null || targetComponent == null) return;
            if (string.IsNullOrEmpty(sourcePropertyName) || string.IsNullOrEmpty(targetPropertyName)) return;

            // Cache target members
            Type targetType = targetComponent.GetType();
            _targetPropertyInfo = targetType.GetProperty(targetPropertyName, BindingFlags.Public | BindingFlags.Instance);
            _targetFieldInfo = targetType.GetField(targetPropertyName, BindingFlags.Public | BindingFlags.Instance);
            _targetMethodInfo = targetType.GetMethod(targetPropertyName, BindingFlags.Public | BindingFlags.Instance);

            if (mode == BindingMode.Event)
            {
                InitializeEventBinding();
                return;
            }

            // 1. Get Source Property/Field
            Type sourceType = source.GetType();
            FieldInfo sourceField = sourceType.GetField(sourcePropertyName, BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo sourceProp = sourceType.GetProperty(sourcePropertyName, BindingFlags.Public | BindingFlags.Instance);

            object sourceValue = null;
            Type valueType = null;

            if (sourceField != null)
            {
                sourceValue = sourceField.GetValue(source);
                valueType = sourceField.FieldType;
            }
            else if (sourceProp != null)
            {
                sourceValue = sourceProp.GetValue(source);
                valueType = sourceProp.PropertyType;
            }

            if (sourceValue == null) return;

            // Check if it is a BindableProperty
            bool isBindable = valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(BindableProperty<>);
            // Check if it is a BindableTrigger
            bool isTrigger = valueType == typeof(BindableTrigger);
            bool isGenericTrigger = valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(BindableTrigger<>);

            if (isBindable)
            {
                _sourceBindable = sourceValue;
                _sourceValuePropertyInfo = valueType.GetProperty("Value");
                Type innerType = valueType.GetGenericArguments()[0];

                // Get Bind/Unbind method
                MethodInfo bindMethod = valueType.GetMethod("Bind", new Type[] { typeof(Action<>).MakeGenericType(innerType), typeof(bool) });
                _sourceUnbindMethod = valueType.GetMethod("Unbind", new Type[] { typeof(Action<>).MakeGenericType(innerType) });

                // Create callback handler
                MethodInfo handlerMethod = this.GetType().GetMethod("OnSourceValueChanged", BindingFlags.NonPublic | BindingFlags.Instance)
                    .MakeGenericMethod(innerType);

                _onValueChangedDelegate = Delegate.CreateDelegate(typeof(Action<>).MakeGenericType(innerType), this, handlerMethod);

                // Invoke Bind(callback, true)
                bindMethod.Invoke(_sourceBindable, new object[] { _onValueChangedDelegate, true });

                // If TwoWay, setup listener on target UI components
                if (mode == BindingMode.TwoWay)
                {
                    SetupTwoWayBinding(innerType);
                }
            }
            else if (isTrigger)
            {
                _sourceBindable = sourceValue;
                MethodInfo bindMethod = valueType.GetMethod("Bind", new Type[] { typeof(Action) });
                _sourceUnbindMethod = valueType.GetMethod("Unbind", new Type[] { typeof(Action) });

                Action action = () => InvokeTargetMethodOrProperty(null);
                _onValueChangedDelegate = action;

                bindMethod.Invoke(_sourceBindable, new object[] { action });
            }
            else if (isGenericTrigger)
            {
                _sourceBindable = sourceValue;
                Type innerType = valueType.GetGenericArguments()[0];
                MethodInfo bindMethod = valueType.GetMethod("Bind", new Type[] { typeof(Action<>).MakeGenericType(innerType) });
                _sourceUnbindMethod = valueType.GetMethod("Unbind", new Type[] { typeof(Action<>).MakeGenericType(innerType) });

                MethodInfo handlerMethod = this.GetType().GetMethod("OnTriggerTriggered", BindingFlags.NonPublic | BindingFlags.Instance)
                    .MakeGenericMethod(innerType);
                _onValueChangedDelegate = Delegate.CreateDelegate(typeof(Action<>).MakeGenericType(innerType), this, handlerMethod);

                bindMethod.Invoke(_sourceBindable, new object[] { _onValueChangedDelegate });
            }
        }

        private void OnTriggerTriggered<T>(T value)
        {
            InvokeTargetMethodOrProperty(value);
        }

        private void InvokeTargetMethodOrProperty(object value)
        {
            if (targetComponent == null || string.IsNullOrEmpty(targetPropertyName)) return;

            if (targetPropertyName == "gameObject.activeSelf" || targetPropertyName == "gameObject.active")
            {
                targetComponent.gameObject.SetActive(Convert.ToBoolean(value));
                return;
            }

            // Try to find a Method first
            if (_targetMethodInfo != null)
            {
                var parameters = _targetMethodInfo.GetParameters();
                if (parameters.Length == 0)
                {
                    _targetMethodInfo.Invoke(targetComponent, null);
                }
                else if (parameters.Length == 1)
                {
                    object valToPass = value;
                    if (converter != null)
                    {
                        valToPass = converter.Convert(valToPass, parameters[0].ParameterType);
                    }
                    try
                    {
                        valToPass = Convert.ChangeType(valToPass, parameters[0].ParameterType);
                    }
                    catch { }
                    _targetMethodInfo.Invoke(targetComponent, new object[] { valToPass });
                }
                return;
            }

            // Fallback to property/field setting
            object finalValue = value;
            if (converter != null)
            {
                Type targetPropType = GetTargetPropertyType();
                finalValue = converter.Convert(value, targetPropType);
            }
            SetTargetValue(finalValue);
        }

        private void InitializeEventBinding()
        {
            object source = GetSource();
            if (source == null) return;

            Type sourceType = source.GetType();

            // Cache source member details
            _cachedSourceMethod = sourceType.GetMethod(sourcePropertyName, BindingFlags.Public | BindingFlags.Instance);
            if (_cachedSourceMethod == null)
            {
                _cachedSourceField = sourceType.GetField(sourcePropertyName, BindingFlags.Public | BindingFlags.Instance);
                _cachedSourceProperty = sourceType.GetProperty(sourcePropertyName, BindingFlags.Public | BindingFlags.Instance);

                if (_cachedSourceField != null)
                    _cachedSourceMemberValue = _cachedSourceField.GetValue(source);
                else if (_cachedSourceProperty != null)
                    _cachedSourceMemberValue = _cachedSourceProperty.GetValue(source);
            }

            if (_cachedSourceMemberValue != null)
            {
                Type memberType = _cachedSourceMemberValue.GetType();
                if (memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(BindableTrigger<>))
                {
                    _cachedTriggerMethod = memberType.GetMethod("Trigger");
                }
            }

            Type targetType = targetComponent.GetType();
            PropertyInfo eventProp = targetType.GetProperty(targetPropertyName, BindingFlags.Public | BindingFlags.Instance);
            FieldInfo eventField = targetType.GetField(targetPropertyName, BindingFlags.Public | BindingFlags.Instance);

            _eventValue = null;
            if (eventProp != null)
                _eventValue = eventProp.GetValue(targetComponent);
            else if (eventField != null)
                _eventValue = eventField.GetValue(targetComponent);

            if (_eventValue == null) return;

            var addListenerMethod = _eventValue.GetType().GetMethod("AddListener");
            if (addListenerMethod == null) return;

            var delegateType = addListenerMethod.GetParameters()[0].ParameterType;

            if (delegateType == typeof(UnityAction))
            {
                UnityAction action = () => OnEventTriggered(null);
                _eventListenerDelegate = action;
                addListenerMethod.Invoke(_eventValue, new object[] { action });
            }
            else if (delegateType.IsGenericType && delegateType.GetGenericTypeDefinition() == typeof(UnityAction<>))
            {
                Type eventParamType = delegateType.GetGenericArguments()[0];
                MethodInfo handlerMethod = this.GetType().GetMethod("OnGenericEventTriggered", BindingFlags.NonPublic | BindingFlags.Instance)
                    .MakeGenericMethod(eventParamType);
                _eventListenerDelegate = Delegate.CreateDelegate(delegateType, this, handlerMethod);
                addListenerMethod.Invoke(_eventValue, new object[] { _eventListenerDelegate });
            }
        }

        private void OnEventTriggered(object dynamicValue)
        {
            ExecuteSourceMethod(dynamicValue);
        }

        private void OnGenericEventTriggered<T>(T dynamicValue)
        {
            ExecuteSourceMethod(dynamicValue);
        }

        private void ExecuteSourceMethod(object dynamicValue)
        {
            object source = GetSource();
            if (source == null) return;

            if (_cachedSourceMethod != null)
            {
                var parameters = _cachedSourceMethod.GetParameters();
                if (parameters.Length == 0)
                {
                    _cachedSourceMethod.Invoke(source, null);
                }
                else if (parameters.Length == 1)
                {
                    Type targetParamType = parameters[0].ParameterType;
                    object valToPass = GetParamValue(dynamicValue, targetParamType);
                    _cachedSourceMethod.Invoke(source, new object[] { valToPass });
                }
                return;
            }

            if (_cachedSourceMemberValue != null)
            {
                if (_cachedSourceMemberValue is BindableTrigger trigger)
                {
                    trigger.Trigger();
                    return;
                }

                if (_cachedTriggerMethod != null)
                {
                    Type triggerParamType = _cachedSourceMemberValue.GetType().GetGenericArguments()[0];
                    object valToPass = GetParamValue(dynamicValue, triggerParamType);
                    _cachedTriggerMethod.Invoke(_cachedSourceMemberValue, new object[] { valToPass });
                    return;
                }

                if (_cachedSourceMemberValue is Delegate del)
                {
                    var parameters = del.Method.GetParameters();
                    if (parameters.Length == 0)
                    {
                        del.DynamicInvoke(null);
                    }
                    else if (parameters.Length == 1)
                    {
                        Type delParamType = parameters[0].ParameterType;
                        object valToPass = GetParamValue(dynamicValue, delParamType);
                        del.DynamicInvoke(new object[] { valToPass });
                    }
                    return;
                }
            }
        }

        private object GetParamValue(object dynamicValue, Type targetType)
        {
            object rawValue = null;
            switch (parameterType)
            {
                case ParameterType.Int:
                    rawValue = constIntParam;
                    break;
                case ParameterType.Float:
                    rawValue = constFloatParam;
                    break;
                case ParameterType.String:
                    rawValue = constStringParam;
                    break;
                case ParameterType.Bool:
                    rawValue = constBoolParam;
                    break;
                case ParameterType.Dynamic:
                    rawValue = dynamicValue;
                    break;
                case ParameterType.None:
                default:
                    rawValue = null;
                    break;
            }

            if (rawValue == null) return null;

            if (converter != null)
            {
                rawValue = converter.Convert(rawValue, targetType);
            }

            try
            {
                if (targetType.IsEnum)
                {
                    if (rawValue is string strValue)
                        return Enum.Parse(targetType, strValue);
                    else
                        return Enum.ToObject(targetType, Convert.ToInt32(rawValue));
                }
                return Convert.ChangeType(rawValue, targetType);
            }
            catch
            {
                return rawValue;
            }
        }

        private void OnSourceValueChanged<T>(T newValue)
        {
            if (targetComponent == null || string.IsNullOrEmpty(targetPropertyName)) return;

            object finalValue = newValue;
            if (converter != null)
            {
                Type targetType = GetTargetPropertyType();
                finalValue = converter.Convert(newValue, targetType);
            }

            SetTargetValue(finalValue);
        }

        private void SetTargetValue(object value)
        {
            if (targetComponent == null) return;

            if (targetPropertyName == "gameObject.activeSelf" || targetPropertyName == "gameObject.active")
            {
                targetComponent.gameObject.SetActive(Convert.ToBoolean(value));
                return;
            }

            if (_targetPropertyInfo != null && _targetPropertyInfo.CanWrite)
            {
                try
                {
                    object finalValue = value;
                    if (_targetPropertyInfo.PropertyType.IsEnum)
                    {
                        if (value is string strValue)
                            finalValue = Enum.Parse(_targetPropertyInfo.PropertyType, strValue);
                        else
                            finalValue = Enum.ToObject(_targetPropertyInfo.PropertyType, Convert.ToInt32(value));
                    }
                    else
                    {
                        finalValue = Convert.ChangeType(value, _targetPropertyInfo.PropertyType);
                    }
                    _targetPropertyInfo.SetValue(targetComponent, finalValue);
                }
                catch
                {
                    _targetPropertyInfo.SetValue(targetComponent, value);
                }
                return;
            }

            if (_targetFieldInfo != null)
            {
                try
                {
                    object finalValue = value;
                    if (_targetFieldInfo.FieldType.IsEnum)
                    {
                        if (value is string strValue)
                            finalValue = Enum.Parse(_targetFieldInfo.FieldType, strValue);
                        else
                            finalValue = Enum.ToObject(_targetFieldInfo.FieldType, Convert.ToInt32(value));
                    }
                    else
                    {
                        finalValue = Convert.ChangeType(value, _targetFieldInfo.FieldType);
                    }
                    _targetFieldInfo.SetValue(targetComponent, finalValue);
                }
                catch
                {
                    _targetFieldInfo.SetValue(targetComponent, value);
                }
            }
        }

        private Type GetTargetPropertyType()
        {
            if (targetPropertyName == "gameObject.activeSelf" || targetPropertyName == "gameObject.active")
            {
                return typeof(bool);
            }

            if (_targetPropertyInfo != null) return _targetPropertyInfo.PropertyType;
            if (_targetFieldInfo != null) return _targetFieldInfo.FieldType;
            return typeof(object);
        }

        private void SetupTwoWayBinding(Type type)
        {
            if (targetComponent is UnityEngine.UI.InputField inputField && type == typeof(string))
            {
                inputField.onValueChanged.AddListener(OnInputFieldValueChanged);
            }
            else if (targetComponent is TMP_InputField tmpInputField && type == typeof(string))
            {
                tmpInputField.onValueChanged.AddListener(OnInputFieldValueChanged);
            }
            else if (targetComponent is UnityEngine.UI.Toggle toggle && type == typeof(bool))
            {
                toggle.onValueChanged.AddListener(OnToggleValueChanged);
            }
            else if (targetComponent is UnityEngine.UI.Slider slider && type == typeof(float))
            {
                slider.onValueChanged.AddListener(OnSliderValueChanged);
            }
            else if (targetComponent is UnityEngine.UI.Dropdown dropdown && type == typeof(int))
            {
                dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            }
            else if (targetComponent is TMP_Dropdown tmpDropdown && type == typeof(int))
            {
                tmpDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            }
            else if (targetComponent is UnityEngine.UI.Scrollbar scrollbar && type == typeof(float))
            {
                scrollbar.onValueChanged.AddListener(OnScrollbarValueChanged);
            }
        }

        private void OnInputFieldValueChanged(string value) => UpdateSourceValue(value);
        private void OnToggleValueChanged(bool value) => UpdateSourceValue(value);
        private void OnSliderValueChanged(float value) => UpdateSourceValue(value);
        private void OnDropdownValueChanged(int value) => UpdateSourceValue(value);
        private void OnScrollbarValueChanged(float value) => UpdateSourceValue(value);

        private void UpdateSourceValue(object value)
        {
            object source = GetSource();
            if (source == null || _sourceBindable == null || _sourceValuePropertyInfo == null) return;

            object convertedValue = value;
            if (converter != null)
            {
                Type sourceType = _sourceBindable.GetType().GetGenericArguments()[0];
                convertedValue = converter.ConvertBack(value, sourceType);
            }

            try
            {
                object finalValue = convertedValue;
                if (_sourceValuePropertyInfo.PropertyType.IsEnum)
                {
                    if (convertedValue is string strValue)
                        finalValue = Enum.Parse(_sourceValuePropertyInfo.PropertyType, strValue);
                    else
                        finalValue = Enum.ToObject(_sourceValuePropertyInfo.PropertyType, Convert.ToInt32(convertedValue));
                }
                else
                {
                    finalValue = Convert.ChangeType(convertedValue, _sourceValuePropertyInfo.PropertyType);
                }
                _sourceValuePropertyInfo.SetValue(_sourceBindable, finalValue);
            }
            catch
            {
                _sourceValuePropertyInfo.SetValue(_sourceBindable, convertedValue);
            }
        }

        private void UnbindCurrent()
        {
            if (_sourceBindable != null && _sourceUnbindMethod != null && _onValueChangedDelegate != null)
            {
                try
                {
                    _sourceUnbindMethod.Invoke(_sourceBindable, new object[] { _onValueChangedDelegate });
                }
                catch { }
            }

            if (_eventValue != null && _eventListenerDelegate != null)
            {
                try
                {
                    MethodInfo removeListenerMethod = _eventValue.GetType().GetMethod("RemoveListener", new Type[] { _eventListenerDelegate.GetType() });
                    if (removeListenerMethod != null)
                    {
                        removeListenerMethod.Invoke(_eventValue, new object[] { _eventListenerDelegate });
                    }
                }
                catch { }
            }

            _sourceBindable = null;
            _sourceUnbindMethod = null;
            _onValueChangedDelegate = null;
            _eventListenerDelegate = null;
            _eventValue = null;
        }

        private void OnDestroy()
        {
            UnbindCurrent();

            if (targetComponent is UnityEngine.UI.InputField inputField)
                inputField.onValueChanged.RemoveListener(OnInputFieldValueChanged);
            else if (targetComponent is TMP_InputField tmpInputField)
                tmpInputField.onValueChanged.RemoveListener(OnInputFieldValueChanged);
            else if (targetComponent is UnityEngine.UI.Toggle toggle)
                toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            else if (targetComponent is UnityEngine.UI.Slider slider)
                slider.onValueChanged.RemoveListener(OnSliderValueChanged);
            else if (targetComponent is UnityEngine.UI.Dropdown dropdown)
                dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
            else if (targetComponent is TMP_Dropdown tmpDropdown)
                tmpDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
            else if (targetComponent is UnityEngine.UI.Scrollbar scrollbar)
                scrollbar.onValueChanged.RemoveListener(OnScrollbarValueChanged);
        }
    }
}
