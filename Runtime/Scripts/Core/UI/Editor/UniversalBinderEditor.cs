#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace OSK
{
    [CustomEditor(typeof(UniversalBinder))]
    public class UniversalBinderEditor : Editor
    {
        private UniversalBinder _binder;

        private void OnEnable()
        {
            _binder = (UniversalBinder)target;
        }

        private Type ResolveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;
            Type t = Type.GetType(typeName);
            if (t != null) return t;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                t = assembly.GetType(typeName);
                if (t != null) return t;

                t = assembly.GetType("OSK." + typeName);
                if (t != null) return t;
            }
            return null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // --- 1. GENERAL BINDING SETTINGS ---
            EditorGUILayout.LabelField("Binding General Settings", EditorStyles.boldLabel);
            
            UniversalBinder.BindingMode newMode = (UniversalBinder.BindingMode)EditorGUILayout.EnumPopup("Binding Mode", _binder.mode);
            if (newMode != _binder.mode)
            {
                Undo.RecordObject(_binder, "Change Binding Mode");
                _binder.mode = newMode;
                _binder.targetPropertyName = "";
                _binder.sourcePropertyName = "";
                EditorUtility.SetDirty(_binder);
            }

            ValueConverter newConverter = (ValueConverter)EditorGUILayout.ObjectField("Converter", _binder.converter, typeof(ValueConverter), false);
            if (newConverter != _binder.converter)
            {
                Undo.RecordObject(_binder, "Change Converter");
                _binder.converter = newConverter;
                EditorUtility.SetDirty(_binder);
            }

            EditorGUILayout.Space();

            // --- 2. SOURCE CONFIGURATION ---
            EditorGUILayout.LabelField("Source Settings (ViewModel / Model)", EditorStyles.boldLabel);

            UniversalBinder.SourceResolutionMode newResMode = (UniversalBinder.SourceResolutionMode)EditorGUILayout.EnumPopup("Resolution Mode", _binder.sourceResolution);
            if (newResMode != _binder.sourceResolution)
            {
                Undo.RecordObject(_binder, "Change Resolution Mode");
                _binder.sourceResolution = newResMode;
                _binder.sourcePropertyName = "";
                EditorUtility.SetDirty(_binder);
            }

            Type resolvedSourceType = null;

            if (_binder.sourceResolution == UniversalBinder.SourceResolutionMode.AssignManual)
            {
                // GameObject field
                GameObject sourceGo = null;
                if (_binder.sourceComponent != null) sourceGo = _binder.sourceComponent.gameObject;

                GameObject newSourceGo = (GameObject)EditorGUILayout.ObjectField("Source GameObject", sourceGo, typeof(GameObject), true);
                if (newSourceGo != sourceGo)
                {
                    Undo.RecordObject(_binder, "Change Source GameObject");
                    if (newSourceGo != null)
                    {
                        var components = newSourceGo.GetComponents<Component>();
                        _binder.sourceComponent = null;
                        foreach (var comp in components)
                        {
                            if (comp != null && comp != _binder)
                            {
                                _binder.sourceComponent = comp;
                                break;
                            }
                        }
                    }
                    else
                    {
                        _binder.sourceComponent = null;
                    }
                    _binder.sourcePropertyName = "";
                    EditorUtility.SetDirty(_binder);
                }

                // Component dropdown
                if (_binder.sourceComponent != null)
                {
                    var components = _binder.sourceComponent.gameObject.GetComponents<Component>();
                    List<string> compNames = new List<string>();
                    int selectedCompIdx = 0;

                    for (int i = 0; i < components.Length; i++)
                    {
                        if (components[i] == null) continue;
                        compNames.Add(components[i].GetType().Name);
                        if (components[i] == _binder.sourceComponent)
                        {
                            selectedCompIdx = compNames.Count - 1;
                        }
                    }

                    int newCompIdx = EditorGUILayout.Popup("Source Component", selectedCompIdx, compNames.ToArray());
                    if (newCompIdx != selectedCompIdx)
                    {
                        Undo.RecordObject(_binder, "Change Source Component");
                        int idx = 0;
                        for (int i = 0; i < components.Length; i++)
                        {
                            if (components[i] == null) continue;
                            if (idx == newCompIdx)
                            {
                                _binder.sourceComponent = components[i];
                                break;
                            }
                            idx++;
                        }
                        _binder.sourcePropertyName = "";
                        EditorUtility.SetDirty(_binder);
                    }

                    resolvedSourceType = _binder.sourceComponent.GetType();
                }
            }
            else
            {
                // Text Field for Source Type Name
                string newTypeName = EditorGUILayout.TextField("Source Type Name", _binder.sourceTypeName);
                if (newTypeName != _binder.sourceTypeName)
                {
                    Undo.RecordObject(_binder, "Change Source Type Name");
                    _binder.sourceTypeName = newTypeName;
                    _binder.sourcePropertyName = "";
                    EditorUtility.SetDirty(_binder);
                }

                resolvedSourceType = ResolveType(_binder.sourceTypeName);
            }

            // Source Property/Trigger/Method dropdown
            if (resolvedSourceType != null)
            {
                List<string> bindablePropNames = new List<string>();

                if (_binder.mode == UniversalBinder.BindingMode.Event)
                {
                    MethodInfo[] methods = resolvedSourceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var method in methods)
                    {
                        Type declType = method.DeclaringType;
                        if (declType == typeof(object) || declType == typeof(Object) || 
                            declType == typeof(MonoBehaviour) || declType == typeof(Component) || 
                            declType == typeof(Behaviour))
                        {
                            continue;
                        }

                        if (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
                        {
                            continue;
                        }

                        bindablePropNames.Add(method.Name);
                    }

                    FieldInfo[] fields = resolvedSourceType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var field in fields)
                    {
                        if (field.FieldType == typeof(BindableTrigger) || 
                            (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(BindableTrigger<>)) ||
                            typeof(MulticastDelegate).IsAssignableFrom(field.FieldType))
                        {
                            bindablePropNames.Add(field.Name);
                        }
                    }

                    PropertyInfo[] props = resolvedSourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in props)
                    {
                        if (prop.PropertyType == typeof(BindableTrigger) || 
                            (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(BindableTrigger<>)) ||
                            typeof(MulticastDelegate).IsAssignableFrom(prop.PropertyType))
                        {
                            bindablePropNames.Add(prop.Name);
                        }
                    }
                }
                else
                {
                    FieldInfo[] fields = resolvedSourceType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var field in fields)
                    {
                        if ((field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(BindableProperty<>)) ||
                            field.FieldType == typeof(BindableTrigger) ||
                            (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(BindableTrigger<>)))
                        {
                            bindablePropNames.Add(field.Name);
                        }
                    }

                    PropertyInfo[] props = resolvedSourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in props)
                    {
                        if ((prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(BindableProperty<>)) ||
                            prop.PropertyType == typeof(BindableTrigger) ||
                            (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(BindableTrigger<>)))
                        {
                            bindablePropNames.Add(prop.Name);
                        }
                    }
                }

                bindablePropNames.Sort();

                if (bindablePropNames.Count > 0)
                {
                    int selectedPropIdx = bindablePropNames.IndexOf(_binder.sourcePropertyName);
                    if (selectedPropIdx < 0) selectedPropIdx = 0;

                    string labelName = _binder.mode == UniversalBinder.BindingMode.Event ? "Source Method / Trigger" : "Source Property / Trigger";
                    int newPropIdx = EditorGUILayout.Popup(labelName, selectedPropIdx, bindablePropNames.ToArray());
                    if (newPropIdx != selectedPropIdx || string.IsNullOrEmpty(_binder.sourcePropertyName))
                    {
                        Undo.RecordObject(_binder, "Change Source Property Name");
                        _binder.sourcePropertyName = bindablePropNames[newPropIdx];
                        EditorUtility.SetDirty(_binder);
                    }
                }
                else
                {
                    string msg = _binder.mode == UniversalBinder.BindingMode.Event 
                        ? "No public methods or BindableTriggers found on source type." 
                        : "No BindableProperties or BindableTriggers found on source type.";
                    EditorGUILayout.HelpBox(msg, MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Please assign Source GameObject or write Source Type Name.", MessageType.Info);
            }

            EditorGUILayout.Space();

            // --- 3. TARGET CONFIGURATION ---
            EditorGUILayout.LabelField("Target Settings (UI Component)", EditorStyles.boldLabel);
            
            GameObject targetGo = _binder.targetComponent != null ? _binder.targetComponent.gameObject : _binder.gameObject;
            GameObject newTargetGo = (GameObject)EditorGUILayout.ObjectField("Target GameObject", targetGo, typeof(GameObject), true);
            if (newTargetGo != targetGo)
            {
                Undo.RecordObject(_binder, "Change Target GameObject");
                if (newTargetGo != null)
                {
                    var components = newTargetGo.GetComponents<Component>();
                    _binder.targetComponent = null;
                    foreach (var comp in components)
                    {
                        if (comp != null && comp != _binder)
                        {
                            _binder.targetComponent = comp;
                            break;
                        }
                    }
                }
                else
                {
                    _binder.targetComponent = null;
                }
                _binder.targetPropertyName = "";
                EditorUtility.SetDirty(_binder);
            }

            if (_binder.targetComponent != null)
            {
                var components = _binder.targetComponent.gameObject.GetComponents<Component>();
                List<string> compNames = new List<string>();
                int selectedCompIdx = 0;

                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null) continue;
                    compNames.Add(components[i].GetType().Name);
                    if (components[i] == _binder.targetComponent)
                    {
                        selectedCompIdx = compNames.Count - 1;
                    }
                }

                int newCompIdx = EditorGUILayout.Popup("Target Component", selectedCompIdx, compNames.ToArray());
                if (newCompIdx != selectedCompIdx)
                {
                    Undo.RecordObject(_binder, "Change Target Component");
                    int idx = 0;
                    for (int i = 0; i < components.Length; i++)
                    {
                        if (components[i] == null) continue;
                        if (idx == newCompIdx)
                        {
                            _binder.targetComponent = components[i];
                            break;
                        }
                        idx++;
                    }
                    _binder.targetPropertyName = "";
                    EditorUtility.SetDirty(_binder);
                }

                List<string> targetPropNames = new List<string>();
                Type targetType = _binder.targetComponent.GetType();

                if (_binder.mode == UniversalBinder.BindingMode.Event)
                {
                    PropertyInfo[] targetProps = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in targetProps)
                    {
                        if (typeof(UnityEventBase).IsAssignableFrom(prop.PropertyType))
                        {
                            targetPropNames.Add(prop.Name);
                        }
                    }

                    FieldInfo[] targetFields = targetType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var field in targetFields)
                    {
                        if (typeof(UnityEventBase).IsAssignableFrom(field.FieldType))
                        {
                            targetPropNames.Add(field.Name);
                        }
                    }
                }
                else
                {
                    PropertyInfo[] targetProps = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in targetProps)
                    {
                        if (prop.CanWrite && prop.GetIndexParameters().Length == 0)
                        {
                            targetPropNames.Add(prop.Name);
                        }
                    }

                    FieldInfo[] targetFields = targetType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var field in targetFields)
                    {
                        targetPropNames.Add(field.Name);
                    }

                    MethodInfo[] methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var method in methods)
                    {
                        Type declType = method.DeclaringType;
                        if (declType == typeof(object) || declType == typeof(Object) || 
                            declType == typeof(MonoBehaviour) || declType == typeof(Component) || 
                            declType == typeof(Behaviour))
                        {
                            continue;
                        }

                        if (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
                        {
                            continue;
                        }

                        var parameters = method.GetParameters();
                        if (parameters.Length <= 1)
                        {
                            targetPropNames.Add(method.Name);
                        }
                    }

                    targetPropNames.Add("gameObject.activeSelf");
                }

                targetPropNames.Sort();

                if (targetPropNames.Count > 0)
                {
                    int selectedPropIdx = targetPropNames.IndexOf(_binder.targetPropertyName);
                    if (selectedPropIdx < 0) selectedPropIdx = 0;

                    string labelName = _binder.mode == UniversalBinder.BindingMode.Event ? "Target Event" : "Target Property / Method";
                    int newPropIdx = EditorGUILayout.Popup(labelName, selectedPropIdx, targetPropNames.ToArray());
                    if (newPropIdx != selectedPropIdx || string.IsNullOrEmpty(_binder.targetPropertyName))
                    {
                        Undo.RecordObject(_binder, "Change Target Property Name");
                        _binder.targetPropertyName = targetPropNames[newPropIdx];
                        EditorUtility.SetDirty(_binder);
                    }
                }
                else
                {
                    string msg = _binder.mode == UniversalBinder.BindingMode.Event 
                        ? "No events found on selected target." 
                        : "No writeable properties, fields, or methods found on selected target.";
                    EditorGUILayout.HelpBox(msg, MessageType.Warning);
                }
            }

            // --- 4. EVENT PARAMETER CONFIGURATION ---
            if (_binder.mode == UniversalBinder.BindingMode.Event)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Event Parameter Settings", EditorStyles.boldLabel);

                UniversalBinder.ParameterType newParamType = (UniversalBinder.ParameterType)EditorGUILayout.EnumPopup("Parameter Type", _binder.parameterType);
                if (newParamType != _binder.parameterType)
                {
                    Undo.RecordObject(_binder, "Change Parameter Type");
                    _binder.parameterType = newParamType;
                    EditorUtility.SetDirty(_binder);
                }

                switch (_binder.parameterType)
                {
                    case UniversalBinder.ParameterType.Int:
                        int newInt = EditorGUILayout.IntField("Constant Int", _binder.constIntParam);
                        if (newInt != _binder.constIntParam)
                        {
                            Undo.RecordObject(_binder, "Change Constant Int");
                            _binder.constIntParam = newInt;
                            EditorUtility.SetDirty(_binder);
                        }
                        break;
                    case UniversalBinder.ParameterType.Float:
                        float newFloat = EditorGUILayout.FloatField("Constant Float", _binder.constFloatParam);
                        if (newFloat != _binder.constFloatParam)
                        {
                            Undo.RecordObject(_binder, "Change Constant Float");
                            _binder.constFloatParam = newFloat;
                            EditorUtility.SetDirty(_binder);
                        }
                        break;
                    case UniversalBinder.ParameterType.String:
                        string newStr = EditorGUILayout.TextField("Constant String", _binder.constStringParam);
                        if (newStr != _binder.constStringParam)
                        {
                            Undo.RecordObject(_binder, "Change Constant String");
                            _binder.constStringParam = newStr;
                            EditorUtility.SetDirty(_binder);
                        }
                        break;
                    case UniversalBinder.ParameterType.Bool:
                        bool newBool = EditorGUILayout.Toggle("Constant Bool", _binder.constBoolParam);
                        if (newBool != _binder.constBoolParam)
                        {
                            Undo.RecordObject(_binder, "Change Constant Bool");
                            _binder.constBoolParam = newBool;
                            EditorUtility.SetDirty(_binder);
                        }
                        break;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif