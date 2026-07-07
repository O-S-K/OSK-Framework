using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static OSK.DebugConsoleView.DebugInspectorBuilder;

namespace OSK
{
    public sealed class DebugUIBuilder
    {
        public RectTransform Root { get; }
        public RectTransform Content { get; }
        private readonly Font _font;
        private readonly Text _headerLabel;

        public DebugUIBuilder(RectTransform parent, string title, Font font)
        {
            _font = font;
            Root = CreateRect("Section_" + title, parent);
            AddImage(Root, SectionColor);

            VerticalLayoutGroup layout = Root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 6f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            Root.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _headerLabel = CreateText("Header", Root, title, 12, FontStyle.Bold, MutedTextColor);
            _headerLabel.alignment = TextAnchor.MiddleLeft;
            LayoutElement headerTextLayout = _headerLabel.gameObject.AddComponent<LayoutElement>();
            headerTextLayout.preferredHeight = 20f;
            headerTextLayout.preferredWidth = 304f;
            _headerLabel.rectTransform.sizeDelta = new Vector2(304f, 20f);

            Content = CreateRect("Rows", Root);
            VerticalLayoutGroup rows = Content.gameObject.AddComponent<VerticalLayoutGroup>();
            rows.spacing = 4f;
            rows.childControlHeight = false;
            rows.childControlWidth = true;
            rows.childForceExpandHeight = false;
            rows.childForceExpandWidth = true;
            Content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // Makes this section collapsible by clicking its header.
        public DebugUIBuilder Foldout(bool expanded = true)
        {
            Content.gameObject.SetActive(expanded);
            _headerLabel.text = (expanded ? "v  " : ">  ") + _headerLabel.text;
            _headerLabel.raycastTarget = true;
            Button button = _headerLabel.gameObject.AddComponent<Button>();
            button.targetGraphic = _headerLabel;
            button.onClick.AddListener(() =>
            {
                bool isOpen = !Content.gameObject.activeSelf;
                Content.gameObject.SetActive(isOpen);
                string title = _headerLabel.text.Length > 3 ? _headerLabel.text.Substring(3) : _headerLabel.text;
                _headerLabel.text = (isOpen ? "v  " : ">  ") + title;
                LayoutRebuilder.ForceRebuildLayoutImmediate(Root);
            });
            return this;
        }

        // Adds a multi-line text block.
        public DebugUIBuilder TextBlock(out Text text, int size, FontStyle style)
        {
            return TextBlock(out text, size, style, 44f);
        }

        // Adds a multi-line text block with explicit height.
        public DebugUIBuilder TextBlock(out Text text, int size, FontStyle style, float height)
        {
            text = CreateText("Text", Content, string.Empty, size, style, TextColor);
            text.font = _font;
            text.alignment = TextAnchor.UpperLeft;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            LayoutElement textLayout = text.gameObject.AddComponent<LayoutElement>();
            textLayout.minHeight = height;
            textLayout.preferredHeight = height;
            textLayout.preferredWidth = 304f;
            text.rectTransform.sizeDelta = new Vector2(304f, height);
            return this;
        }

        // Adds a text label/value area.
        public DebugUIBuilder TextField(out Text text, int size, FontStyle style)
        {
            return TextBlock(out text, size, style);
        }

        // Adds a text label/value area with explicit height.
        public DebugUIBuilder TextField(out Text text, int size, FontStyle style, float height)
        {
            return TextBlock(out text, size, style, height);
        }

        // Adds a read-only text label/value area.
        public DebugUIBuilder Label(out Text text, int size, FontStyle style)
        {
            return TextBlock(out text, size, style);
        }

        // Adds a read-only text label/value area with explicit height.
        public DebugUIBuilder Label(out Text text, int size, FontStyle style, float height)
        {
            return TextBlock(out text, size, style, height);
        }

        // Adds a scrollable console log area.
        public DebugUIBuilder ConsoleLog(out RectTransform logContent, float height)
        {
            RectTransform scrollRoot = CreateRect("ConsoleScroll", Content);
            LayoutElement scrollLayout = scrollRoot.gameObject.AddComponent<LayoutElement>();
            scrollLayout.preferredHeight = height;
            scrollLayout.preferredWidth = 304f;
            scrollRoot.sizeDelta = new Vector2(304f, height);
            AddImage(scrollRoot, new Color(0f, 0f, 0f, 0.92f));

            ScrollRect scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            RectTransform viewport = CreateRect("Viewport", scrollRoot);
            Stretch(viewport);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            AddImage(viewport, new Color(0f, 0f, 0f, 0.01f));

            logContent = CreateRect("Content", viewport);
            logContent.anchorMin = new Vector2(0f, 1f);
            logContent.anchorMax = new Vector2(1f, 1f);
            logContent.pivot = new Vector2(0.5f, 1f);
            logContent.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = logContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 4, 4);
            layout.spacing = 2f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            logContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = logContent;
            return this;
        }

        // Adds a clickable button row.
        public DebugUIBuilder Button(string label, Action action)
        {
            CreateButton(Content, label, action);
            return this;
        }

        // Adds a clickable action button.
        public DebugUIBuilder ButtonAction(string label, Action action)
        {
            return Button(label, action);
        }

        // Adds a clickable action button.
        public DebugUIBuilder ActionButton(string label, Action action)
        {
            return Button(label, action);
        }

        // Adds a dangerous action that requires two clicks.
        public DebugUIBuilder DangerButton(string label, Action action)
        {
            Button button = CreateButton(Content, label, null);
            Text text = button.GetComponentInChildren<Text>();
            Image image = button.GetComponent<Image>();
            bool armed = false;
            if (image != null)
            {
                image.color = new Color(0.32f, 0.08f, 0.08f, 1f);
            }

            button.onClick.AddListener(() =>
            {
                if (!armed)
                {
                    armed = true;
                    if (text != null)
                    {
                        text.text = "Confirm " + label;
                    }
                    return;
                }

                armed = false;
                if (text != null)
                {
                    text.text = label;
                }
                action?.Invoke();
            });
            return this;
        }

