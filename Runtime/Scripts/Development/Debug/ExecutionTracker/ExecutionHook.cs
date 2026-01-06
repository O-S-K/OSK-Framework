#if UNITY_EDITOR
using System.Reflection;
using UnityEngine;
using OSK.ExecutionDebug;

public class ExecutionHook : MonoBehaviour
{
    static readonly BindingFlags Flags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    void Awake()     => Capture(ExecutionPhase.Awake);
    void OnEnable()  => Capture(ExecutionPhase.OnEnable);
    void Start()     => Capture(ExecutionPhase.Start);

    void Capture(ExecutionPhase phase)
    {
        var components = GetComponents<MonoBehaviour>();

        foreach (var mb in components)
        {
            if (mb == null || mb == this) continue;

            bool hasRealMethod =
                mb.GetType().GetMethod(phase.ToString(), Flags) != null;

            ExecutionTracker.Log(
                mb,
                phase,
                hasRealMethod ? ExecutionSource.Real : ExecutionSource.Hook
            );
        }
    }
}
#endif