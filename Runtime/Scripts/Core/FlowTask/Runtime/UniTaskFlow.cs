using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace OSK
{
    internal class BreakState
    {
         public bool BreakChain;
    }
    
    public sealed class UniTaskFlow
    {
        private Queue<Func<CancellationToken, UniTask>> _queue = new();
        private CancellationToken _token;
        private MonoBehaviour _owner;
        private BreakState _breakState;
        private bool _ignoreTimeScale;

        public string OwnerName => _owner ? _owner.name : "null";
        public int PendingStepCount => _queue.Count;

        internal void Setup(MonoBehaviour owner)
        {
            _owner = owner;
            _token = owner.GetCancellationTokenOnDestroy();
            _breakState = new BreakState();
        }

        internal void Clear()
        { 
            _queue.Clear();
            _owner = null;
        }

        // ================= API =================

        public UniTaskFlow Play(Func<CancellationToken, UniTask> task)
        {
            _queue.Enqueue(task);
            return this;
        }

        public UniTaskFlow Wait(float sec)
        {
            _queue.Enqueue(ct =>
                UniTask.Delay( TimeSpan.FromSeconds(sec),
                    ignoreTimeScale: _ignoreTimeScale,
                    cancellationToken: ct));
            return this;
        }


        public UniTaskFlow WaitUntil(Func<bool> condition)
        {
            _queue.Enqueue(ct =>
                UniTask.WaitUntil(condition, cancellationToken: ct));
            return this;
        }

        public UniTaskFlow Call(Action action)
        {
            _queue.Enqueue(ct =>
            {
                action?.Invoke();
                return UniTask.CompletedTask;
            });
            return this;
        }

        public UniTaskFlow Parallel(params Func<CancellationToken, UniTask>[] tasks)
        {
            _queue.Enqueue(async ct =>
            {
                var list = new UniTask[tasks.Length];
                for (int i = 0; i < tasks.Length; i++)
                    list[i] = tasks[i](ct);

                await UniTask.WhenAll(list);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            });

            return this;
        }


        public UniTaskFlow If(
            Func<bool> condition,
            Action<UniTaskFlow> then,
            Action<UniTaskFlow> otherwise = null)
        {
            _queue.Enqueue(async ct =>
            {
                ct.ThrowIfCancellationRequested();

                var chain = UniTaskChainPool.Spawn(_owner);
                chain._breakState = _breakState;  

                if (condition())
                    then?.Invoke(chain);
                else
                    otherwise?.Invoke(chain);

                await chain.RunInternal();
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            });

            return this;
        }


        public UniTaskFlow Repeat(int count, Action<UniTaskFlow> body)
        {
            _queue.Enqueue(async ct =>
            {
                for (int i = 0; i < count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var loop = UniTaskChainPool.Spawn(_owner);
                    loop._breakState = _breakState; // 🔥 chia sẻ
                    body(loop);
                    await loop.RunInternal();
                }
            });
            return this;
        }

        public UniTaskFlow While(
            Func<bool> condition,
            Action<UniTaskFlow> body)
        {
            _queue.Enqueue(async ct =>
            {
                while (condition())
                {
                    ct.ThrowIfCancellationRequested();

                    var loop = UniTaskChainPool.Spawn(_owner);
                    body(loop);
                    await loop.RunInternal();
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
            });

            return this;
        }

        public UniTaskFlow Timeout(float seconds)
        {
            _queue.Enqueue(async ct =>
            {
                using var timeout =
                    CancellationTokenSource.CreateLinkedTokenSource(ct);
                await UniTask.WhenAny(
                    UniTask.Delay(TimeSpan.FromSeconds(seconds), ignoreTimeScale: _ignoreTimeScale, cancellationToken: timeout.Token),
                    UniTask.WaitUntilCanceled(timeout.Token)
                );

                timeout.Cancel();
            });
            return this;
        }

        // ================= RUN =================

        public void Run(bool isIgnoreTimeScale = false)
        {
            _ignoreTimeScale = isIgnoreTimeScale;
            RunInternal().Forget();
        }

        private async UniTask RunInternal()
        {
            try
            {
                while (_queue.Count > 0)
                {
                    _token.ThrowIfCancellationRequested();

                    if (_breakState.BreakChain)
                        break;

                    var step = _queue.Dequeue();
                    await step(_token);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                UniTaskChainPool.Despawn(this);
            }
        }

        
        public UniTaskFlow StopChain()
        {
            _queue.Enqueue(ct =>
            {
                _breakState.BreakChain = true;
                return UniTask.CompletedTask;
            });
            return this;
        }
        
        
        public UniTaskFlow StopChainIf(Func<bool> condition, bool stopImmediately = false)
        {
            _queue.Enqueue(ct =>
            {
                if (!condition())
                    return UniTask.CompletedTask;
                if (stopImmediately)
                    _breakState.BreakChain = true;

                return UniTask.CompletedTask;
            });
            return this;
        }
    }
}