#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace OSK
{
    [CustomEditor(typeof(EventBusManager))]
    public class EventBusEditor : Editor
    {
        private EventBusManager _eventBusManager;

        private FieldInfo _syncSubscribersField;
        private FieldInfo _asyncSubscribersField;
        private FieldInfo _lastEventsField;

        private bool _showSync = true;
        private bool _showAsync = true;
        private bool _showLastEvents = true;

        private string _testEventTypeName = "";

        private void OnEnable()
        {
            _eventBusManager = (EventBusManager)target;

            var type = typeof(EventBusManager);
            _syncSubscribersField = type.GetField("syncSubscribers", BindingFlags.NonPublic | BindingFlags.Instance);
            _asyncSubscribersField = type.GetField("asyncSubscribers", BindingFlags.NonPublic | BindingFlags.Instance);
            _lastEventsField = type.GetField("lastEvents", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("=== Event Bus Debug Tool ===", EditorStyles.boldLabel);

            DrawDebugButtons();

            _showSync = EditorGUILayout.Foldout(_showSync, "Sync Subscribers");
            if (_showSync)
                DisplaySubscribers(_syncSubscribersField);

            EditorGUILayout.Space();
            _showAsync = EditorGUILayout.Foldout(_showAsync, "Async Subscribers");
            if (_showAsync)
                DisplaySubscribers(_asyncSubscribersField);

            EditorGUILayout.Space();
            _showLastEvents = EditorGUILayout.Foldout(_showLastEvents, "Last Published Events");
            if (_showLastEvents)
                DisplayLastEvents();

            EditorGUILayout.Space();
            DrawTestEventPublisher();
        }

        #region --- Subscribers Display ---

        private void DisplaySubscribers(FieldInfo field)
        {
            var subscribers = field.GetValue(_eventBusManager) as Dictionary<Type, List<Delegate>>;
            if (subscribers == null || subscribers.Count == 0)
            {
                EditorGUILayout.LabelField("    No registered listeners", EditorStyles.miniLabel);
                return;
            }

            foreach (var kvp in subscribers)
            {
                Type eventType = kvp.Key;
                List<Delegate> handlers = kvp.Value;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Event",$"{eventType.Name}  ({handlers.Count} listeners)", EditorStyles.boldLabel);

                foreach (var handler in handlers)
                {
                    if (handler == null) continue;

                    string methodName = handler.Method.Name;
                    string declaringType = handler.Method.DeclaringType != null
                        ? handler.Method.DeclaringType.FullName
                        : "Unknown";

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Event",$"- {declaringType}.{methodName}", EditorStyles.miniLabel);

                    // Nút "Ping" để highlight object nếu callback thuộc một MonoBehaviour
                    if (handler.Target is UnityEngine.Object unityObj)
                    {
                        if (GUILayout.Button("Ping", GUILayout.Width(45)))
                            EditorGUIUtility.PingObject(unityObj);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }
        }

        #endregion

        #region --- Last Events ---

        private void DisplayLastEvents()
        {
            var lastEvents = _lastEventsField.GetValue(_eventBusManager) as Dictionary<Type, GameEvent>;

            if (lastEvents == null || lastEvents.Count == 0)
            {
                EditorGUILayout.LabelField("    No events published yet", EditorStyles.miniLabel);
                return;
            }

            foreach (var kvp in lastEvents)
            {
                Type eventType = kvp.Key;
                var gameEvent = kvp.Value;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Event",$"{eventType.Name}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Event",$"Last Event: {gameEvent}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
            }
        }

        #endregion

        #region --- Debug Buttons ---

        private void DrawDebugButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear Sync"))
            {
                ClearDictionary(_syncSubscribersField);
            }

            if (GUILayout.Button("Clear Async"))
            {
                ClearDictionary(_asyncSubscribersField);
            }

            if (GUILayout.Button("Clear All"))
            {
                ClearDictionary(_syncSubscribersField);
                ClearDictionary(_asyncSubscribersField);
            }

            if (GUILayout.Button("Clear Last Events"))
            {
                ClearDictionary(_lastEventsField);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ClearDictionary(FieldInfo field)
        {
            var dict = field.GetValue(_eventBusManager) as System.Collections.IDictionary;
            dict?.Clear();
            EditorUtility.SetDirty(_eventBusManager);
        }

        #endregion

        #region --- Test Publisher ---

        private void DrawTestEventPublisher()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Test Event Publisher", EditorStyles.boldLabel);

            _testEventTypeName = EditorGUILayout.TextField("Event Type Name", _testEventTypeName);

            if (GUILayout.Button("Publish Test Event"))
            {
                if (string.IsNullOrEmpty(_testEventTypeName))
                {
                    Debug.LogWarning("[EventBusEditor] Event type name is empty!");
                    return;
                }

                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                Type eventType = null;
                foreach (var asm in assemblies)
                {
                    eventType = asm.GetType(_testEventTypeName);
                    if (eventType != null) break;
                }

                if (eventType == null)
                {
                    Logg.LogError("Event",$"[EventBusEditor] Event type '{_testEventTypeName}' not found!");
                    return;
                }

                if (!typeof(GameEvent).IsAssignableFrom(eventType))
                {
                    Logg.LogError("Event",$"[EventBusEditor] Type '{_testEventTypeName}' is not a GameEvent!");
                    return;
                }

                try
                {
                    var instance = Activator.CreateInstance(eventType) as GameEvent;
                    var method = typeof(EventBusManager).GetMethod("Publish")?.MakeGenericMethod(eventType);
                    method?.Invoke(_eventBusManager, new object[] { instance });
                    Logg.Log("Event",$"[EventBusEditor] Published test event: {eventType.Name}");
                }
                catch (Exception ex)
                {
                    Logg.LogError("Event",$"[EventBusEditor] Failed to publish test event: {ex}");
                }
            }
        }

        #endregion
    }
}
#endif
