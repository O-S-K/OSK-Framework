using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace OSK
{
    public class DebugConsoleView : View
    {
        [Title("Debug Components")]
        [SerializeField] private bool _autoBuildIfMissing = true;
        [SerializeField] private int _maxLogCount = 80;
        [SerializeField] private float _refreshInterval = 0.35f;
        [SerializeField] private Text _infoText;
        [SerializeField] private Text _consoleText;
        [SerializeField] private Text _summaryText;
        [SerializeField] private RectTransform _consoleContent;
        [SerializeField] private RectTransform _buttonContainer;
        [SerializeField] private GameObject _buttonPrefab;
        [SerializeField] private Button _closeButton;
        [SerializeField] private RectTransform _panelRoot;

        private DebugConsoleManager _manager;
        private float _nextRefreshTime;
        private readonly float[] _fpsSamples = new float[24];
        private RawImage _fpsSparkline;
        private Slider _timeScaleSlider;
        private Toggle _logToggle;
        private Toggle _warningToggle;
        private Toggle _errorToggle;

        // Sets the view type and builds the runtime inspector if no prefab references exist.
        protected override void OnInit()
        {
            viewType = EViewType.Overlay;
            if (_manager == null)
            {
                _manager = new DebugConsoleManager(_maxLogCount);
            }
            else
            {
                _manager.SetMaxLogCount(_maxLogCount);
            }

            if (_autoBuildIfMissing && (_infoText == null || (_consoleText == null && _consoleContent == null) || _buttonContainer == null))
            {
                BuildInspector();
            }

            _closeButton.BindButton(Hide);
            RefreshButtons();
            RefreshAll();
        }

        // Subscribes to Unity logs when the view is enabled.
        private void OnEnable()
        {
            Application.logMessageReceived -= OnLogReceived;
            Application.logMessageReceived += OnLogReceived;
        }

        // Unsubscribes from Unity logs when the view is disabled.
        private void OnDisable()
        {
            Application.logMessageReceived -= OnLogReceived;
        }

        // Handles hotkey and throttled text refresh.
        private void Update()
        {
            OnInspectorTick();

            _manager.Tick(Time.unscaledDeltaTime);
            if (Time.unscaledTime < _nextRefreshTime)
            {
                return;
            }

            _nextRefreshTime = Time.unscaledTime + _refreshInterval;
            RefreshSystemInfo();
            if (_manager.ConsoleDirty)
            {
                RefreshConsoleText();
            }
        }

        // Refreshes buttons when new data is passed through Main.UI.Open.
        protected override void SetData(object data = null)
        {
            base.SetData(data);
            RefreshButtons();
            RefreshAll();
        }

        // Builds an inspector-like debug panel using builder pattern.
        protected virtual void BuildInspector()
        {
            StretchRootToParent();
            SeedRuntimeSamples();
            DebugInspectorBuilder builder = DebugInspectorBuilder.Create(transform)
                .WithPanel(new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(360f, -16f), new Vector2(-8f, 0f))
                .WithHeader("Inspector", out _closeButton)
                .WithScroll(out RectTransform content);
            _panelRoot = builder.Panel;

            builder.Folder(content, "System")
                .Label(out _summaryText, 13, FontStyle.Bold, 24f)
                .Label(out _infoText, 12, FontStyle.Normal, 96f);

            builder.Folder(content, "Runtime")
                //.Sparkline("FPS", _fpsSamples, DebugInspectorBuilder.AccentColor, out _fpsSparkline)
                .SliderField("Time Scale", Mathf.Clamp(Time.timeScale, 0f, 3f), 0f, 3f, value =>
                {
                    Time.timeScale = value;
                }, out _timeScaleSlider);

            builder.Folder(content, "Console")
                .Toggle("Log", true, value => { _manager.SetShowLog(value); RefreshConsoleText(); }, out _logToggle)
                .Toggle("Warning", true, value => { _manager.SetShowWarning(value); RefreshConsoleText(); }, out _warningToggle)
                .Toggle("Error", true, value => { _manager.SetShowError(value); RefreshConsoleText(); }, out _errorToggle)
                .Button("Clear", ClearLogs)
                .Button("Copy", _manager.CopyLogs)
                .ConsoleLog(out _consoleContent, 132f);

            _buttonContainer = builder.Folder(content, "Actions").Content;
            BuildCustomSections(builder, content);
        }

        // Makes sure the generated inspector can anchor to the real screen area.
        private void StretchRootToParent()
        {
            RectTransform rectTransform = transform as RectTransform;
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.localScale = Vector3.one;
        }

        // Refreshes all dynamic text.
        protected void RefreshAll()
        {
            RefreshSystemInfo();
            RefreshConsoleText();
            OnInspectorRefresh();
        }

        // Adds custom sections after the default System, Framework, Console, and Actions sections.
        protected virtual void BuildCustomSections(DebugInspectorBuilder builder, RectTransform content) { }

        // Ticks custom widgets once per frame.
        protected virtual void OnInspectorTick() { }

        // Refreshes custom widgets together with the default inspector refresh loop.
        protected virtual void OnInspectorRefresh() { }

        // Parses and runs a command from a command input.
        protected bool RunDebugCommand(string command, Dictionary<string, Action<string>> handlers, string[] usages, string logPrefix = "Debug")
        {
            string trimmed = command != null ? command.Trim() : string.Empty;
            if (trimmed.Length == 0)
            {
                return false;
            }

            int spaceIndex = trimmed.IndexOf(' ');
            string name = spaceIndex < 0 ? trimmed : trimmed.Substring(0, spaceIndex);
            string args = spaceIndex < 0 ? string.Empty : trimmed.Substring(spaceIndex + 1).Trim();
            name = name.ToLowerInvariant();

            Action<string> handler;
            if (handlers == null || !handlers.TryGetValue(name, out handler))
            {
                Debug.LogWarning("[" + logPrefix + "] Unknown command: " + name + ". Try: " + JoinCommandUsages(usages));
                return false;
            }

            handler(args);
            return true;
        }

        // Prints the expected syntax for a command.
        protected static void WarnCommandUsage(string logPrefix, string usage)
        {
            Debug.LogWarning("[" + logPrefix + "] Usage: " + usage);
        }

        private static string JoinCommandUsages(string[] usages)
        {
            if (usages == null || usages.Length == 0)
            {
                return "no commands registered";
            }

            return string.Join(" | ", usages);
        }

        // Refreshes top system information.
        private void RefreshSystemInfo()
        {
            if (_manager == null || _infoText == null)
            {
                return;
            }

            if (_summaryText != null)
            {
                _summaryText.text = _manager.BuildSummaryText();
            }

            _infoText.text = _manager.BuildSystemInfoText();
            RefreshRuntimeControls();
        }

        // Keeps runtime controls in sync when another script or command changes the game state.
        private void RefreshRuntimeControls()
        {
            RecordFpsSample();
            DebugInspectorBuilder.UpdateSparkline(_fpsSparkline, _fpsSamples, DebugInspectorBuilder.AccentColor);

            if (_timeScaleSlider == null)
            {
                return;
            }

            float timeScale = Mathf.Clamp(Time.timeScale, _timeScaleSlider.minValue, _timeScaleSlider.maxValue);
            if (Mathf.Abs(_timeScaleSlider.value - timeScale) > 0.001f)
            {
                _timeScaleSlider.value = timeScale;
            }
        }

        // Fills runtime graphs with a stable starting value.
        private void SeedRuntimeSamples()
        {
            for (int i = 0; i < _fpsSamples.Length; i++)
            {
                _fpsSamples[i] = 60f;
            }
        }

        // Pushes latest unscaled FPS into the runtime FPS graph.
        private void RecordFpsSample()
        {
            for (int i = 1; i < _fpsSamples.Length; i++)
            {
                _fpsSamples[i - 1] = _fpsSamples[i];
            }

            float fps = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            _fpsSamples[^1] = Mathf.Clamp(fps, 1f, 240f);
        }

        // Refreshes the console log block.
        private void RefreshConsoleText()
        {
            if (_manager == null || (_consoleText == null && _consoleContent == null))
            {
                return;
            }

            if (_consoleContent != null)
            {
                for (int i = _consoleContent.childCount - 1; i >= 0; i--)
                {
                    Destroy(_consoleContent.GetChild(i).gameObject);
                }
            }

            var visibleLogs = _manager.GetVisibleLogs();
            for (int i = 0; i < visibleLogs.Count; i++)
            {
                AddConsoleRow(visibleLogs[i]);
            }

            if (visibleLogs.Count == 0)
            {
                AddConsoleRow(new DebugConsoleManager.LogEntry("No logs.", string.Empty, LogType.Log));
            }

            if (_consoleText != null)
            {
                _consoleText.text = _manager.BuildConsoleText();
            }

            _manager.MarkConsoleClean();
        }

        // Adds one colored log row to the generated console scroll view.
        private void AddConsoleRow(DebugConsoleManager.LogEntry entry)
        {
            if (_consoleContent == null)
            {
                return;
            }

            Text text = DebugInspectorBuilder.CreateText("Log_" + entry.Type, _consoleContent, DebugConsoleManager.FormatLog(entry), 10, FontStyle.Normal, DebugConsoleManager.GetLogColor(entry.Type));
            text.alignment = TextAnchor.UpperLeft;
            text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            float height = DebugConsoleManager.GetLogRowHeight(entry.Message);
            LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.preferredWidth = 292f;
            text.rectTransform.sizeDelta = new Vector2(292f, height);
        }

        // Rebuilds custom action buttons for legacy prefab support.
        protected virtual void RefreshButtons()
        {
            if (_buttonContainer == null)
            {
                return;
            }

            for (int i = _buttonContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = _buttonContainer.GetChild(i);
                if (child != null && child.name.StartsWith("Action_"))
                {
                    Destroy(child.gameObject);
                }
            }

            AddAction("Refresh", RefreshAll);
            AddAction("Clear Logs", ClearLogs);
            AddAction("Copy Logs", _manager.CopyLogs);
        }

        // Adds a debug action button.
        private void AddAction(string label, Action action)
        {
            if (_buttonPrefab != null)
            {
                GameObject go = Instantiate(_buttonPrefab, _buttonContainer);
                go.name = "Action_" + label;
                Button button = go.GetComponent<Button>();
                Text text = go.GetComponentInChildren<Text>();
                RectTransform rect = go.transform as RectTransform;
                if (rect != null)
                {
                    rect.sizeDelta = new Vector2(304f, 22f);
                }

                LayoutElement layout = go.GetComponent<LayoutElement>();
                if (layout == null)
                {
                    layout = go.AddComponent<LayoutElement>();
                }

                layout.minHeight = 22f;
                layout.preferredHeight = 22f;
                layout.preferredWidth = 304f;

                if (text != null)
                {
                    text.text = label;
                    text.alignment = TextAnchor.MiddleCenter;
                    text.resizeTextForBestFit = true;
                    text.resizeTextMinSize = 9;
                    text.resizeTextMaxSize = 12;
                    DebugInspectorBuilder.Stretch(text.rectTransform);
                }

                button.BindButton(action);
                return;
            }

            DebugInspectorBuilder.CreateButton(_buttonContainer, label, action);
        }

        // Stores Unity log messages.
        private void OnLogReceived(string condition, string stackTrace, LogType type)
        {
            if (_manager == null)
            {
                _manager = new DebugConsoleManager(_maxLogCount);
            }

            _manager.CaptureLog(condition, stackTrace, type);
            RefreshConsoleText();
        }

        // Clears captured logs.
        private void ClearLogs()
        {
            _manager.ClearLogs();
            RefreshConsoleText();
        }

        // Shows or hides only the inspector panel so the hotkey can bring it back.
        private void TogglePanel()
        {
            if (_panelRoot != null)
            {
                _panelRoot.gameObject.SetActive(!_panelRoot.gameObject.activeSelf);
            }
        }

        public sealed class DebugInspectorBuilder
        {
            private readonly Font _font;
            private readonly Transform _root;
            private RectTransform _panel;

            public RectTransform Panel => _panel;

            public static readonly Color PanelColor = new Color(0.12f, 0.15f, 0.21f, 0.94f);
            public static readonly Color SectionColor = new Color(0.08f, 0.1f, 0.14f, 0.95f);
            public static readonly Color RowColor = new Color(0.04f, 0.05f, 0.07f, 0.95f);
            public static readonly Color TextColor = new Color(0.82f, 0.86f, 0.92f, 1f);
            public static readonly Color MutedTextColor = new Color(0.6f, 0.66f, 0.74f, 1f);
            public static readonly Color AccentColor = new Color(0.35f, 0.58f, 0.95f, 1f);

            private DebugInspectorBuilder(Transform root)
            {
                _root = root;
                _font = DebugUIFactory.GetBuiltinFont();
            }

            // Starts a new inspector builder.
            public static DebugInspectorBuilder Create(Transform root)
            {
                return new DebugInspectorBuilder(root);
            }

            // Creates the root panel.
            public DebugInspectorBuilder WithPanel(Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition)
            {
                _panel = CreateRect("Panel", _root);
                _panel.anchorMin = anchorMin;
                _panel.anchorMax = anchorMax;
                _panel.pivot = new Vector2(1f, 0.5f);
                _panel.sizeDelta = sizeDelta;
                _panel.anchoredPosition = anchoredPosition;
                AddImage(_panel, PanelColor);

                VerticalLayoutGroup layout = _panel.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(10, 10, 10, 10);
                layout.spacing = 8f;
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;
                return this;
            }

            // Adds a header row with a close button.
            public DebugInspectorBuilder WithHeader(string title, out Button closeButton)
            {
                RectTransform header = CreateRect("Header", _panel);
                LayoutElement headerLayout = header.gameObject.AddComponent<LayoutElement>();
                headerLayout.preferredHeight = 34f;
                headerLayout.preferredWidth = 340f;

                HorizontalLayoutGroup row = header.gameObject.AddComponent<HorizontalLayoutGroup>();
                row.spacing = 8f;
                row.childControlHeight = false;
                row.childControlWidth = false;
                row.childForceExpandHeight = false;
                row.childForceExpandWidth = false;

                Text label = CreateText("Title", header, title, 14, FontStyle.Bold, TextColor);
                label.alignment = TextAnchor.MiddleLeft;
                LayoutElement labelLayout = label.gameObject.AddComponent<LayoutElement>();
                labelLayout.preferredWidth = 294f;
                labelLayout.preferredHeight = 34f;
                label.rectTransform.sizeDelta = new Vector2(294f, 34f);

                closeButton = CreateButton(header, "X", null);
                LayoutElement closeLayout = closeButton.gameObject.GetComponent<LayoutElement>();
                closeLayout.preferredWidth = 34f;
                closeLayout.preferredHeight = 34f;
                ((RectTransform)closeButton.transform).sizeDelta = new Vector2(34f, 34f);
                return this;
            }

            // Adds a scroll view and returns its content transform.
            public DebugInspectorBuilder WithScroll(out RectTransform content)
            {
                RectTransform scrollRoot = CreateRect("Scroll", _panel);
                LayoutElement scrollLayout = scrollRoot.gameObject.AddComponent<LayoutElement>();
                scrollLayout.preferredWidth = 340f;
                scrollLayout.flexibleHeight = 1f;

                AddImage(scrollRoot, new Color(0f, 0f, 0f, 0.12f));
                ScrollRect scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
                scrollRect.horizontal = false;

                RectTransform viewport = CreateRect("Viewport", scrollRoot);
                viewport.anchorMin = Vector2.zero;
                viewport.anchorMax = Vector2.one;
                viewport.offsetMin = Vector2.zero;
                viewport.offsetMax = Vector2.zero;
                Mask mask = viewport.gameObject.AddComponent<Mask>();
                mask.showMaskGraphic = false;
                AddImage(viewport, new Color(0f, 0f, 0f, 0.01f));

                content = CreateRect("Content", viewport);
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(0.5f, 1f);
                content.anchoredPosition = Vector2.zero;
                content.sizeDelta = Vector2.zero;

                VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 8f;
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;
                content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                scrollRect.viewport = viewport;
                scrollRect.content = content;
                return this;
            }

            // Creates a new debug UI builder.
            public DebugUIBuilder Section(RectTransform parent, string title)
            {
                return new DebugUIBuilder(parent, title, _font);
            }

            // Creates a named group/folder of debug widgets.
            public DebugUIBuilder CreateFolder(RectTransform parent, string title)
            {
                return Section(parent, title);
            }

            // Creates a named group/folder of debug widgets.
            public DebugUIBuilder Folder(RectTransform parent, string title)
            {
                return Section(parent, title);
            }

            // Creates a standalone button under a parent.
            public static Button CreateButton(RectTransform parent, string label, Action action)
            {
                return DebugUIFactory.CreateButton(parent, label, action, RowColor, TextColor);
            }

            public static RectTransform CreateRect(string name, Transform parent)
            {
                return DebugUIFactory.CreateRect(name, parent);
            }

            public static Text CreateText(string name, Transform parent, string value, int size, FontStyle style, Color color)
            {
                return DebugUIFactory.CreateText(name, parent, value, size, style, color);
            }

            public static Image AddImage(RectTransform rect, Color color)
            {
                return DebugUIFactory.AddImage(rect, color);
            }

            public static void Stretch(RectTransform rect)
            {
                DebugUIFactory.FullAnchor(rect);
            }

            public static void FillParent(RectTransform rect)
            {
                Stretch(rect);
            }
 
            public static void FullAnchor(RectTransform rect)
            {
                Stretch(rect);
            }

            public static ColorBlock ButtonColors()
            {
                return DebugUIFactory.ButtonColors(RowColor);
            }

            public static void UpdateSparkline(RawImage image, float[] values, Color lineColor)
            {
                DebugUIBuilder.UpdateSparkline(image, values, lineColor);
            } 
        }
    }
}
