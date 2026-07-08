using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;


namespace OSK
{
    public class EventBusManager : GameFrameworkComponent
    {
        private readonly Dictionary<Type, List<Delegate>> syncSubscribers = new();
        private readonly Dictionary<Type, List<Delegate>> asyncSubscribers = new();
        private readonly Dictionary<Type, GameEvent> lastEvents = new();

        public override void OnInit() { }

        #region Subscribe

        public void Subscribe<T>(Action<T> callback, bool receiveLastIfExists = false) where T : GameEvent
        {
            Type eventType = typeof(T);

            if (!syncSubscribers.ContainsKey(eventType))
                syncSubscribers[eventType] = new List<Delegate>();

            syncSubscribers[eventType].Add(callback);

            // if have last event, invoke callback immediately
            if (receiveLastIfExists && lastEvents.TryGetValue(eventType, out var lastEvent))
            {
                callback?.Invoke((T)lastEvent);
            }
        }

        public void SubscribeAsync<T>(Func<T, UniTask> callback, bool receiveLastIfExists = false) where T : GameEvent
        {
            Type eventType = typeof(T);

            if (!asyncSubscribers.ContainsKey(eventType))
                asyncSubscribers[eventType] = new List<Delegate>();

            asyncSubscribers[eventType].Add(callback);

            // if have last event, invoke callback immediately
            if (receiveLastIfExists && lastEvents.TryGetValue(eventType, out var lastEvent))
            {
                _ = callback.Invoke((T)lastEvent);
            }
        }
        #endregion

        #region Unsubscribe

        public void Unsubscribe<T>(Action<T> callback) where T : GameEvent
        {
            Type eventType = typeof(T);
            if (syncSubscribers.ContainsKey(eventType))
            {
                syncSubscribers[eventType].Remove(callback);
            }
        }

        public void UnsubscribeAsync<T>(Func<T, UniTask> callback) where T : GameEvent
        {
            Type eventType = typeof(T);
            if (asyncSubscribers.ContainsKey(eventType))
            {
                asyncSubscribers[eventType].Remove(callback);
            }
        }

        public void ClearLast<T>() where T : GameEvent
        {
            lastEvents.Remove(typeof(T));
        }

        public void ClearAllLast()
        {
            lastEvents.Clear();
        }
        #endregion

        #region Publish

        public void Publish<T>(T gameEvent) where T : GameEvent
        {
            Type eventType = typeof(T);

            // Cache last event 
            lastEvents[eventType] = gameEvent;

            // send to sync subscribers (snapshot to avoid collection modified)
            if (syncSubscribers.TryGetValue(eventType, out var syncList))
            {
                var snapshot = syncList.ToArray();
                foreach (var subscriber in snapshot)
                {
                    (subscriber as Action<T>)?.Invoke(gameEvent);
                }
            }

            // send to async subscribers (fire and forget with error handling)
            if (asyncSubscribers.TryGetValue(eventType, out var asyncList))
            {
                var snapshot = asyncList.ToArray();
                foreach (var subscriber in snapshot)
                {
                    var handler = subscriber as Func<T, UniTask>;
                    if (handler != null)
                        SafeInvokeAsync(handler, gameEvent).Forget();
                }
            }
        }

        private async UniTask SafeInvokeAsync<T>(Func<T, UniTask> handler, T gameEvent) where T : GameEvent
        {
            try
            {
                await handler.Invoke(gameEvent);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"[EventBus] Async subscriber error for {typeof(T).Name}: {ex.Message}");
            }
        }


        public async UniTask PublishAsync<T>(T gameEvent) where T : GameEvent
        {
            Type eventType = typeof(T);

            // Cache last event 
            lastEvents[eventType] = gameEvent;

            // send to sync subscribers (snapshot to avoid collection modified)
            if (syncSubscribers.TryGetValue(eventType, out var syncList))
            {
                var snapshot = syncList.ToArray();
                foreach (var subscriber in snapshot)
                {
                    (subscriber as Action<T>)?.Invoke(gameEvent);
                }
            }

            // send to async subscribers (snapshot to avoid collection modified)
            if (asyncSubscribers.TryGetValue(eventType, out var asyncList))
            {
                var snapshot = asyncList.ToArray();
                foreach (var subscriber in snapshot)
                {
                    if (subscriber is Func<T, UniTask> asyncHandler)
                    {
                        await asyncHandler.Invoke(gameEvent);
                    }
                }
            }
        }
        #endregion
    }
}
