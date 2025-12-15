#if UNITY_EDITOR
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace OSK
{
    public class FlowTaskExample : MonoBehaviour
    {
        public bool stop1;
        public bool stop2;
        public bool stop3;

        void Start()
        {
            this.StartFlowTask()
                .Wait(1f)
                .Parallel(
                    PrintA,
                    PrintB,
                    PrintC
                )
                .While(() => !stop1, c =>
                {
                    c.Play(PrintLoop)
                        .Wait(0.5f);
                })
                .WaitUntil(() => stop1 && !stop2)
                .While(() => !stop2, c =>
                {
                    c.Parallel(
                        PrintA,
                        PrintB,
                        PrintC
                    ).Wait(0.2f);
                })
                .StopChainIf(() => stop3 || !gameObject.activeSelf)  
                .Call(() => Debug.Log("Chain End"))
                .Run(isIgnoreTimeScale: true);
        }

        UniTask PrintA(CancellationToken ct)
        {
            Debug.Log("A");
            return UniTask.Delay(300, cancellationToken: ct);
        }

        UniTask PrintB(CancellationToken ct)
        {
            Debug.Log("B");
            return UniTask.Delay(400, cancellationToken: ct);
        }

        UniTask PrintC(CancellationToken ct)
        {
            Debug.Log("C");
            return UniTask.Delay(500, cancellationToken: ct);
        }

        UniTask PrintLoop(CancellationToken ct)
        {
            Debug.Log("Looping...");
            return UniTask.CompletedTask;
        }
    }
}
#endif