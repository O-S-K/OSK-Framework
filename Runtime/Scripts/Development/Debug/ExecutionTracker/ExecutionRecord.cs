#if UNITY_EDITOR
using UnityEngine;

namespace OSK.ExecutionDebug
{
    public struct ExecutionRecord
    {
        public int frame;
        public float time;
        public ExecutionPhase phase;
        public ExecutionSource source;

        public MonoBehaviour target;
        public int executionOrder;
    }
}
#endif