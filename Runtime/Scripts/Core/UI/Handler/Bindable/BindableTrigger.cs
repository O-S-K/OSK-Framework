using System;

namespace OSK
{
    /// <summary>
    /// Giống như BindableProperty nhưng không lưu trữ trạng thái (State).
    /// Dùng cho các sự kiện xảy ra 1 lần (Signals) như: PlaySound, SpawnParticle, ShowError...
    /// Giúp View nhận biết được các hành động từ Model mà không cần biến boolean bật tắt.
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
    /// BindableTrigger có truyền kèm theo dữ liệu (Payload)
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
