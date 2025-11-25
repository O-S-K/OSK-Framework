#if  UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OSK
{
    public class ClipEditorUIHelper : IDisposable
    {
        // Public callbacks (window should subscribe)
        public Action<float> OnRequestScrub;                // absolute seconds
        public Action OnScrubEnd;
        public Action<float, float> OnRequestLoopPlay;     // loop start/end absolute seconds
        public Action<float> OnRequestSetPlayhead;         // absolute seconds (request move)
        public Action<float, float, Rect> OnRequestZoom;   // delta, mouseX, rect
        public Action<float> OnRequestPan;                 // delta pixels
        public Action<float, float> OnTrimChanged;         // start,end seconds
        public Action<float, float> OnFadeChanged;         // fadeInSec, fadeOutSec

        // Visible state (can be set by window before Draw)
        public float PlayheadTime { get; set; } = 0f; // absolute seconds
        public float TrimStart { get; set; } = 0f;   // absolute seconds
        public float TrimEnd { get; set; } = 1f;     // absolute seconds

        public bool IsLooping { get; set; } = false;
        public float LoopStart { get; set; } = 0f;
        public float LoopEnd { get; set; } = 0f;

        // Allow window to set reversed flag
        public bool IsReversed
        {
            get => _isReversed;
            set => _isReversed = value;
        }

        // internals
        private Texture2D _waveTex;
        private AudioClip _lastClip;
        private int _lastW = 0, _lastH = 0;
        private double _lastRegen = 0;
        private double _regenInterval = 0.25;
        private int _texWidthCap = 2048;

        public float ClipLength { get; private set; } = 1f;

        // Fade state (read from OSKAudioClip each Draw)
        private float _currentFadeIn = 0f;
        private float _currentFadeOut = 0f;

        // Interaction flags
        private bool _draggingPlayhead = false;
        private bool _draggingLeftHandle = false;
        private bool _draggingRightHandle = false;
        private bool _rightDragSelecting = false;

        private bool _draggingFadeInHandle = false;
        private bool _draggingFadeOutHandle = false;

        private float _rightDragStartAbs = 0f;

        // Visuals
        private Color _bg = new Color(0.08f, 0.08f, 0.08f);
        private Color _waveColor = EditorGUIUtility.isProSkin ? new Color(0.16f, 0.55f, 1f) : new Color(0.05f, 0.35f, 0.8f);
        private Color _fadeColor = new Color(0.18f, 0.75f, 0.35f, 1f);
        private const float MIN_FADE_PX = 2f;

        // Reversal flag
        private bool _isReversed = false;

        // constructor
        public ClipEditorUIHelper()
        {
        }

        /// <summary>
        /// Set both fades from external (window) and clamp so they don't overlap.
        /// </summary>
        public void SetFadeSeconds(float inSec, float outSec)
        {
            float selSec = Mathf.Max(0.0001f, TrimEnd - TrimStart);
            inSec = Mathf.Clamp(inSec, 0f, Mathf.Max(0f, selSec - outSec));
            outSec = Mathf.Clamp(outSec, 0f, Mathf.Max(0f, selSec - inSec));
            _currentFadeIn = inSec;
            _currentFadeOut = outSec;
        }

        public float CurrentFadeInSec => _currentFadeIn;
        public float CurrentFadeOutSec => _currentFadeOut;

        /// <summary>
        /// Draw waveform editor UI
        /// </summary>
        /// <param name="rect">area to draw waveform</param>
        /// <param name="clip">original audio clip</param>
        /// <param name="osk">OSKAudioClip (source of FadeIn/FadeOut default values and reversed flag)</param>
        public void Draw(Rect rect, AudioClip clip, OSKAudioClip osk)
        {
            if (clip == null)
            {
                EditorGUI.DrawRect(rect, _bg);
                return;
            }

            // Set reversal flag from osk if possible (window can also set IsReversed property)
            if (osk != null)
            {
                _isReversed = osk.IsReversed;
            }

            ClipLength = Mathf.Max(0.0001f, clip.length);

            // read fade values from passed OSKAudioClip (source of truth)
            if (osk != null)
            {
                _currentFadeIn = Mathf.Max(0f, osk.FadeInDuration);
                _currentFadeOut = Mathf.Max(0f, osk.FadeOutDuration);

                // ensure TrimStart/TrimEnd are synced if window set them previously
                if (!Mathf.Approximately(TrimStart, osk.StartTime) || !Mathf.Approximately(TrimEnd, osk.EndTime))
                {
                    TrimStart = osk.StartTime;
                    TrimEnd = osk.EndTime > 0f ? osk.EndTime : ClipLength;
                }
            }

            // rebuild waveform if needed
            RequestRebuildIfNeeded(rect, clip);

            // background + ruler
            EditorGUI.DrawRect(rect, _bg);
            DrawRuler(rect, clip);

            // draw waveform texture; if reversed, flip horizontally
            if (_waveTex != null)
            {
                if (_isReversed)
                {
                    GUI.DrawTextureWithTexCoords(rect, _waveTex, new Rect(1f, 0f, -1f, 1f));
                }
                else
                {
                    GUI.DrawTexture(rect, _waveTex, ScaleMode.StretchToFill, false);
                }
            }

            // Helper to map time -> x pixel taking reversal into account
            Func<float, float> TimeToX = (t) =>
            {
                float norm = Mathf.InverseLerp(0f, ClipLength, t);
                if (_isReversed)
                    return Mathf.Lerp(rect.xMax, rect.xMin, norm); // reversed mapping
                else
                    return Mathf.Lerp(rect.xMin, rect.xMax, norm);
            };

            // compute selection rect using TimeToX
            float sx = TimeToX(TrimStart);
            float ex = TimeToX(TrimEnd);
            float selXMin = Mathf.Min(sx, ex);
            float selXMax = Mathf.Max(sx, ex);
            Rect selRect = new Rect(selXMin, rect.yMin, Mathf.Max(0.0001f, selXMax - selXMin), rect.height);

            // darken outside selection (works regardless of reversed)
            Color overlay = new Color(0f, 0f, 0f, 0.58f);
            if (selRect.xMin > rect.xMin) EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, selRect.xMin - rect.xMin, rect.height), overlay);
            if (selRect.xMax < rect.xMax) EditorGUI.DrawRect(new Rect(selRect.xMax, rect.yMin, rect.xMax - selRect.xMax, rect.height), overlay);

            // selection highlight
            if (selRect.width > 2f)
                EditorGUI.DrawRect(selRect, new Color(0.2f, 0.55f, 1f, 0.14f));

            // draw fade lines and fade handles (based on _currentFadeIn/_currentFadeOut in seconds)
            DrawFadeLines(rect, selRect, _currentFadeIn, _currentFadeOut);

            // loop region painting if active
            if (IsLooping)
            {
                float l1x = TimeToX(LoopStart);
                float l2x = TimeToX(LoopEnd);
                Rect lr = new Rect(Mathf.Min(l1x, l2x), rect.yMin, Mathf.Abs(l2x - l1x), rect.height);
                EditorGUI.DrawRect(lr, new Color(0.15f, 0.5f, 1f, 0.18f));
            }

            // playhead
            float phX = TimeToX(PlayheadTime);
            EditorGUI.DrawRect(new Rect(phX - 0.75f, rect.yMin, 1.5f, rect.height), Color.red);

            Handles.color = Color.red;
            if (!_isReversed)
            {
                Vector3[] tri = { new Vector3(phX, rect.yMin - 6f, 0), new Vector3(phX - 6f, rect.yMin - 0.5f, 0), new Vector3(phX + 6f, rect.yMin - 0.5f, 0) };
                Handles.DrawAAConvexPolygon(tri);
            }
            else
            {
                Vector3[] triDown = { new Vector3(phX, rect.yMax + 6f, 0), new Vector3(phX - 6f, rect.yMax + 0.5f, 0), new Vector3(phX + 6f, rect.yMax + 0.5f, 0) };
                Handles.DrawAAConvexPolygon(triDown);
            }
            Handles.color = Color.white;

            // time label
            string label = $"{PlayheadTime:F3}s / {ClipLength:F3}s";
            Vector2 sSz = GUI.skin.label.CalcSize(new GUIContent(label));
            float labelX = Mathf.Min(rect.xMax - sSz.x - 10, phX + 8);
            float labelY = !_isReversed ? rect.yMin + 6 : rect.yMax - sSz.y - 6;
            Rect lblRect = new Rect(labelX, labelY, sSz.x + 8, sSz.y + 4);
            EditorGUI.DrawRect(lblRect, new Color(0f, 0f, 0f, 0.6f));
            GUI.Label(lblRect, label);

            // draw trim handles (white)
            DrawTrimHandles(rect, selRect);

            // input handling (pass selRect)
            HandleInput(rect, clip, selRect);
            
            if (Event.current.isMouse && rect.Contains(Event.current.mousePosition)) 
            {
                // Nếu sự kiện là MouseDown hoặc MouseDrag trong vùng này
                if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
                {
                    // Lệnh này nói với Unity: "Tôi đã xử lý rồi, đừng can thiệp nữa"
                    Event.current.Use(); 
                }
            }
        }

        // Draw small white trim handles
        private void DrawTrimHandles(Rect rect, Rect selRect)
        {
            float w = 12f, h = 20f;
            Rect left = new Rect(selRect.xMin - w * 0.5f, rect.center.y - h * 0.5f, w, h);
            Rect right = new Rect(selRect.xMax - w * 0.5f, rect.center.y - h * 0.5f, w, h);
            EditorGUI.DrawRect(left, Color.white);
            EditorGUI.DrawRect(new Rect(left.x + 2, left.y + 2, left.width - 4, left.height - 4), new Color(0.95f, 0.95f, 0.95f));
            EditorGUI.DrawRect(right, Color.white);
            EditorGUI.DrawRect(new Rect(right.x + 2, right.y + 2, right.width - 4, right.height - 4), new Color(0.95f, 0.95f, 0.95f));
        }

        // Draw fade lines + fade handles
        private void DrawFadeLines(Rect rect, Rect selRect, float fadeInSec, float fadeOutSec)
        {
            float selLenSec = Mathf.Max(0.0001f, TrimEnd - TrimStart);
            if (selLenSec <= 0f || selRect.width <= 2f) return;

            // compute fade pixel widths from seconds
            float rawFadeInPx = (fadeInSec / selLenSec) * selRect.width;
            float rawFadeOutPx = (fadeOutSec / selLenSec) * selRect.width;

            // other fade px based on current stored values to avoid overlap
            float otherOutPx = Mathf.Clamp((_currentFadeOut / selLenSec) * selRect.width, 0f, selRect.width);
            float otherInPx = Mathf.Clamp((_currentFadeIn / selLenSec) * selRect.width, 0f, selRect.width);

            // Allowed ranges: fade cannot exceed whole selection AND cannot overlap the other fade
            float fadeInPx = Mathf.Clamp(rawFadeInPx, 0f, Mathf.Max(0f, selRect.width - otherOutPx));
            float fadeOutPx = Mathf.Clamp(rawFadeOutPx, 0f, Mathf.Max(0f, selRect.width - otherInPx));

            if (fadeInPx >= MIN_FADE_PX)
            {
                Vector3 a = new Vector3(selRect.xMin, selRect.yMax, 0f); // bottom-left
                Vector3 b = new Vector3(selRect.xMin + fadeInPx, selRect.yMin, 0f); // top in
                Handles.color = _fadeColor;
                Handles.DrawAAPolyLine(2.8f, a, b);
                Handles.color = new Color(1f, 1f, 1f, 0.08f);
                Handles.DrawAAPolyLine(1f, a, b);
                Handles.color = Color.white;

                // fade-in handle (small rectangle above selection)
                float hw = 10f, hh = 10f;
                Rect fin = new Rect(selRect.xMin + fadeInPx - hw * 0.5f, selRect.yMin - hh - 4f, hw, hh);
                EditorGUI.DrawRect(fin, _fadeColor);
            }

            if (fadeOutPx >= MIN_FADE_PX)
            {
                Vector3 c = new Vector3(selRect.xMax, selRect.yMax, 0f); // bottom-right
                Vector3 d = new Vector3(selRect.xMax - fadeOutPx, selRect.yMin, 0f); // top in
                Handles.color = _fadeColor;
                Handles.DrawAAPolyLine(2.8f, c, d);
                Handles.color = new Color(1f, 1f, 1f, 0.08f);
                Handles.DrawAAPolyLine(1f, c, d);
                Handles.color = Color.white;

                // fade-out handle
                float hw = 10f, hh = 10f;
                Rect fout = new Rect(selRect.xMax - fadeOutPx - hw * 0.5f, selRect.yMin - hh - 4f, hw, hh);
                EditorGUI.DrawRect(fout, _fadeColor);
            }
        }

        // Input / interaction: scrubbing, trim handles, fade handles, loop selection, zoom, pan
        private void HandleInput(Rect rect, AudioClip clip, Rect selRect)
        {
            Event e = Event.current;
            if (e == null) return;

            // zoom with wheel
            if (e.type == EventType.ScrollWheel && rect.Contains(e.mousePosition))
            {
                float delta = -e.delta.y;
                OnRequestZoom?.Invoke(delta, e.mousePosition.x, rect);
                e.Use();
                return;
            }

            // middle drag pan
            if (e.button == 2)
            {
                if (e.type == EventType.MouseDrag && rect.Contains(e.mousePosition))
                {
                    OnRequestPan?.Invoke(e.delta.x);
                    e.Use();
                }
            }

            // precompute handle rects and fade handle rects using current fade values
            float selW = Mathf.Max(1f, selRect.width);
            float selSec = Mathf.Max(0.0001f, TrimEnd - TrimStart);

            float fadeInPx = Mathf.Clamp((_currentFadeIn / selSec) * selW, 0f, selW);
            float fadeOutPx = Mathf.Clamp((_currentFadeOut / selSec) * selW, 0f, selW);

            float trimHandleW = 12f, trimHandleH = 20f;
            Rect leftTrimRect = new Rect(selRect.xMin - trimHandleW * 0.5f, rect.center.y - trimHandleH * 0.5f, trimHandleW, trimHandleH);
            Rect rightTrimRect = new Rect(selRect.xMax - trimHandleW * 0.5f, rect.center.y - trimHandleH * 0.5f, trimHandleW, trimHandleH);

            float fhW = 10f, fhH = 10f;
            Rect fadeInHandleRect = new Rect(selRect.xMin + fadeInPx - fhW * 0.5f, selRect.yMin - fhH - 4f, fhW, fhH);
            Rect fadeOutHandleRect = new Rect(selRect.xMax - fadeOutPx - fhW * 0.5f, selRect.yMin - fhH - 4f, fhW, fhH);

            // --- Fade handle drag priority ---
            if (e.type == EventType.MouseDown && e.button == 0 && fadeInHandleRect.Contains(e.mousePosition))
            {
                _draggingFadeInHandle = true;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && _draggingFadeInHandle)
            {
                // allow dragging and PUSH other fade if necessary
                float otherOutPx = Mathf.Clamp((_currentFadeOut / selSec) * selW, 0f, selW);

                // candidate px (0..selW) based on mouse x relative to selection
                float localX = Mathf.Clamp(e.mousePosition.x - selRect.xMin, 0f, selW);

                // candidate seconds
                float candidateInSec = (localX / selW) * selSec;

                // if candidate overlaps other fade, push other fade inward
                if (candidateInSec + _currentFadeOut > selSec)
                {
                    float newOut = Mathf.Max(0f, selSec - candidateInSec);
                    _currentFadeOut = newOut;
                }

                // final clamp ensure no overlap
                float newFadeInSec = Mathf.Clamp(candidateInSec, 0f, Mathf.Max(0f, selSec - _currentFadeOut));
                _currentFadeIn = newFadeInSec;

                OnFadeChanged?.Invoke(_currentFadeIn, _currentFadeOut);
                e.Use();
            }
            else if (e.type == EventType.MouseUp && _draggingFadeInHandle)
            {
                _draggingFadeInHandle = false;
                e.Use();
            }

            if (e.type == EventType.MouseDown && e.button == 0 && fadeOutHandleRect.Contains(e.mousePosition))
            {
                _draggingFadeOutHandle = true;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && _draggingFadeOutHandle)
            {
                // allow dragging and PUSH other fade if necessary
                float localFromRight = Mathf.Clamp(selRect.xMax - e.mousePosition.x, 0f, selW);
                float candidateOutSec = (localFromRight / selW) * selSec;

                if (candidateOutSec + _currentFadeIn > selSec)
                {
                    float newIn = Mathf.Max(0f, selSec - candidateOutSec);
                    _currentFadeIn = newIn;
                }

                float newFadeOutSec = Mathf.Clamp(candidateOutSec, 0f, Mathf.Max(0f, selSec - _currentFadeIn));
                _currentFadeOut = newFadeOutSec;

                OnFadeChanged?.Invoke(_currentFadeIn, _currentFadeOut);
                e.Use();
            }
            else if (e.type == EventType.MouseUp && _draggingFadeOutHandle)
            {
                _draggingFadeOutHandle = false;
                e.Use();
            }

            // --- Trim handles (priority after fade handles) ---
            if (e.type == EventType.MouseDown && e.button == 0 && leftTrimRect.Contains(e.mousePosition))
            {
                _draggingLeftHandle = true;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && _draggingLeftHandle)
            {
                float t = PixelToTimeWithReverse(rect, e.mousePosition.x);
                t = Mathf.Clamp(t, 0f, TrimEnd - 0.001f);
                TrimStart = t;
                OnTrimChanged?.Invoke(TrimStart, TrimEnd);
                PlayheadTime = TrimStart;
                e.Use();
            }
            else if (e.type == EventType.MouseUp && _draggingLeftHandle)
            {
                _draggingLeftHandle = false;
                OnTrimChanged?.Invoke(TrimStart, TrimEnd);
                e.Use();
            }

            if (e.type == EventType.MouseDown && e.button == 0 && rightTrimRect.Contains(e.mousePosition))
            {
                _draggingRightHandle = true;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && _draggingRightHandle)
            {
                float t = PixelToTimeWithReverse(rect, e.mousePosition.x);
                t = Mathf.Clamp(t, TrimStart + 0.001f, ClipLength);
                TrimEnd = t;
                OnTrimChanged?.Invoke(TrimStart, TrimEnd);
                PlayheadTime = TrimEnd;
                e.Use();
            }
            else if (e.type == EventType.MouseUp && _draggingRightHandle)
            {
                _draggingRightHandle = false;
                OnTrimChanged?.Invoke(TrimStart, TrimEnd);
                e.Use();
            }

            // --- If not dragging handles, use left-drag as scrub ---
            if (!_draggingLeftHandle && !_draggingRightHandle && !_draggingFadeInHandle && !_draggingFadeOutHandle)
            {
                if (e.button == 0)
                {
                    if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
                    {
                        _draggingPlayhead = true;
                        float t = PixelToTimeWithReverse(rect, e.mousePosition.x);
                        PlayheadTime = t;
                        OnRequestScrub?.Invoke(t);
                        e.Use();
                    }
                    else if (e.type == EventType.MouseDrag && _draggingPlayhead)
                    {
                        float t = PixelToTimeWithReverse(rect, e.mousePosition.x);
                        PlayheadTime = t;
                        OnRequestScrub?.Invoke(t);
                        e.Use();
                    }
                    else if (e.type == EventType.MouseUp && _draggingPlayhead)
                    {
                        _draggingPlayhead = false;
                        OnScrubEnd?.Invoke();
                        e.Use();
                    }
                }

                // right-drag -> loop selection
                if (e.button == 1)
                {
                    if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
                    {
                        _rightDragSelecting = true;
                        _rightDragStartAbs = PixelToTimeWithReverse(rect, e.mousePosition.x);
                        e.Use();
                    }
                    else if (e.type == EventType.MouseDrag && _rightDragSelecting)
                    {
                        float cur = PixelToTimeWithReverse(rect, e.mousePosition.x);
                        LoopStart = _rightDragStartAbs;
                        LoopEnd = cur;
                        IsLooping = true;
                        e.Use();
                    }
                    else if (e.type == EventType.MouseUp && _rightDragSelecting)
                    {
                        _rightDragSelecting = false;
                        float cur = PixelToTimeWithReverse(rect, e.mousePosition.x);
                        LoopStart = Mathf.Min(_rightDragStartAbs, cur);
                        LoopEnd = Mathf.Max(_rightDragStartAbs, cur);
                        IsLooping = true;
                        OnRequestLoopPlay?.Invoke(LoopStart, LoopEnd);
                        e.Use();
                    }
                }
            }

            // double click clears loop
            if (e.type == EventType.MouseDown && e.clickCount == 2 && rect.Contains(e.mousePosition))
            {
                IsLooping = false;
                e.Use();
            }

            // keyboard shortcuts inside rect
            if (e.type == EventType.KeyDown && rect.Contains(e.mousePosition))
            {
                if (e.keyCode == KeyCode.Space)
                {
                    OnRequestSetPlayhead?.Invoke(PlayheadTime);
                    e.Use();
                }
                if (e.keyCode == KeyCode.L)
                {
                    IsLooping = !IsLooping;
                    e.Use();
                }
            }
        }

        // Waveform generation (throttled)
        private void RequestRebuildIfNeeded(Rect rect, AudioClip clip)
        {
            int width = Mathf.Min(_texWidthCap, Mathf.Max(1, (int)rect.width));
            int height = Mathf.Max(32, (int)rect.height);
            double now = EditorApplication.timeSinceStartup;

            if (_waveTex == null || clip != _lastClip || width != _lastW || height != _lastH)
            {
                if (now - _lastRegen < _regenInterval)
                {
                    EditorApplication.delayCall += () => { RebuildWaveform(clip, width, height); };
                }
                else
                {
                    RebuildWaveform(clip, width, height);
                }
                _lastRegen = now;
            }
        }

        private void RebuildWaveform(AudioClip clip, int width, int height)
        {
            try
            {
                _lastClip = clip;
                _lastW = width;
                _lastH = height;
                var tex = GenerateWaveformTextureDownsampled(clip, width, height);
                if (_waveTex != null) Object.DestroyImmediate(_waveTex);
                _waveTex = tex;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Waveform rebuild failed: " + ex.Message);
            }
        }

        private Texture2D GenerateWaveformTextureDownsampled(AudioClip clip, int width, int height)
        {
            if (clip == null) return null;
            if (!clip.LoadAudioData()) return null;

            int channels = clip.channels;
            int samples = clip.samples;
            int step = Mathf.Max(1, samples / width);

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            Color bg = EditorGUIUtility.isProSkin ? new Color(0.09f, 0.09f, 0.1f) : new Color(0.92f, 0.92f, 0.93f);
            Color wave = _waveColor;

            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = bg;
            int halfH = height / 2;

            float[] buf = new float[step * channels];
            float maxAmp = 1e-5f;

            int quickStep = Mathf.Max(1, samples / Mathf.Min(width * 4, 4096));
            float[] quickBuf = new float[channels];
            for (int s = 0; s < samples; s += quickStep)
            {
                int p = Mathf.Min(s, samples - 1);
                clip.GetData(quickBuf, p);
                float sum = 0f;
                for (int c = 0; c < channels; c++) sum += quickBuf[Mathf.Min(c, quickBuf.Length - 1)];
                float avg = sum / channels;
                maxAmp = Mathf.Max(maxAmp, Mathf.Abs(avg));
            }
            if (maxAmp <= 0f) maxAmp = 1f;

            for (int x = 0; x < width; x++)
            {
                int sIdx = Mathf.Min(samples - 1, x * step);
                int readCount = Mathf.Min(step, samples - sIdx);
                clip.GetData(buf, sIdx);

                float min = 1f, maxv = -1f;
                for (int i = 0; i < readCount; i++)
                {
                    float sum = 0f;
                    int baseIdx = i * channels;
                    for (int c = 0; c < channels; c++)
                    {
                        int idx = baseIdx + c;
                        if (idx < buf.Length) sum += buf[idx];
                    }
                    float v = sum / channels;
                    min = Mathf.Min(min, v);
                    maxv = Mathf.Max(maxv, v);
                }

                int yMin = Mathf.Clamp(Mathf.FloorToInt((min / maxAmp + 1f) * halfH), 0, height - 1);
                int yMax = Mathf.Clamp(Mathf.CeilToInt((maxv / maxAmp + 1f) * halfH), 0, height - 1);

                for (int y = yMin; y <= yMax; y++)
                {
                    pix[y * width + x] = wave;
                }
            }

            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }

        private void DrawRuler(Rect rect, AudioClip clip)
        {
            Rect ruler = new Rect(rect.xMin, rect.yMin - 18, rect.width, 18);
            EditorGUI.DrawRect(ruler, new Color(0.06f, 0.06f, 0.06f));

            float total = ClipLength;
            if (total <= 0f) return;

            float approxPxPerSec = rect.width / total;
            float[] steps = { 5f, 2f, 1f, 0.5f, 0.25f, 0.1f, 0.05f, 0.02f, 0.01f };
            float step = 1f;
            foreach (var s in steps)
            {
                if (s * approxPxPerSec >= 80f) { step = s; break; }
            }

            for (float t = 0f; t <= total; t += step)
            {
                float nx = Mathf.InverseLerp(0f, total, t);
                float x = Mathf.Lerp(rect.xMin, rect.xMax, nx);
                Handles.color = new Color(0.6f, 0.6f, 0.6f);
                Handles.DrawLine(new Vector3(x, ruler.yMax - 2), new Vector3(x, ruler.yMax + 2));
                Handles.color = Color.white;
                GUI.Label(new Rect(x + 3, ruler.yMin + 1, 80, 16), $"{t:F2}s");
            }
        }

        // Pixel -> Time mapping that respects reverse state
        private float PixelToTimeWithReverse(Rect rect, float px)
        {
            float norm = Mathf.InverseLerp(rect.xMin, rect.xMax, px);
            if (_isReversed)
            {
                // invert norm for reversed mapping
                return Mathf.Lerp(0f, ClipLength, 1f - norm);
            }
            else
            {
                return Mathf.Lerp(0f, ClipLength, norm);
            }
        }

        private float PixelToTimeWithReverseClamped(Rect rect, float px)
        {
            return Mathf.Clamp(PixelToTimeWithReverse(rect, px), 0f, ClipLength);
        }

        private float PixelToNormalized(Rect rect, float px)
        {
            return Mathf.InverseLerp(rect.xMin, rect.xMax, px);
        }

        public void ResetView()
        {
            // reserved for zoom/pan state if you extend
        }

        public void Dispose()
        {
            if (_waveTex != null) Object.DestroyImmediate(_waveTex);
            _waveTex = null;
        }
    }
}

#endif