        // Adds a tab row.
        public DebugUIBuilder TabBar(string[] tabs, int selectedIndex, Action<int> onChanged, out Button[] buttons)
        {
            int count = tabs == null ? 0 : tabs.Length;
            buttons = new Button[count];
            RectTransform row = CreateRow("Tabs");
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;

            float width = count <= 0 ? 304f : (304f - (count - 1) * 4f - 16f) / count;
            for (int i = 0; i < count; i++)
            {
                int index = i;
                Button button = CreateButton(row, tabs[i], () => onChanged?.Invoke(index));
                LayoutElement buttonLayout = button.GetComponent<LayoutElement>();
                buttonLayout.preferredWidth = width;
                buttonLayout.preferredHeight = 22f;
                ((RectTransform)button.transform).sizeDelta = new Vector2(width, 22f);
                Image image = button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = i == selectedIndex ? AccentColor : RowColor;
                }
                buttons[i] = button;
            }

            return this;
        }

        // Adds a destructive action button that requires two clicks.
        public DebugUIBuilder ConfirmButton(string label, Action action)
        {
            return DangerButton(label, action);
        }

        // Adds a destructive action button that requires two clicks.
        public DebugUIBuilder DangerousAction(string label, Action action)
        {
            return DangerButton(label, action);
        }

        // Adds a search input row.
        public DebugUIBuilder SearchField(string label, string value, Action<string> onChanged, out InputField input)
        {
            RectTransform row = CreateRow("Search_" + label);
            Text text = CreateText("Label", row, label, 12, FontStyle.Normal, TextColor);
            text.alignment = TextAnchor.MiddleLeft;
            LayoutElement labelLayout = text.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 74f;
            labelLayout.preferredHeight = 24f;
            text.rectTransform.sizeDelta = new Vector2(74f, 24f);

            input = CreateInput(row, value, 202f);
            input.onValueChanged.AddListener(raw => onChanged?.Invoke(raw));
            return this;
        }

        // Adds a command input row with a run button.
        public DebugUIBuilder CommandInput(string label, Action<string> onSubmit, out InputField input)
        {
            return CommandInput(label, string.Empty, onSubmit, out input);
        }

        // Adds a command input row with a hint placeholder and a run button.
        public DebugUIBuilder CommandInput(string label, string placeholder, Action<string> onSubmit, out InputField input)
        {
            return CommandInput(label, placeholder, null, onSubmit, out input);
        }

