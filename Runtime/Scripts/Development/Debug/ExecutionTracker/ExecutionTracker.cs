#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace OSK.ExecutionDebug
{
    public static class ExecutionTracker
    {
        public static readonly List<ExecutionRecord> Records = new();

        public static void Log(
            MonoBehaviour target,
            ExecutionPhase phase,
            ExecutionSource source)
        {
            if (target == null) return;

            int order = 0;
            var script = MonoScript.FromMonoBehaviour(target);
            if (script != null)
                order = MonoImporter.GetExecutionOrder(script);

            Records.Add(new ExecutionRecord
            {
                frame = Time.frameCount,
                time = Time.realtimeSinceStartup,
                phase = phase,
                source = source,
                target = target,
                executionOrder = order
            });
        }

        public static void Clear() => Records.Clear();
    }
}
#endif