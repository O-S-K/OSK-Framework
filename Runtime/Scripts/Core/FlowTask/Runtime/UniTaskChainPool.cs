using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    public static class UniTaskChainPool
    {
        static readonly Stack<UniTaskFlow> _pool = new();

        public static UniTaskFlow Spawn(MonoBehaviour owner)
        {
            UniTaskFlow flow;
            if (_pool.Count > 0)
                flow = _pool.Pop();
            else
                flow = new UniTaskFlow();

            flow.Setup(owner);
            return flow;
        }

        public static void Despawn(UniTaskFlow flow)
        {
            flow.Clear();
            _pool.Push(flow);
        }
    }
}