using System;
using UnityEngine;
using System.Collections;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Reflection;

namespace OSK
{
    public class MonoManager : GameFrameworkComponent, IUpdateable, IFixedUpdateable, ILateUpdateable
    {
        [ReadOnly, ShowInInspector] private readonly List<Action> _toMainThreads = new();
        [ReadOnly, ShowInInspector] private List<Action> _localToMainThreads = new();
        private volatile bool _isToMainThreadQueueEmpty = true;

        // Tick processes
        [ShowInInspector] private readonly List<IUpdate> tickProcesses = new(1024);
        [ShowInInspector] private readonly List<IFixedUpdate> fixedTickProcesses = new(512);
        [ShowInInspector] private readonly List<ILateUpdate> lateTickProcesses = new(256);
        
        private static readonly List<IUpdate> _tempTickList = new();
        private static readonly List<IFixedUpdate> _tempFixedList = new();
        private static readonly List<ILateUpdate> _tempLateList = new();


        [ShowInInspector] public bool IsPause { get; private set; } = false;
        [ShowInInspector] public float TimeScale { get; private set; } = 1f;
        [ShowInInspector] public float SpeedGame { get; private set; } = 1f;
        
        
        internal event Action<bool> OnGamePause = null;
        internal event Action OnGameQuit = null;

        #region Init

        public override void OnInit()
        {
            IsPause = false;
            TimeScale = 1f;
            AutoRegisterAll();
            
        }
        #endregion

        #region Config

        public MonoManager SetSpeed(float speed = 1f)
        {
            this.SpeedGame = speed;
            return this;
        }

        public MonoManager SetTimeScale(float timeScale)
        {
            TimeScale = timeScale;
            Time.timeScale = TimeScale;
            return this;
        }

        public MonoManager SetPause(bool isPause)
        {
            IsPause = isPause;
            return this;
        }

        #endregion

        #region Register / Unregister

        private void AutoRegisterAll()
        {
            foreach (var obj in FindObjectsOfType<MonoBehaviour>())
            {
                if (obj?.GetType().GetCustomAttribute<AutoRegisterUpdateAttribute>() == null)
                    continue;
                Register(obj);
            }
        }

        public void Register(object obj)
        {
            if (obj is IUpdate tick) if (!tickProcesses.Contains(tick)) tickProcesses.Add(tick);
            if (obj is IFixedUpdate fixedTick) if (!fixedTickProcesses.Contains(fixedTick)) fixedTickProcesses.Add(fixedTick);
            if (obj is ILateUpdate lateTick) if (!lateTickProcesses.Contains(lateTick)) lateTickProcesses.Add(lateTick);
        }

        public void UnRegister(object obj)
        {
            if (obj is IUpdate tick) tickProcesses.Remove(tick);
            if (obj is IFixedUpdate fixedTick) fixedTickProcesses.Remove(fixedTick);
            if (obj is ILateUpdate lateTick) lateTickProcesses.Remove(lateTick);
        }

        public void RemoveAllTickProcess()
        {
            tickProcesses?.Clear();
            fixedTickProcesses?.Clear();
            lateTickProcesses?.Clear();
        }

        #endregion

        #region Update Handle (Integrated with Main)

        public void OnUpdate()
        {
            if (IsPause || SpeedGame == 0) return;

            float deltaTime = Time.deltaTime * SpeedGame;

            // Copy snapshot to avoid collection modified during iteration
            _tempTickList.Clear();
            _tempTickList.AddRange(tickProcesses);

            for (int i = 0; i < _tempTickList.Count; i++)
            {
                var t = _tempTickList[i];
                // One failing tick must not skip the rest of the frame (game time, input, the main-thread queue).
                try { if (t != null) t.Tick(deltaTime); }
                catch (Exception e) { Debug.LogException(e); }
            }

            // Execute Main thread queue
            if (!_isToMainThreadQueueEmpty)
            {
                _localToMainThreads.Clear();
                lock (_toMainThreads)
                {
                    _localToMainThreads.AddRange(_toMainThreads);
                    _toMainThreads.Clear();
                    _isToMainThreadQueueEmpty = true;
                }

                for (int i = 0; i < _localToMainThreads.Count; i++)
                {
                    try
                    {
                        _localToMainThreads[i]?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        MyLogger.LogError($"[MonoManager] Error in MainThread action: {ex.Message}\n{ex.StackTrace}");
                    }
                }
            }
        }

        public void OnFixedUpdate()
        {
            if (IsPause || SpeedGame == 0) return;

            float fixedDeltaTime = Time.fixedDeltaTime * SpeedGame;

            _tempFixedList.Clear();
            _tempFixedList.AddRange(fixedTickProcesses);

            for (int i = 0; i < _tempFixedList.Count; i++)
            {
                var t = _tempFixedList[i];
                try { if (t != null) t.FixedTick(fixedDeltaTime); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        public void OnLateUpdate()
        {
            if (IsPause || SpeedGame == 0) return;

            float deltaTime = Time.deltaTime * SpeedGame;

            _tempLateList.Clear();
            _tempLateList.AddRange(lateTickProcesses);

            for (int i = 0; i < _tempLateList.Count; i++)
            {
                var t = _tempLateList[i];
                try { if (t != null) t.LateTick(deltaTime); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        #endregion

        #region App Handle

        private void OnApplicationFocus(bool hasFocus)
        {
            OnGamePause?.Invoke(hasFocus);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            OnGamePause?.Invoke(pauseStatus);
        }

        private void OnApplicationQuit()
        {
            OnGameQuit?.Invoke();
        }

        #endregion

        #region Effective (Coroutine + MainThread)

        public Coroutine StartCoroutineImpl(IEnumerator routine)
        {
            return routine != null ? StartCoroutine(routine) : null;
        }

        public Coroutine StartCoroutineImpl(string methodName, object value)
        {
            return !string.IsNullOrEmpty(methodName) ? StartCoroutine(methodName, value) : null;
        }

        public Coroutine StartCoroutineImpl(string methodName)
        {
            return !string.IsNullOrEmpty(methodName) ? StartCoroutine(methodName) : null;
        }

        public void StopCoroutineImpl(IEnumerator routine)
        {
            if (routine != null) StopCoroutine(routine);
        }

        public void StopCoroutineImpl(Coroutine routine)
        {
            if (routine != null) StopCoroutine(routine);
        }

        public void StopCoroutineImpl(string methodName)
        {
            if (!string.IsNullOrEmpty(methodName))
            {
                StopCoroutine(methodName);
            }
        }

        public void StopAllCoroutinesImpl()
        {
            StopAllCoroutines();
        }

        public void RunOnMainThreadImpl(Action action)
        {
            if (action == null) return;
            lock (_toMainThreads)
            {
                _toMainThreads.Add(action);
                _isToMainThreadQueueEmpty = false;
            }
        }

        public Action ToMainThreadImpl(Action action)
        {
            if (action == null) return delegate { };
            return () => RunOnMainThreadImpl(action);
        }

        public Action<T> ToMainThreadImpl<T>(Action<T> action)
        {
            if (action == null) return delegate { };
            return (arg) => RunOnMainThreadImpl(() => action(arg));
        }

        #endregion
    }
}