using System;
using UnityEngine;
using System.Collections;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Reflection;

namespace OSK
{
    public class MonoManager : GameFrameworkComponent
    {
        [ReadOnly, ShowInInspector] private readonly List<Action> _toMainThreads = new();
        [ReadOnly, ShowInInspector] private List<Action> _localToMainThreads = new();
        private volatile bool _isToMainThreadQueueEmpty = true;

        // Tick processes
        [ShowInInspector] private readonly List<IUpdate> tickProcesses = new(1024);
        [ShowInInspector] private readonly List<IFixedUpdate> fixedTickProcesses = new(512);
        [ShowInInspector] private readonly List<ILateUpdate> lateTickProcesses = new(256);

        // Lifecycle processes
        [ShowInInspector] private readonly List<IAwake> awakeProcesses = new(128);
        [ShowInInspector] private readonly List<IOnEnable> enableProcesses = new(128);
        [ShowInInspector] private readonly List<IOnDisable> disableProcesses = new(128);
        [ShowInInspector] private readonly List<IStart> startProcesses = new(128);
        [ShowInInspector] private readonly List<IDestroy> destroyProcesses = new(128);

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

        public override void Awake()
        {
            foreach (var a in awakeProcesses) a?.OnAwake();
        }

        protected void OnEnable()
        {
            foreach (var e in enableProcesses) e?.OnEnable();
        }

        protected void Start()
        {
            foreach (var s in startProcesses) s?.OnStart();
        }

        protected void OnDisable()
        {
            foreach (var d in disableProcesses) d?.OnDisable();
        }

        public override void OnDestroy()
        {
            foreach (var d in destroyProcesses) d?.OnDestroy();
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
            if (obj is IUpdate tick) tickProcesses.Add(tick);
            if (obj is IFixedUpdate fixedTick) fixedTickProcesses.Add(fixedTick);
            if (obj is ILateUpdate lateTick) lateTickProcesses.Add(lateTick);
            if (obj is IAwake awake) awakeProcesses.Add(awake);
            if (obj is IOnEnable en) enableProcesses.Add(en);
            if (obj is IOnDisable dis) disableProcesses.Add(dis);
            if (obj is IStart st) startProcesses.Add(st);
            if (obj is IDestroy de) destroyProcesses.Add(de);
        }

        public void UnRegister(object obj)
        {
            if (obj is IUpdate tick) tickProcesses.Remove(tick);
            if (obj is IFixedUpdate fixedTick) fixedTickProcesses.Remove(fixedTick);
            if (obj is ILateUpdate lateTick) lateTickProcesses.Remove(lateTick);
            if (obj is IAwake awake) awakeProcesses.Remove(awake);
            if (obj is IOnEnable en) enableProcesses.Remove(en);
            if (obj is IOnDisable dis) disableProcesses.Remove(dis);
            if (obj is IStart st) startProcesses.Remove(st);
            if (obj is IDestroy de) destroyProcesses.Remove(de);
        }

        public void RemoveAllTickProcess()
        {
            tickProcesses?.Clear();
            fixedTickProcesses?.Clear();
            lateTickProcesses?.Clear();
            awakeProcesses?.Clear();
            enableProcesses?.Clear();
            disableProcesses?.Clear();
            startProcesses?.Clear();
            destroyProcesses?.Clear();
        }

        #endregion

        #region Update Handle

        private void Update()
        {
            if (IsPause || SpeedGame == 0) return;

            float deltaTime = Time.deltaTime * SpeedGame;

            foreach (var t in tickProcesses)
                t?.Tick(deltaTime);

            if (_isToMainThreadQueueEmpty) return;
            _localToMainThreads.Clear();
            lock (_toMainThreads)
            {
                _localToMainThreads.AddRange(_toMainThreads);
                _toMainThreads.Clear();
                _isToMainThreadQueueEmpty = true;
            }

            for (var i = _localToMainThreads.Count - 1; i >= 0; i--)
            {
                _localToMainThreads[i]?.Invoke();
            }
        }

        private void FixedUpdate()
        {
            if (IsPause || SpeedGame == 0) return;

            float fixedDeltaTime = Time.fixedDeltaTime * SpeedGame;
            foreach (var t in fixedTickProcesses)
                t?.FixedTick(fixedDeltaTime);
        }

        private void LateUpdate()
        {
            if (IsPause || SpeedGame == 0) return;

            float deltaTime = Time.deltaTime * SpeedGame;
            foreach (var t in lateTickProcesses)
                t?.LateTick(deltaTime);
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