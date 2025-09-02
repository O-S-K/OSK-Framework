using System;
using System.Collections.Generic;

#if CYSHARP_UNITASK
using Cysharp.Threading.Tasks;
#endif


namespace OSK
{
    public class EventBusManager : GameFrameworkComponent
    {
        // Key: Event Type, Value: List of Subscribers
        private readonly Dictionary<Type, List<Delegate>> syncSubscribers = new();
        // Key: Event Type, Value: List of Async Subscribers
        private readonly Dictionary<Type, List<Delegate>> asyncSubscribers = new();
        // Key: Event Type, Value: Last Event
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

#if CYSHARP_UNITASK
        public void UnsubscribeAsync<T>(Func<T, UniTask> callback) where T : GameEvent
        {
            Type eventType = typeof(T);
            if (asyncSubscribers.ContainsKey(eventType))
            {
                asyncSubscribers[eventType].Remove(callback);
            }
        }
#endif
        #endregion

        #region Publish

        public void Publish<T>(T gameEvent) where T : GameEvent
        {
            Type eventType = typeof(T);

            // Cache last event 
            lastEvents[eventType] = gameEvent;

            // send to sync subscribers
            if (syncSubscribers.ContainsKey(eventType))
            {
                foreach (var subscriber in syncSubscribers[eventType])
                {
                    (subscriber as Action<T>)?.Invoke(gameEvent);
                }
            }

#if CYSHARP_UNITASK
            // send to async subscribers (fire and forget)
            if (asyncSubscribers.ContainsKey(eventType))
            {
                foreach (var subscriber in asyncSubscribers[eventType])
                {
                    _ = (subscriber as Func<T, UniTask>)?.Invoke(gameEvent);
                }
            }
#endif
        }


#if CYSHARP_UNITASK
        public async UniTask PublishAsync<T>(T gameEvent) where T : GameEvent
        {
            Type eventType = typeof(T);

            // Cache last event 
            lastEvents[eventType] = gameEvent;

            // send to sync subscribers
            if (syncSubscribers.ContainsKey(eventType))
            {
                foreach (var subscriber in syncSubscribers[eventType])
                {
                    (subscriber as Action<T>)?.Invoke(gameEvent);
                }
            }

            // send to async subscribers
            if (asyncSubscribers.ContainsKey(eventType))
            {
                foreach (var subscriber in asyncSubscribers[eventType])
                {
                    if (subscriber is Func<T, UniTask> asyncHandler)
                    {
                        await asyncHandler.Invoke(gameEvent);
                    }
                }
            }
        }
#endif
        #endregion
    }
}