using System;

namespace OSK
{
    /// <summary>
    /// Similar to BindableProperty but does not store state.
    /// Used for one-time events (Signals) such as: PlaySound, SpawnParticle, ShowError...
    /// Helps the View recognize actions from the Model without needing a toggle boolean variable.
    /// </summary>
    public class BindableTrigger
    {
        private Action _onTrigger;

        public void Bind(Action action)
        {
            _onTrigger += action;
        }

        public void Unbind(Action action)
        {
            _onTrigger -= action;
        }

        public void UnbindAll()
        {
            _onTrigger = null;
        }

        public void Trigger()
        {
            _onTrigger?.Invoke();
        }
    }

    /// <summary>
    /// triggers transmit data (payload) along with the trigger.
    /// </summary>
    public class BindableTrigger<T>
    {
        private Action<T> _onTrigger;

        public void Bind(Action<T> action)
        {
            _onTrigger += action;
        }

        public void Unbind(Action<T> action)
        {
            _onTrigger -= action;
        }

        public void UnbindAll()
        {
            _onTrigger = null;
        }

        public void Trigger(T payload)
        {
            _onTrigger?.Invoke(payload);
        }
    }
}
