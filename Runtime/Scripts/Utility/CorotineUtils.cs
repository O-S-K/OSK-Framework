using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using System;

#if CYSHARP_UNITASK
using Cysharp.Threading.Tasks;
#endif

namespace OSK
{
    public static class CoroutineExtension
    {
        // Run Coroutine
        public static LazyCoroutine Run(this IEnumerator enumerator)
        {
            var handler = new LazyCoroutine(enumerator);
            handler.Start();
            return handler;
        }

        // Run Delayed Coroutine
        public static LazyCoroutine RunDelay(this IEnumerator enumerator, float delay)
        {
            var handler = new LazyCoroutine(DelayedCoroutine(enumerator, delay));
            handler.Start();
            return handler;
        }
        
        // Run Until Coroutine
        public static LazyCoroutine RunUtil(this IEnumerator enumerator, Func<bool> condition)
        { 
            var handler = new LazyCoroutine(UntilCoroutine(enumerator, condition));
            handler.Start();
            return handler;
        }
        
        // Run Until Coroutine
        private static IEnumerator UntilCoroutine(IEnumerator enumerator, Func<bool> condition)
        {
            while (!condition())
            {
                yield return null;
            }
            yield return enumerator;
        }
        private static IEnumerator DelayedCoroutine(IEnumerator enumerator, float delay)
        {
            yield return new WaitForSeconds(delay);
            yield return enumerator;
        } 
    }

    public class LazyCoroutine
    {
        private IEnumerator _coroutine;
        private bool _paused;
        private bool _running;
        private bool _stopped;

        // Event for Completion
        private readonly UnityEvent<bool> _completed = new UnityEvent<bool>();

        public LazyCoroutine(IEnumerator coroutine)
        {
            _coroutine = coroutine ?? throw new ArgumentNullException(nameof(coroutine));
        }

        // Control Methods
        public void Start()
        {
            if (!_running && _coroutine != null)
            {
                _running = true;
                CoroutineDriver.Run(CallWrapper());
            }
        }
        public void Stop()
        {
            _stopped = true;
            _running = false;
        }
        public void Pause() => _paused = true;
        public void Resume() => _paused = false;
        public void OnComplete(UnityAction<bool> action) => _completed.AddListener(action);

        // Cleanup Method
        private void Finish()
        {
            _completed.Invoke(_stopped);
            _completed.RemoveAllListeners();
            _coroutine = null;
        }

        // Internal Coroutine Wrapper
        private IEnumerator CallWrapper()
        {
            yield return null;

            while (_running)
            {
                if (_paused)
                {
                    yield return null;
                }
                else if (_coroutine?.MoveNext() == true)
                {
                    yield return _coroutine.Current;
                }
                else
                {
                    _running = false;
                }
            }

            Finish();
        }

        internal class CoroutineDriver : MonoBehaviour
        {
            private static CoroutineDriver instance;

            public static Coroutine Run(IEnumerator target) => Instance.StartCoroutine(target);

            private static CoroutineDriver Instance
            {
                get
                {
                    if (instance == null)
                    {
                        var go = new GameObject("[CoroutineDriver]");
                        instance = go.AddComponent<CoroutineDriver>();
                        DontDestroyOnLoad(go);
                    }

                    return instance;
                }
            }

            private void Awake()
            {
                if (instance != null && instance != this)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}