        // Adds a command input row with prefix suggestions and tab autocomplete.
        public DebugUIBuilder CommandInput(string label, string placeholder, string[] commandUsages, Action<string> onSubmit, out InputField input)
        {
            RectTransform row = CreateRow("Command_" + label);
            InputField commandInput = CreateInput(row, string.Empty, 220f, placeholder);
            commandInput.textComponent.alignment = TextAnchor.MiddleLeft;
            commandInput.textComponent.rectTransform.offsetMin = new Vector2(6f, 0f);
            commandInput.textComponent.rectTransform.offsetMax = new Vector2(-4f, 0f);
            Text placeholderText = commandInput.placeholder as Text;
            if (placeholderText != null)
            {
                placeholderText.alignment = TextAnchor.MiddleLeft;
                placeholderText.horizontalOverflow = HorizontalWrapMode.Overflow;
                placeholderText.resizeTextForBestFit = true;
                placeholderText.resizeTextMinSize = 7;
                placeholderText.rectTransform.offsetMin = new Vector2(6f, 0f);
                placeholderText.rectTransform.offsetMax = new Vector2(-4f, 0f);
            }

            Button run = CreateButton(row, label, () => onSubmit?.Invoke(commandInput.text));
            LayoutElement runLayout = run.GetComponent<LayoutElement>();
            runLayout.preferredWidth = 56f;
            runLayout.preferredHeight = 22f;
            ((RectTransform)run.transform).sizeDelta = new Vector2(56f, 22f);
            commandInput.onEndEdit.AddListener(raw =>
            {
                if (!string.IsNullOrEmpty(raw) && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
                {
                    onSubmit?.Invoke(raw);
                }
            });
            AttachCommandAutocomplete(commandInput, commandUsages);
            input = commandInput;
            return this;
        }

        private static void AttachCommandAutocomplete(InputField input, string[] commandUsages)
        {
            if (input == null || commandUsages == null || commandUsages.Length == 0)
            {
                return;
            }

            Text suggestionText = CreateText(
                "Suggestion",
                input.transform,
                string.Empty,
                11,
                FontStyle.Normal,
                new Color(0.55f, 0.65f, 0.78f, 0.45f));
            suggestionText.alignment = TextAnchor.MiddleLeft;
            suggestionText.horizontalOverflow = HorizontalWrapMode.Overflow;
            FullAnchor(suggestionText.rectTransform);
            suggestionText.rectTransform.offsetMin = new Vector2(6f, 0f);
            suggestionText.rectTransform.offsetMax = new Vector2(-4f, 0f);
            suggestionText.transform.SetAsFirstSibling();

            DebugCommandInputAutocomplete autocomplete = input.gameObject.AddComponent<DebugCommandInputAutocomplete>();
            autocomplete.Input = input;
            autocomplete.SuggestionText = suggestionText;
            autocomplete.CommandUsages = commandUsages;
        }

        // Adds a tiny table/list view.
        public DebugUIBuilder Table(string[] headers, string[,] rows)
        {
            int columns = headers == null ? 0 : headers.Length;
            if (columns <= 0)
            {
                return this;
            }

            AddTableRow("Table_Header", headers, true);
            int rowCount = rows == null ? 0 : rows.GetLength(0);
            for (int r = 0; r < rowCount; r++)
            {
                string[] values = new string[columns];
                for (int c = 0; c < columns; c++)
                {
                    values[c] = rows[r, c];
                }
                AddTableRow("Table_Row_" + r, values, false);
            }
            return this;
        }

        // Adds a compact sparkline graph.
        public DebugUIBuilder Sparkline(string label, float[] values, Color lineColor)
        {
            RawImage image;
            return Sparkline(label, values, lineColor, out image);
        }

        // Adds a compact sparkline graph and exposes its image for live updates.
        public DebugUIBuilder Sparkline(string label, float[] values, Color lineColor, out RawImage image)
        {
            RectTransform row = CreateRow("Sparkline_" + label);
            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.minHeight = 34f;
            rowLayout.preferredHeight = 34f;
            row.sizeDelta = new Vector2(304f, 34f);
            Text text = CreateText("Label", row, label, 12, FontStyle.Normal, TextColor);
            text.alignment = TextAnchor.MiddleLeft;
            LayoutElement labelLayout = text.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 74f;
            labelLayout.preferredHeight = 34f;
            text.rectTransform.sizeDelta = new Vector2(74f, 34f);

            RectTransform graph = CreateRect("Graph", row);
            graph.sizeDelta = new Vector2(202f, 34f);
            LayoutElement graphLayout = graph.gameObject.AddComponent<LayoutElement>();
            graphLayout.preferredWidth = 202f;
            graphLayout.preferredHeight = 34f;
            image = graph.gameObject.AddComponent<RawImage>();
            image.texture = CreateSparklineTexture(values, lineColor, 202, 34);
            image.raycastTarget = false;
            return this;
        }

        private void AddTableRow(string name, string[] values, bool header)
        {
            RectTransform row = CreateRow(name);
            int count = values.Length;
            float width = (304f - 16f - (count - 1) * 4f) / count;
            for (int i = 0; i < count; i++)
            {
                Text text = CreateText("Cell_" + i, row, values[i], 10, header ? FontStyle.Bold : FontStyle.Normal, header ? AccentColor : TextColor);
                text.alignment = i == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
                layout.preferredWidth = width;
                layout.preferredHeight = 24f;
                text.rectTransform.sizeDelta = new Vector2(width, 24f);
            }
        }

        // Adds a toggle row.
        public DebugUIBuilder Toggle(string label, bool value, Action<bool> onChanged, out Toggle toggle)
        {
            RectTransform row = CreateRow("Toggle_" + label);
            Text text = CreateText("Label", row, label, 12, FontStyle.Normal, TextColor);
            text.alignment = TextAnchor.MiddleLeft;
            LayoutElement labelLayout = text.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 268f;
            labelLayout.preferredHeight = 24f;
            text.rectTransform.sizeDelta = new Vector2(268f, 24f);

            RectTransform box = CreateRect("Box", row);
            LayoutElement boxLayout = box.gameObject.AddComponent<LayoutElement>();
            boxLayout.preferredWidth = 22f;
            boxLayout.preferredHeight = 22f;
            box.sizeDelta = new Vector2(22f, 22f);
            Image boxImage = AddImage(box, new Color(0f, 0f, 0f, 0.95f));

            Text checkText = CreateText("Check", box, "v", 18, FontStyle.Bold, AccentColor);
            checkText.alignment = TextAnchor.MiddleCenter;
            Stretch(checkText.rectTransform);

            toggle = row.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = boxImage;
            toggle.graphic = checkText;
            toggle.isOn = value;
            toggle.onValueChanged.AddListener(v => onChanged?.Invoke(v));
            return this;
        }

        // Adds a compact key/value row for counters, ids, long values, and read-only state.
        public DebugUIBuilder KeyValueRow(string label, string value, Color valueColor)
        {
            RectTransform row = CreateRow("KeyValue_" + label);
            Text key = CreateText("Key", row, label, 12, FontStyle.Normal, TextColor);
            key.alignment = TextAnchor.MiddleLeft;
            LayoutElement keyLayout = key.gameObject.AddComponent<LayoutElement>();
            keyLayout.preferredWidth = 132f;
            keyLayout.preferredHeight = 24f;
            key.rectTransform.sizeDelta = new Vector2(132f, 24f);

            Text valueText = CreateText("Value", row, value, 12, FontStyle.Bold, valueColor);
            valueText.alignment = TextAnchor.MiddleRight;
            valueText.horizontalOverflow = HorizontalWrapMode.Overflow;
            LayoutElement valueLayout = valueText.gameObject.AddComponent<LayoutElement>();
            valueLayout.preferredWidth = 140f;
            valueLayout.preferredHeight = 24f;
            valueText.rectTransform.sizeDelta = new Vector2(140f, 24f);
            return this;
        }

        // Adds a read-only label/value row.
        public DebugUIBuilder InfoRow(string label, string value, Color valueColor)
        {
            return KeyValueRow(label, value, valueColor);
        }

        // Adds a read-only label/value row.
        public DebugUIBuilder ValueRow(string label, string value, Color valueColor)
        {
            return KeyValueRow(label, value, valueColor);
        }

        // Adds a read-only label/value row.
        public DebugUIBuilder ReadOnlyField(string label, string value, Color valueColor)
        {
            return KeyValueRow(label, value, valueColor);
        }

        // Adds a small status badge row.
        public DebugUIBuilder StatusBadge(string label, string status, Color statusColor)
        {
            RectTransform row = CreateRow("Badge_" + label);
            Text key = CreateText("Key", row, label, 12, FontStyle.Normal, TextColor);
            key.alignment = TextAnchor.MiddleLeft;
            LayoutElement keyLayout = key.gameObject.AddComponent<LayoutElement>();
            keyLayout.preferredWidth = 154f;
            keyLayout.preferredHeight = 24f;
            key.rectTransform.sizeDelta = new Vector2(154f, 24f);

            RectTransform badge = CreateRect("Status", row);
            badge.sizeDelta = new Vector2(118f, 22f);
            LayoutElement badgeLayout = badge.gameObject.AddComponent<LayoutElement>();
            badgeLayout.preferredWidth = 118f;
            badgeLayout.preferredHeight = 22f;
            AddImage(badge, statusColor);

            Text text = CreateText("Text", badge, status, 11, FontStyle.Bold, Color.white);
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            Stretch(text.rectTransform);
            return this;
        }

        // Adds a progress bar row.
        public DebugUIBuilder ProgressBar(string label, float current, float max, Color fillColor)
        {
            float normalized = max <= 0f ? 0f : Mathf.Clamp01(current / max);
            RectTransform row = CreateRow("Progress_" + label);
            Text key = CreateText("Key", row, label, 12, FontStyle.Normal, TextColor);
            key.alignment = TextAnchor.MiddleLeft;
            LayoutElement keyLayout = key.gameObject.AddComponent<LayoutElement>();
            keyLayout.preferredWidth = 82f;
            keyLayout.preferredHeight = 24f;
            key.rectTransform.sizeDelta = new Vector2(82f, 24f);

            RectTransform bar = CreateRect("Bar", row);
            bar.sizeDelta = new Vector2(142f, 18f);
            LayoutElement barLayout = bar.gameObject.AddComponent<LayoutElement>();
            barLayout.preferredWidth = 142f;
            barLayout.preferredHeight = 18f;
            AddImage(bar, new Color(0f, 0f, 0f, 0.95f));

            RectTransform fill = CreateRect("Fill", bar);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(normalized, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            AddImage(fill, fillColor);

            Text value = CreateText("Value", row, Mathf.RoundToInt(current) + "/" + Mathf.RoundToInt(max), 11, FontStyle.Bold, TextColor);
            value.alignment = TextAnchor.MiddleRight;
            LayoutElement valueLayout = value.gameObject.AddComponent<LayoutElement>();
            valueLayout.preferredWidth = 54f;
            valueLayout.preferredHeight = 24f;
            value.rectTransform.sizeDelta = new Vector2(54f, 24f);
            return this;
        }

        // Adds an inventory/resource row with a color icon, bar, and compact value.
        public DebugUIBuilder ResourceBar(string label, long amount, long capacity, Color iconColor, Color fillColor)
        {
            float normalized = capacity <= 0L ? 0f : Mathf.Clamp01((float)amount / capacity);
            RectTransform row = CreateRow("Resource_" + label);

            RectTransform icon = CreateRect("Icon", row);
            icon.sizeDelta = new Vector2(20f, 20f);
            LayoutElement iconLayout = icon.gameObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 20f;
            iconLayout.preferredHeight = 20f;
            AddImage(icon, iconColor);

            Text key = CreateText("Key", row, label, 11, FontStyle.Bold, TextColor);
            key.alignment = TextAnchor.MiddleLeft;
            LayoutElement keyLayout = key.gameObject.AddComponent<LayoutElement>();
            keyLayout.preferredWidth = 74f;
            keyLayout.preferredHeight = 24f;
            key.rectTransform.sizeDelta = new Vector2(74f, 24f);

            RectTransform bar = CreateRect("Bar", row);
            bar.sizeDelta = new Vector2(100f, 16f);
            LayoutElement barLayout = bar.gameObject.AddComponent<LayoutElement>();
            barLayout.preferredWidth = 100f;
            barLayout.preferredHeight = 16f;
            AddImage(bar, new Color(0f, 0f, 0f, 0.95f));

            RectTransform fill = CreateRect("Fill", bar);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(normalized, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            AddImage(fill, fillColor);

            Text value = CreateText("Value", row, FormatCompact(amount), 11, FontStyle.Bold, TextColor);
            value.alignment = TextAnchor.MiddleRight;
            LayoutElement valueLayout = value.gameObject.AddComponent<LayoutElement>();
            valueLayout.preferredWidth = 54f;
            valueLayout.preferredHeight = 24f;
            value.rectTransform.sizeDelta = new Vector2(54f, 24f);
            return this;
        }

        private static string FormatCompact(long value)
        {
            if (value >= 1000000000L) return (value / 1000000000f).ToString("0.##") + "B";
            if (value >= 1000000L) return (value / 1000000f).ToString("0.##") + "M";
            if (value >= 1000L) return (value / 1000f).ToString("0.##") + "K";
            return value.ToString();
        }

        // Adds a numeric input row.
        public DebugUIBuilder StringField(string label, string value, Action<string> onChanged, out InputField input)
        {
            RectTransform row = CreateRow("String_" + label);
            Text text = CreateText("Label", row, label, 12, FontStyle.Normal, TextColor);
            text.alignment = TextAnchor.MiddleLeft;
            LayoutElement labelLayout = text.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 88f;
            labelLayout.preferredHeight = 24f;
            text.rectTransform.sizeDelta = new Vector2(88f, 24f);

            input = CreateInput(row, value, 188f);
            input.onEndEdit.AddListener(raw => onChanged?.Invoke(raw));
            return this;
        }

        // Adds a numeric input row.
        public DebugUIBuilder FloatField(string label, float value, Action<float> onChanged, out InputField input)
        {
            RectTransform row = CreateRow("Float_" + label);
            Text text = CreateText("Label", row, label, 12, FontStyle.Normal, TextColor);
            text.alignment = TextAnchor.MiddleLeft;
            LayoutElement labelLayout = text.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 120f;
            labelLayout.preferredHeight = 24f;
            text.rectTransform.sizeDelta = new Vector2(120f, 24f);

            input = CreateInput(row, FormatFloat(value, "0.###"), 152f);
            input.onEndEdit.AddListener(raw =>
            {
                if (TryParseFloat(raw, out float parsed))
                {
                    onChanged?.Invoke(parsed);
                }
            });
            return this;
        }

        // Adds a Vector3 row with X, Y, and Z inputs.
        public DebugUIBuilder Vector3Field(string label, Vector3 value, Action<Vector3> onChanged)
        {
            RectTransform row = CreateRow("Vector3_" + label);
            Text text = CreateText("Label", row, label, 12, FontStyle.Normal, TextColor);
            text.alignment = TextAnchor.MiddleLeft;
            LayoutElement labelLayout = text.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 82f;
            labelLayout.preferredHeight = 24f;
            text.rectTransform.sizeDelta = new Vector2(82f, 24f);

            InputField x = CreateInput(row, FormatFloat(value.x), 58f);
            InputField y = CreateInput(row, FormatFloat(value.y), 58f);
            InputField z = CreateInput(row, FormatFloat(value.z), 58f);

            Action<string> apply = _ =>
            {
                Vector3 next = value;
                if (TryParseFloat(x.text, out float parsedX)) next.x = parsedX;
                if (TryParseFloat(y.text, out float parsedY)) next.y = parsedY;
                if (TryParseFloat(z.text, out float parsedZ)) next.z = parsedZ;
                value = next;
                onChanged?.Invoke(next);
            };

            x.onEndEdit.AddListener(raw => apply(raw));
            y.onEndEdit.AddListener(raw => apply(raw));
            z.onEndEdit.AddListener(raw => apply(raw));
            return this;
        }

        // Adds a Color row with R, G, B, and A inputs in 0-1 range.
        public DebugUIBuilder ColorField(string label, Color value, Action<Color> onChanged)
        {
            RectTransform row = CreateRow("Color_" + label);
            Text text = CreateText("Label", row, label, 12, FontStyle.Normal, TextColor);
            text.alignment = TextAnchor.MiddleLeft;
            LayoutElement labelLayout = text.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 52f;
            labelLayout.preferredHeight = 24f;
            text.rectTransform.sizeDelta = new Vector2(52f, 24f);

            Image swatch = AddImage(CreateRect("Swatch", row), value);
            swatch.raycastTarget = true;
            RectTransform swatchRect = swatch.rectTransform;
            swatchRect.sizeDelta = new Vector2(22f, 22f);
            LayoutElement swatchLayout = swatch.gameObject.AddComponent<LayoutElement>();
            swatchLayout.preferredWidth = 22f;
            swatchLayout.preferredHeight = 22f;
            Button swatchButton = swatch.gameObject.AddComponent<Button>();
            swatchButton.targetGraphic = swatch;

            InputField r = CreateInput(row, FormatFloat(value.r), 36f);
            InputField g = CreateInput(row, FormatFloat(value.g), 36f);
            InputField b = CreateInput(row, FormatFloat(value.b), 36f);
            InputField a = CreateInput(row, FormatFloat(value.a), 36f);
            RectTransform picker = CreateColorPicker(value, out RawImage wheel, out RectTransform wheelMarker, out InputField hexInput);
            picker.gameObject.SetActive(false);
            swatchButton.onClick.AddListener(() =>
            {
                picker.gameObject.SetActive(!picker.gameObject.activeSelf);
                LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)Content.parent);
            });

            Action<Color, bool> setColor = (next, updateInputs) =>
            {
                next.r = Mathf.Clamp01(next.r);
                next.g = Mathf.Clamp01(next.g);
                next.b = Mathf.Clamp01(next.b);
                next.a = Mathf.Clamp01(next.a);
                value = next;
                swatch.color = next;
                if (updateInputs)
                {
                    r.text = FormatFloat(next.r);
                    g.text = FormatFloat(next.g);
                    b.text = FormatFloat(next.b);
                    a.text = FormatFloat(next.a);
                }

                hexInput.text = ColorUtility.ToHtmlStringRGBA(next);
                MoveColorMarker(wheelMarker, wheel.rectTransform, next);
                onChanged?.Invoke(next);
            };

            Action<string> applyNumbers = _ =>
            {
                Color next = value;
                if (TryParseFloat(r.text, out float parsedR)) next.r = Mathf.Clamp01(parsedR);
                if (TryParseFloat(g.text, out float parsedG)) next.g = Mathf.Clamp01(parsedG);
                if (TryParseFloat(b.text, out float parsedB)) next.b = Mathf.Clamp01(parsedB);
                if (TryParseFloat(a.text, out float parsedA)) next.a = Mathf.Clamp01(parsedA);
                setColor(next, false);
            };

            r.onEndEdit.AddListener(raw => applyNumbers(raw));
            g.onEndEdit.AddListener(raw => applyNumbers(raw));
            b.onEndEdit.AddListener(raw => applyNumbers(raw));
            a.onEndEdit.AddListener(raw => applyNumbers(raw));
            hexInput.onEndEdit.AddListener(raw =>
            {
                string hex = raw.TrimStart('#');
                if (ColorUtility.TryParseHtmlString("#" + hex, out Color parsed))
                {
                    parsed.a = hex.Length <= 6 ? value.a : parsed.a;
                    setColor(parsed, true);
                }
            });
            AddColorWheelEvents(wheel, next =>
            {
                next.a = value.a;
                setColor(next, true);
            });
            setColor(value, true);
            return this;
        }

        // Adds a slider row.
        public DebugUIBuilder SliderField(string label, float value, float min, float max, Action<float> onChanged, out Slider slider)
        {
            RectTransform row = CreateRow("Slider_" + label);
            Text text = CreateText("Label", row, label, 12, FontStyle.Normal, TextColor);
            text.alignment = TextAnchor.MiddleLeft;
            LayoutElement labelLayout = text.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 88f;
            labelLayout.preferredHeight = 24f;
            text.rectTransform.sizeDelta = new Vector2(88f, 24f);

            slider = CreateSlider(row, min, max, value, 142f);
            Text valueText = CreateText("Value", row, value.ToString("0.##"), 11, FontStyle.Normal, TextColor);
            valueText.alignment = TextAnchor.MiddleRight;
            LayoutElement valueLayout = valueText.gameObject.AddComponent<LayoutElement>();
            valueLayout.preferredWidth = 44f;
            valueLayout.preferredHeight = 24f;
            valueText.rectTransform.sizeDelta = new Vector2(44f, 24f);

            slider.onValueChanged.AddListener(next =>
            {
                valueText.text = next.ToString("0.##");
                onChanged?.Invoke(next);
            });
            return this;
        }

        // Adds a dropdown row.
        public DebugUIBuilder DropdownField(string label, string[] options, int selectedIndex, Action<int> onChanged, out Dropdown dropdown)
        {
            RectTransform row = CreateRow("Dropdown_" + label);
            Text text = CreateText("Label", row, label, 12, FontStyle.Normal, TextColor);
            text.alignment = TextAnchor.MiddleLeft;
            LayoutElement labelLayout = text.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 92f;
            labelLayout.preferredHeight = 24f;
            text.rectTransform.sizeDelta = new Vector2(92f, 24f);

            dropdown = CreateDropdown(row, options, selectedIndex, 180f);
            dropdown.onValueChanged.AddListener(index => onChanged?.Invoke(index));
            return this;
        }

        private RectTransform CreateColorPicker(Color initialColor, out RawImage wheel, out RectTransform wheelMarker, out InputField hexInput)
        {
            RectTransform picker = CreateRect("ColorPicker", Content);
            LayoutElement pickerLayout = picker.gameObject.AddComponent<LayoutElement>();
            pickerLayout.preferredHeight = 112f;
            pickerLayout.preferredWidth = 304f;
            picker.sizeDelta = new Vector2(304f, 112f);
            AddImage(picker, RowColor);

            HorizontalLayoutGroup layout = picker.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 8f;
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            wheel = CreateColorWheel(picker, 92f);
            wheel.color = Color.white;
            wheelMarker = CreateColorMarker(wheel.rectTransform);

            RectTransform right = CreateRect("PickerValues", picker);
            right.sizeDelta = new Vector2(180f, 92f);
            LayoutElement rightLayout = right.gameObject.AddComponent<LayoutElement>();
            rightLayout.preferredWidth = 180f;
            rightLayout.preferredHeight = 92f;
            VerticalLayoutGroup rightGroup = right.gameObject.AddComponent<VerticalLayoutGroup>();
            rightGroup.spacing = 4f;
            rightGroup.childControlHeight = false;
            rightGroup.childControlWidth = false;
            rightGroup.childForceExpandHeight = false;
            rightGroup.childForceExpandWidth = false;

            Text hexLabel = CreateText("HexLabel", right, "HEX", 10, FontStyle.Bold, MutedTextColor);
            LayoutElement labelLayout = hexLabel.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 180f;
            labelLayout.preferredHeight = 18f;
            hexLabel.rectTransform.sizeDelta = new Vector2(180f, 18f);

            hexInput = CreateInput(right, ColorUtility.ToHtmlStringRGBA(initialColor), 180f);
            Text hint = CreateText("Hint", right, "Click or drag wheel\nRGBA and HEX sync", 10, FontStyle.Normal, MutedTextColor);
            LayoutElement hintLayout = hint.gameObject.AddComponent<LayoutElement>();
            hintLayout.preferredWidth = 180f;
            hintLayout.preferredHeight = 42f;
            hint.rectTransform.sizeDelta = new Vector2(180f, 42f);
            return picker;
        }

        private static RawImage CreateColorWheel(RectTransform parent, float size)
        {
            RectTransform rect = CreateRect("Wheel", parent);
            rect.sizeDelta = new Vector2(size, size);
            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = size;
            layout.preferredHeight = size;

            RawImage image = rect.gameObject.AddComponent<RawImage>();
            image.texture = CreateColorWheelTexture(96);
            image.raycastTarget = true;
            return image;
        }

        private static RectTransform CreateColorMarker(RectTransform wheel)
        {
            Text marker = CreateText("Marker", wheel, "o", 14, FontStyle.Bold, Color.white);
            marker.alignment = TextAnchor.MiddleCenter;
            marker.raycastTarget = false;
            marker.rectTransform.sizeDelta = new Vector2(16f, 16f);
            return marker.rectTransform;
        }

        private static void AddColorWheelEvents(RawImage wheel, Action<Color> onPicked)
        {
            EventTrigger trigger = wheel.gameObject.AddComponent<EventTrigger>();
            AddEvent(trigger, EventTriggerType.PointerClick, data => PickWheelColor(wheel.rectTransform, data, onPicked));
            AddEvent(trigger, EventTriggerType.Drag, data => PickWheelColor(wheel.rectTransform, data, onPicked));
        }

        private static void AddEvent(EventTrigger trigger, EventTriggerType type, Action<BaseEventData> callback)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(data => callback(data));
            trigger.triggers.Add(entry);
        }

