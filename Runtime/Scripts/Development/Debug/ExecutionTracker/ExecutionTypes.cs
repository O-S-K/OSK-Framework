#if UNITY_EDITOR
namespace OSK.ExecutionDebug
{
    public enum ExecutionPhase
    {
        Awake,
        OnEnable,
        Start
    }

    public enum ExecutionSource
    {
        Real,   // component có method thật
        Hook    // bị log do ExecutionHook
    }
}
#endif

