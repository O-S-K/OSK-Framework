using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
using Sirenix.OdinInspector;

namespace OSK
{
    public class DebugView : View
    {
        [Title("Debug Components")]
        [SerializeField] private Text _infoText;
        [SerializeField] private RectTransform _buttonContainer;
        [SerializeField] private GameObject _buttonPrefab;
        [SerializeField] private Button _closeButton;
        private float _lastUpdateTime;

        protected override void OnInit()
        {
            viewType = EViewType.Overlay;
            _closeButton.BindButton(Hide);
        }

        protected override void SetData(object data = null)
        {
            base.SetData(data);
            RefreshButtons();
        }

        /*private void Update()
        {
            if (Time.time - _lastUpdateTime > 0.5f)
            {
                _lastUpdateTime = Time.time;
                UpdateSystemInfo();
            }
        }*/

        private void UpdateSystemInfo()
        {
            if (_infoText == null) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"FPS: {Mathf.Round(1f / Time.unscaledDeltaTime)}");
            sb.AppendLine($"RAM: {System.GC.GetTotalMemory(false) / 1024 / 1024} MB");
            sb.AppendLine($"Device Model: {SystemInfo.deviceModel}");
            sb.AppendLine($"Device Type: {SystemInfo.deviceType}");
            sb.AppendLine($"Graphics Device: {SystemInfo.graphicsDeviceName}");
            sb.AppendLine($"System Memory: {SystemInfo.systemMemorySize} MB");
            _infoText.text = sb.ToString();
        }

        private void RefreshButtons()
        {
            if (_buttonContainer == null || _buttonPrefab == null) return;

            // Clear old buttons
            foreach (Transform child in _buttonContainer)
            {
                Destroy(child.gameObject);
            }

            // Add Default Cheats
            AddCheat("Show Fps", UpdateSystemInfo);
            AddCheat("Clear Text", () => _infoText.text = "");
        }

        private void AddCheat(string label, System.Action action)
        {
            var go = Instantiate(_buttonPrefab, _buttonContainer);
            go.name = $"Cheat_{label}";
            var btn = go.GetComponent<Button>();
            var txt = go.GetComponentInChildren<Text>();
            
            if (txt != null) txt.text = label;
            if (btn != null) btn.BindButton(() => action?.Invoke());
        } 
    }
}