        private static void PickWheelColor(RectTransform wheel, BaseEventData data, Action<Color> onPicked)
        {
            PointerEventData pointer = data as PointerEventData;
            if (pointer == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(wheel, pointer.position, pointer.pressEventCamera, out Vector2 local))
            {
                return;
            }

            float radius = Mathf.Min(wheel.rect.width, wheel.rect.height) * 0.48f;
            Vector2 clamped = Vector2.ClampMagnitude(local, radius);
            float hue = Mathf.Atan2(clamped.y, clamped.x) / (Mathf.PI * 2f);
            if (hue < 0f)
            {
                hue += 1f;
            }

            onPicked?.Invoke(Color.HSVToRGB(hue, clamped.magnitude / radius, 1f));
        }

        private static void MoveColorMarker(RectTransform marker, RectTransform wheel, Color color)
        {
            float h;
            float s;
            float v;
            Color.RGBToHSV(color, out h, out s, out v);
            float radius = Mathf.Min(wheel.rect.width, wheel.rect.height) * 0.48f * s;
            float angle = h * Mathf.PI * 2f;
            marker.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        private static Texture2D CreateColorWheelTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size * 0.48f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 delta = new Vector2(x, y) - center;
                    float distance = delta.magnitude;
                    if (distance > radius)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float hue = Mathf.Atan2(delta.y, delta.x) / (Mathf.PI * 2f);
                    if (hue < 0f)
                    {
                        hue += 1f;
                    }

                    texture.SetPixel(x, y, Color.HSVToRGB(hue, distance / radius, 1f));
                }
            }

            texture.Apply();
            return texture;
        }

        // Redraws a sparkline image without creating a new texture each refresh.
        public static void UpdateSparkline(RawImage image, float[] values, Color lineColor)
        {
            if (image == null)
            {
                return;
            }

            Texture2D texture = image.texture as Texture2D;
            if (texture == null)
            {
                image.texture = CreateSparklineTexture(values, lineColor, 202, 34);
                return;
            }

            DrawSparklineTexture(texture, values, lineColor);
        }

        private static Texture2D CreateSparklineTexture(float[] values, Color lineColor, int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            DrawSparklineTexture(texture, values, lineColor);
            return texture;
        }

        private static void DrawSparklineTexture(Texture2D texture, float[] values, Color lineColor)
        {
            Color background = new Color(0f, 0f, 0f, 0.95f);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, background);
                }
            }

            if (values == null || values.Length == 0)
            {
                texture.Apply();
                return;
            }

            float min = values[0];
            float max = values[0];
            for (int i = 1; i < values.Length; i++)
            {
                min = Mathf.Min(min, values[i]);
                max = Mathf.Max(max, values[i]);
            }

            float range = Mathf.Max(0.0001f, max - min);
            Vector2 previous = Vector2.zero;
            for (int i = 0; i < values.Length; i++)
            {
                float x = values.Length <= 1 ? 0f : (texture.width - 1f) * i / (values.Length - 1f);
                float y = Mathf.Lerp(2f, texture.height - 3f, (values[i] - min) / range);
                Vector2 current = new Vector2(x, y);
                if (i > 0)
                {
                    DrawLine(texture, previous, current, lineColor);
                }
                previous = current;
            }

            texture.Apply();
        }

        private static void DrawLine(Texture2D texture, Vector2 start, Vector2 end, Color color)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(start, end));
            for (int i = 0; i <= steps; i++)
            {
                float t = steps == 0 ? 0f : (float)i / steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(start.x, end.x, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(start.y, end.y, t));
                if (x >= 0 && y >= 0 && x < texture.width && y < texture.height)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }

        private static Slider CreateSlider(RectTransform parent, float min, float max, float value, float width)
        {
            RectTransform root = CreateRect("Slider", parent);
            root.sizeDelta = new Vector2(width, 20f);
            LayoutElement rootLayout = root.gameObject.AddComponent<LayoutElement>();
            rootLayout.preferredWidth = width;
            rootLayout.preferredHeight = 20f;

            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = Mathf.Clamp(value, min, max);

            RectTransform background = CreateRect("Background", root);
            Stretch(background);
            background.offsetMin = new Vector2(0f, 7f);
            background.offsetMax = new Vector2(0f, -7f);
            AddImage(background, new Color(0f, 0f, 0f, 0.95f));

            RectTransform fillArea = CreateRect("Fill Area", root);
            Stretch(fillArea);
            fillArea.offsetMin = new Vector2(0f, 7f);
            fillArea.offsetMax = new Vector2(0f, -7f);

            RectTransform fill = CreateRect("Fill", fillArea);
            Stretch(fill);
            Image fillImage = AddImage(fill, AccentColor);

            RectTransform handle = CreateRect("Handle", root);
            handle.anchorMin = new Vector2(0f, 0f);
            handle.anchorMax = new Vector2(0f, 1f);
            handle.offsetMin = new Vector2(0f, -0.3f);
            handle.offsetMax = new Vector2(12f, -0.3f);
            handle.sizeDelta = new Vector2(12f, -0.6f);
            Image handleImage = AddImage(handle, TextColor);

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            return slider;
        }

        private static Dropdown CreateDropdown(RectTransform parent, string[] options, int selectedIndex, float width)
        {
            RectTransform root = CreateRect("Dropdown", parent);
            root.sizeDelta = new Vector2(width, 22f);
            LayoutElement rootLayout = root.gameObject.AddComponent<LayoutElement>();
            rootLayout.preferredWidth = width;
            rootLayout.preferredHeight = 22f;
            Image rootImage = AddImage(root, new Color(0f, 0f, 0f, 0.95f));
            rootImage.raycastTarget = true;
            SetLayerRecursively(root.gameObject, "UI");

            Dropdown dropdown = root.gameObject.AddComponent<Dropdown>();
            dropdown.targetGraphic = rootImage;
            dropdown.captionText = CreateText("Label", root, string.Empty, 11, FontStyle.Normal, TextColor);
            dropdown.captionText.alignment = TextAnchor.MiddleLeft;
            Stretch(dropdown.captionText.rectTransform);
            dropdown.captionText.rectTransform.offsetMin = new Vector2(8f, 0f);
            dropdown.captionText.rectTransform.offsetMax = new Vector2(-18f, 0f);

            Text arrow = CreateText("Arrow", root, "v", 11, FontStyle.Bold, MutedTextColor);
            arrow.alignment = TextAnchor.MiddleCenter;
            arrow.rectTransform.anchorMin = new Vector2(1f, 0f);
            arrow.rectTransform.anchorMax = new Vector2(1f, 1f);
            arrow.rectTransform.pivot = new Vector2(1f, 0.5f);
            arrow.rectTransform.sizeDelta = new Vector2(18f, 0f);
            arrow.rectTransform.anchoredPosition = Vector2.zero;

            int optionCount = options != null ? options.Length : 0;
            RectTransform template = CreateDropdownTemplate(root, optionCount);
            dropdown.template = template;
            Transform itemLabel = template.Find("Viewport/Content/Item/Item Label");
            if (itemLabel != null)
            {
                dropdown.itemText = itemLabel.GetComponent<Text>();
            }

            dropdown.options.Clear();
            if (options != null)
            {
                for (int i = 0; i < options.Length; i++)
                {
                    dropdown.options.Add(new Dropdown.OptionData(options[i]));
                }
            }

            dropdown.value = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, dropdown.options.Count - 1));
            dropdown.RefreshShownValue();
            SetLayerRecursively(root.gameObject, "UI");
            return dropdown;
        }

        private static RectTransform CreateDropdownTemplate(RectTransform parent, int optionCount)
        {
            float templateHeight = Mathf.Clamp(Mathf.Max(1, optionCount) * 28f + 10f, 64f, 200f);
            RectTransform template = CreateRect("Template", parent);
            template.anchorMin = new Vector2(0f, 0f);
            template.anchorMax = new Vector2(1f, 0f);
            template.pivot = new Vector2(0.5f, 1f);
            template.anchoredPosition = new Vector2(0f, -2f);
            template.sizeDelta = new Vector2(0f, templateHeight);
            AddImage(template, SectionColor);
            SetLayerRecursively(template.gameObject, "UI");
            Canvas canvas = template.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32767;
            template.gameObject.AddComponent<GraphicRaycaster>();
            template.gameObject.SetActive(false);

            ScrollRect scrollRect = template.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            RectTransform viewport = CreateRect("Viewport", template);
            Stretch(viewport);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            AddImage(viewport, new Color(0f, 0f, 0f, 0.01f));

            RectTransform content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, Mathf.Max(1, optionCount) * 28f + 8f);
            VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(4, 4, 4, 4);
            contentLayout.spacing = 3f;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Toggle item = CreateDropdownItem(content);
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            SetLayerRecursively(template.gameObject, "UI");
            return template;
        }

        private static Toggle CreateDropdownItem(RectTransform parent)
        {
            RectTransform itemRect = CreateRect("Item", parent);
            LayoutElement itemLayout = itemRect.gameObject.AddComponent<LayoutElement>();
            itemLayout.minHeight = 26f;
            itemLayout.preferredHeight = 26f;
            itemRect.sizeDelta = new Vector2(0f, 26f);
            Image itemImage = AddImage(itemRect, RowColor);
            itemImage.raycastTarget = true;
            DropdownItemRaycastFix itemFix = itemRect.gameObject.AddComponent<DropdownItemRaycastFix>();
            itemFix.Target = itemImage;

            Toggle toggle = itemRect.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = itemImage;

            Text checkmark = CreateText("Item Checkmark", itemRect, "v", 11, FontStyle.Bold, AccentColor);
            checkmark.alignment = TextAnchor.MiddleCenter;
            checkmark.rectTransform.anchorMin = new Vector2(0f, 0f);
            checkmark.rectTransform.anchorMax = new Vector2(0f, 1f);
            checkmark.rectTransform.pivot = new Vector2(0f, 0.5f);
            checkmark.rectTransform.sizeDelta = new Vector2(20f, 0f);
            toggle.graphic = checkmark;

            Text label = CreateText("Item Label", itemRect, "Option", 11, FontStyle.Normal, TextColor);
            label.alignment = TextAnchor.MiddleLeft;
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(24f, 0f);
            label.rectTransform.offsetMax = new Vector2(-4f, 0f);
            return toggle;
        }

        private static void SetLayerRecursively(GameObject target, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                return;
            }

            target.layer = layer;
            for (int i = 0; i < target.transform.childCount; i++)
            {
                SetLayerRecursively(target.transform.GetChild(i).gameObject, layerName);
            }
        }

        private static InputField CreateInput(RectTransform parent, string value, float width)
        {
            return CreateInput(parent, value, width, string.Empty);
        }

        private static InputField CreateInput(RectTransform parent, string value, float width, string placeholder)
        {
            RectTransform fieldRect = CreateRect("Input", parent);
            fieldRect.sizeDelta = new Vector2(width, 20f);
            LayoutElement fieldLayout = fieldRect.gameObject.AddComponent<LayoutElement>();
            fieldLayout.preferredWidth = width;
            fieldLayout.preferredHeight = 20f;
            AddImage(fieldRect, new Color(0f, 0f, 0f, 0.95f));

            InputField input = fieldRect.gameObject.AddComponent<InputField>();
            input.textComponent = CreateText("Text", fieldRect, value, 11, FontStyle.Normal, TextColor);
            input.textComponent.alignment = TextAnchor.MiddleCenter;
            Stretch(input.textComponent.rectTransform);
            input.placeholder = CreateText("Placeholder", fieldRect, placeholder, 11, FontStyle.Normal, MutedTextColor);
            Stretch(((Text)input.placeholder).rectTransform);
            input.text = value;
            return input;
        }

        private static bool TryParseFloat(string raw, out float value)
        {
            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            string normalized = string.IsNullOrEmpty(raw) ? string.Empty : raw.Replace(',', '.');
            return float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static string FormatFloat(float value, string format = "0.##")
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private RectTransform CreateRow(string name)
        {
            RectTransform row = CreateRect(name, Content);
            LayoutElement rowLayoutElement = row.gameObject.AddComponent<LayoutElement>();
            rowLayoutElement.minHeight = 24f;
            rowLayoutElement.preferredHeight = 24f;
            rowLayoutElement.preferredWidth = 304f;
            row.sizeDelta = new Vector2(304f, 24f);
            AddImage(row, RowColor);

            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 8f;
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            return row;
        }
    }
}
