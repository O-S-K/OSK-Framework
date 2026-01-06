#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using OSK.ExecutionDebug;
using UnityEditor;
using UnityEngine;

public class ExecutionOrderWindow : EditorWindow
{
    [MenuItem("OSK-Framework/Debug/Execution Order Viewer")]
    static void Open() => GetWindow<ExecutionOrderWindow>("Execution Order");

    Vector2 _scroll;
    bool _showHook = true;
    bool _hideHookStart = true;

    readonly Dictionary<int, bool> _frameFold = new();
    readonly Dictionary<(int, ExecutionPhase), bool> _phaseFold = new();
    readonly Dictionary<(int, ExecutionPhase, int), bool> _orderFold = new();
    
    // ExecutionInjector.IsInjected = true; // to see hook records

    bool GetFold<T>(Dictionary<T, bool> d, T key, bool def = true)
    {
        if (!d.TryGetValue(key, out var v))
            d[key] = def;
        return d[key];
    }

    void SetFold<T>(Dictionary<T, bool> d, T key, bool v)
        => d[key] = v;

    void OnGUI()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear", GUILayout.Width(80)))
            ExecutionTracker.Clear();

        _showHook = GUILayout.Toggle(_showHook, "Show Hook");
        _hideHookStart = GUILayout.Toggle(_hideHookStart, "Hide Hook Start");

        GUILayout.Label($"Records: {ExecutionTracker.Records.Count}");
        GUILayout.EndHorizontal();

        _scroll = GUILayout.BeginScrollView(_scroll);

        var records = ExecutionTracker.Records
            .Where(r => _showHook || r.source == ExecutionSource.Real)
            .Where(r => !_hideHookStart ||
                        !(r.source == ExecutionSource.Hook && r.phase == ExecutionPhase.Start));

        var frameGroups = records
            .GroupBy(r => r.frame)
            .OrderBy(g => g.Key);

        foreach (var fg in frameGroups)
        {
            bool frameOpen = GetFold(_frameFold, fg.Key);
            frameOpen = EditorGUILayout.Foldout(frameOpen, $"Frame {fg.Key}", true);
            SetFold(_frameFold, fg.Key, frameOpen);
            if (!frameOpen) continue;

            foreach (var pg in fg.GroupBy(r => r.phase).OrderBy(g => g.Key))
            {
                GUI.color = pg.Key switch
                {
                    ExecutionPhase.Awake => Color.cyan,
                    ExecutionPhase.OnEnable => Color.yellow,
                    ExecutionPhase.Start => Color.green,
                    _ => Color.white
                };

                var pk = (fg.Key, pg.Key);
                bool phaseOpen = GetFold(_phaseFold, pk);
                phaseOpen = EditorGUILayout.Foldout(
                    phaseOpen, $"  {pg.Key}", true);
                SetFold(_phaseFold, pk, phaseOpen);

                GUI.color = Color.white;
                if (!phaseOpen) continue;

                foreach (var og in pg.GroupBy(r => r.executionOrder).OrderBy(g => g.Key))
                {
                    var ok = (fg.Key, pg.Key, og.Key);
                    bool orderOpen = GetFold(_orderFold, ok);
                    orderOpen = EditorGUILayout.Foldout(
                        orderOpen, $"    order = {og.Key}", true);
                    SetFold(_orderFold, ok, orderOpen);

                    if (!orderOpen) continue;

                    foreach (var r in og)
                    {
                        GUI.color = r.source == ExecutionSource.Hook
                            ? new Color(1f, 0.75f, 0.75f)
                            : Color.white;

                        GUILayout.Label(
                            $"      - {r.target.GetType().Name}  [{r.source}]");

                        GUI.color = Color.white;
                    }
                }
            }
        }

        GUILayout.EndScrollView();
    }
}
#endif
