using UnityEngine;
using OSK;

namespace OSKDemo
{
    [AddComponentMenu("OSK/Debug/Visual Binding Demo")]
    public class VisualBindingDemo : MonoBehaviour
    {
        [Header("Bindable Properties")]
        public BindableProperty<string> statusText = new BindableProperty<string>("Sẵn sàng!");
        public BindableProperty<int> counterValue = new BindableProperty<int>(10);
        public BindableProperty<float> sliderProgress = new BindableProperty<float>(0.75f);
        public BindableProperty<bool> isToggled = new BindableProperty<bool>(true);
        public BindableProperty<Color> colorValue = new BindableProperty<Color>(Color.green);
        public BindableProperty<bool> activeState = new BindableProperty<bool>(true);

        [Header("Simulation Settings")]
        [Tooltip("Tự động tăng bộ đếm mỗi giây để test")]
        public bool autoIncrement = false;
        private float _timer = 0f;

        private void Update()
        {
            if (autoIncrement)
            {
                _timer += Time.deltaTime;
                if (_timer >= 1f)
                {
                    _timer = 0f;
                    counterValue.Value += 1;
                    statusText.Value = $"Cập nhật lúc: {Time.time:F1}s";
                    colorValue.Value = Color.Lerp(Color.green, Color.red, Mathf.PingPong(Time.time, 1f));
                    activeState.Value = !activeState.Value; 
                }
            }
        }

        // Các hàm helper để Test gọi từ Button UI
        public void IncrementCounter()
        {
            counterValue.Value += 1;
            statusText.Value = "Tăng bộ đếm thủ công!";
        }

        public void ResetAll()
        {
            counterValue.Value = 0;
            sliderProgress.Value = 0.5f;
            isToggled.Value = true;
            statusText.Value = "Đã Reset!";
            colorValue.Value = Color.white;
        }
    }
}
