using System;

namespace OSK
{
    [Serializable]
    public class ObservableData<T>
    {
        public event Action<object> OnValueChanged;
        public T _value;
   
        public T Value
        {
            get => _value;
            set
            {
                if (!Equals(_value, value))
                {
                    _value = value;
                    OnValueChanged?.Invoke(_value); // gửi object luôn
                }
            }
        }
        
        public ObservableData(T initialValue = default)
        {
            _value = initialValue;
        }
        
        public void SetValueWithoutNotify(T value)
        {
            _value = value;
        }
        
        public override string ToString()
        {
            return _value != null ? _value.ToString() : "null";
        }
        
        public static implicit operator T(ObservableData<T> observable)
        {
            return observable._value;
        }
    }
}