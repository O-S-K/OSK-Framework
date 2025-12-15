using System;
using UnityEngine;

namespace OSK
{
    public static class UniTaskChainExtensions
    {
        public static UniTaskFlow StartFlowTask(this MonoBehaviour mono)
        {
            return UniTaskChainPool.Spawn(mono);
        }
        
        public static void StartFlowDelay(this MonoBehaviour owner, float seconds, Action callback, bool ignoreTimeScale = false)
        {
            owner.StartFlowTask()
                .Wait(seconds)
                .Call(callback)
                .Run(ignoreTimeScale);
        }
    }
}
