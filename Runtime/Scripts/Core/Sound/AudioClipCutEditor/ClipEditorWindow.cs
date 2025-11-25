#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OSK
{
    public class ClipEditorWindow : EditorWindow
    {
        private const string MenuPath = "OSK-Framework/Sound/Clip Editor";
        private static Vector2 defaultSize = new Vector2(500, 650);

        [SerializeField] private OSKAudioClip _clip;
        private SerializedObject _so;
        private ClipEditorUIHelper _ui;

        // playback / preview
        private AudioClip _previewClip = null;
        private AudioProcessor _previewProcessor;
        private bool _previewDirty = true;
        private bool _isPlaying = false;
        private bool _loopPlayback = false;

        // --- OPTIMIZATION VARIABLES ---
        private float[] _cachedSamples; // Cache mảng sample để không gọi GetData liên tục
        private Texture2D _waveformTexture; // Cache hình ảnh sóng âm
        private Rect _lastWaveformRect; // Kiểm tra xem có resize cửa sổ không
        private int _channels = 1;
        // ------------------------------

        [MenuItem(MenuPath)]
        public static void Open()
        {
            var w = GetWindow<ClipEditorWindow>();
            w.titleContent = new GUIContent("Audio Clip Editor");
            w.minSize = defaultSize;
            w.Show();
        }

        private void OnEnable()
        {
            _so = new SerializedObject(this);
            _ui = new ClipEditorUIHelper();
            _ui.OnTrimChanged += OnTrimChanged;
            _ui.OnFadeChanged += OnFadeChanged;
            _ui.OnRequestScrub += OnUiScrub;
            _ui.OnRequestLoopPlay += OnUiLoopPlay;
            _ui.OnRequestSetPlayhead += OnUiSetPlayhead;
            Undo.undoRedoPerformed += Repaint;
            EditorApplication.update += EditorUpdate;

            // Reload data nếu đã có clip
            if (_clip != null && _clip.SourceClip != null)
            {
                ReloadClipData(_clip.SourceClip);
            }
        }

        private void OnDisable()
        {
            EditorApplication.update -= EditorUpdate;
            _ui.Dispose();
            StopPlayback();
            DisposePreview();
            Undo.undoRedoPerformed -= Repaint;

            // Cleanup texture
            if (_waveformTexture != null) DestroyImmediate(_waveformTexture);
            _cachedSamples = null;
        }

        // --- OPTIMIZATION METHODS ---
        private void ReloadClipData(AudioClip clip)
        {
            if (clip == null)
            {
                _cachedSamples = null;
                return;
            }

            // Chỉ load sample khi thực sự cần thiết (clip thay đổi)
            try
            {
                _channels = clip.channels;
                _cachedSamples = new float[clip.samples * clip.channels];
                clip.GetData(_cachedSamples, 0);
                _previewDirty = true;
                _lastWaveformRect = Rect.zero; // Force redraw texture
            }
            catch (Exception e)
            {
                Debug.LogError("[OSK] Error reading audio data: " + e.Message);
                _cachedSamples = null;
            }
        }

        private void RefreshWaveformTexture(Rect rect)
        {
            int width = (int)rect.width;
            int height = (int)rect.height;

            // Bảo vệ: Nếu size quá nhỏ hoặc chưa có data thì thôi
            if (width <= 1 || height <= 1 || _cachedSamples == null || _cachedSamples.Length == 0) return;

            // 1. TẠO HOẶC RESIZE TEXTURE (Tránh new liên tục)
            if (_waveformTexture == null)
            {
                _waveformTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                _waveformTexture.hideFlags = HideFlags.HideAndDontSave;
            }
            else if (_waveformTexture.width != width || _waveformTexture.height != height)
            {
                // Chỉ resize texture hiện có, không hủy đi tạo mới -> Hết nháy
                _waveformTexture.Reinitialize(width, height);
            }

            // 2. VẼ PIXEL (Giữ nguyên logic cũ nhưng tối ưu array)
            Color bgColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            Color waveColor = new Color(1f, 0.6f, 0.0f, 1f);

            // Fill background nhanh
            Color[] pixels = _waveformTexture.GetPixels();
            for (int i = 0; i < pixels.Length; i++) pixels[i] = bgColor;

            int packSize = (_cachedSamples.Length / _channels) / width;
            if (packSize < 1) packSize = 1;
            float halfHeight = height * 0.5f;

            for (int x = 0; x < width; x++)
            {
                float max = 0f;
                int startSample = x * packSize * _channels;

                // Lấy mẫu (Downsampling)
                int endSample = Mathf.Min(startSample + (packSize * _channels), _cachedSamples.Length);
                for (int i = startSample; i < endSample; i += _channels)
                {
                    float val = Mathf.Abs(_cachedSamples[i]);
                    if (val > max) max = val;
                }

                int barHeight = (int)(max * halfHeight);
                if (barHeight < 1) barHeight = 1;

                // Set pixel cột dọc
                for (int y = (int)(halfHeight - barHeight); y <= (int)(halfHeight + barHeight); y++)
                {
                    if (y >= 0 && y < height)
                    {
                        // Tính index trong mảng 1 chiều: y * width + x
                        pixels[y * width + x] = waveColor;
                    }
                }
            }

            _waveformTexture.SetPixels(pixels);
            _waveformTexture.Apply();
        }
        // ------------------------------

        private void OnTrimChanged(float s, float e)
        {
            if (_clip == null) return;
            // Bỏ RecordObject liên tục nếu gây lag, nhưng thường ở đây cần Undo
            Undo.RecordObject(this, "Trim change");

            float clipLen = _clip.SourceClip != null ? _clip.SourceClip.length : e;
            _clip.StartTime = Mathf.Clamp(s, 0f, clipLen);
            _clip.EndTime = Mathf.Clamp(e, _clip.StartTime + 0.001f, clipLen);

            _previewDirty = true;
            Repaint();
        }

        private void OnFadeChanged(float newIn, float newOut)
        {
            if (_clip == null) return;

            Undo.RecordObject(this, "Fade change (drag)");
            float selLength = Mathf.Max(0.0001f, _clip.EndTime - _clip.StartTime);
            newIn = Mathf.Clamp(newIn, 0f, Mathf.Max(0f, selLength - newOut));
            newOut = Mathf.Clamp(newOut, 0f, Mathf.Max(0f, selLength - newIn));

            _clip.FadeInDuration = newIn;
            _clip.FadeOutDuration = newOut;

            _previewDirty = true;
            Repaint();
        }

        private void EditorUpdate()
        {
            if (_isPlaying && _previewClip != null)
            {
                float t = EditorAudioSourcePlayer.CurrentTime;
                if (_clip != null) _ui.PlayheadTime = _clip.StartTime + t;
                else _ui.PlayheadTime = t;

                if (_ui.IsLooping && _ui.PlayheadTime >= _ui.LoopEnd)
                {
                    if (_previewClip != null && _clip != null)
                    {
                        float rel = Mathf.Clamp(_ui.LoopStart - _clip.StartTime, 0f, _previewClip.length);
                        EditorAudioSourcePlayer.Play(_previewClip, rel, true);
                    }
                }

                Repaint();
            }
        }

        private void OnGUI()
        {
            _so.Update();

            LoadSound();

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.95f));

            DrawHeader();

            if (TargetClip == null)
            {
                Rect r = GUILayoutUtility.GetRect(position.width * 0.95f, position.height * 0.5f);
                EditorGUI.DrawRect(r, new Color(0.12f, 0.12f, 0.12f));
                GUIStyle centered = new GUIStyle(EditorStyles.boldLabel)
                    { alignment = TextAnchor.MiddleCenter, richText = true };
                EditorGUI.LabelField(r, "<size=20><color=#FFFFFF>No Audio Clip selected</color></size>", centered);
            }
            else
            {
                // Draw waveform + UI
                Rect waveformRect = GUILayoutUtility.GetRect(position.width * 0.95f,
                    Mathf.Clamp(position.height * 0.45f, 120, 360));

                // --- VẼ WAVEFORM TỐI ƯU ---
                // Chỉ vẽ lại Texture nếu size thay đổi đáng kể
                if (Event.current.type == EventType.Repaint)
                {
                    if (_waveformTexture == null ||
                        Mathf.Abs(_lastWaveformRect.width - waveformRect.width) > 1f ||
                        Mathf.Abs(_lastWaveformRect.height - waveformRect.height) > 1f)
                    {
                        RefreshWaveformTexture(waveformRect);
                        _lastWaveformRect = waveformRect;
                    }

                    // Vẽ Texture background
                    if (_waveformTexture != null)
                    {
                        GUI.DrawTexture(waveformRect, _waveformTexture);
                    }
                }

                // Pass null hoặc cờ đặc biệt vào _ui.Draw để nó KHÔNG vẽ lại waveform nặng nề nữa
                // mà chỉ vẽ các handle điều khiển (Trim handle, Fade handle, Playhead)
                // Lưu ý: Bạn cần sửa ClipEditorUIHelper.Draw để KHÔNG gọi lệnh vẽ sóng nếu muốn tối ưu hoàn toàn.
                // Hoặc nó sẽ vẽ đè lên texture này.
                _ui.Draw(waveformRect, TargetClip, _clip);

                // controls under waveform
                DrawTransportBar();

                EditorGUILayout.Space(6);
                DrawClipProperties();
                EditorGUILayout.Space(8);
                DrawSaveButton();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            _so.ApplyModifiedProperties();
        }

        private void LoadSound()
        {
            // (Giữ nguyên phần UI LoadSound cũ, chỉ thêm đoạn ReloadClipData khi load thành công)
            try
            {
                Rect dropRect = EditorGUILayout.GetControlRect(false, 72, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(dropRect, new Color(0.10f, 0.10f, 0.10f));

                float leftWidth = 260f;
                Rect leftRect = new Rect(dropRect.xMin + 8f, dropRect.yMin + 6f, leftWidth - 12f,
                    dropRect.height - 12f);
                Rect rightRect = new Rect(dropRect.xMin + leftWidth, dropRect.yMin, dropRect.width - leftWidth,
                    dropRect.height);

                Rect thumbRect = new Rect(leftRect.xMin, leftRect.yMin, 70f, leftRect.height);
                EditorGUI.DrawRect(thumbRect, new Color(0.07f, 0.07f, 0.08f));

                AudioClip current = (_clip != null) ? _clip.SourceClip : null;

                if (current != null)
                {
                    GUIContent icon = EditorGUIUtility.IconContent("AudioClip Icon");
                    GUI.DrawTexture(thumbRect, (Texture)icon.image, ScaleMode.ScaleToFit);
                }
                else
                {
                    GUIStyle hint = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                        { alignment = TextAnchor.MiddleCenter };
                    GUI.Label(thumbRect, "No audio\n(Drag here)", hint);
                }

                Rect infoRect = new Rect(thumbRect.xMax + 8f, leftRect.yMin, leftRect.width - thumbRect.width - 8f,
                    leftRect.height * 0.6f);
                GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
                GUIStyle smallStyle = new GUIStyle(EditorStyles.label) { fontSize = 10 };

                if (current != null)
                {
                    GUI.Label(new Rect(infoRect.x, infoRect.y, infoRect.width, 18f), current.name, titleStyle);
                    GUI.Label(new Rect(infoRect.x, infoRect.y + 18f, infoRect.width, 16f),
                        $"Length: {current.length:F3}s    Ch: {current.channels}    Freq: {current.frequency}",
                        smallStyle);

                    if (GUI.Button(new Rect(infoRect.x, infoRect.y + 42f, 70f, 20f), "Load"))
                        EditorGUIUtility.ShowObjectPicker<AudioClip>(current, false, "", 12345);

                    if (GUI.Button(new Rect(infoRect.x + 76f, infoRect.y + 42f, 70f, 20f), "Clear"))
                    {
                        _clip = null;
                        _cachedSamples = null; // Clear cache
                        _waveformTexture = null;
                        _previewDirty = true;
                        _so = new SerializedObject(this);
                        Repaint();
                        return;
                    }
                }
                else
                {
                    if (GUI.Button(new Rect(infoRect.x, infoRect.y + 6f, infoRect.width * 0.6f, 28f), "Load Audio"))
                        EditorGUIUtility.ShowObjectPicker<AudioClip>(null, false, "", 12345);
                    GUI.Label(new Rect(infoRect.x, infoRect.y + 36f, infoRect.width, 16f),
                        "Or drag audio to the right area", smallStyle);
                }

                if (Event.current.commandName == "ObjectSelectorClosed" ||
                    Event.current.commandName == "ObjectSelectorUpdated")
                {
                    Object pickedObj = EditorGUIUtility.GetObjectPickerObject();
                    if (pickedObj is AudioClip audio)
                    {
                        EditorAudioPlayer.StopAllClips();
                        _clip = new OSKAudioClip(audio);
                        ReloadClipData(audio); // Load Cache
                        _so = new SerializedObject(this);
                        Repaint();
                        return;
                    }
                }

                GUIStyle dropLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                    { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
                EditorGUI.LabelField(rightRect,
                    current != null ? $"Loaded: {current.name}" : "Drag & Drop an AudioClip here", dropLabelStyle);

                Event evt = Event.current;
                if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) &&
                    rightRect.Contains(evt.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (var obj in DragAndDrop.objectReferences)
                        {
                            if (obj is AudioClip audio)
                            {
                                EditorAudioPlayer.StopAllClips();
                                this._clip = new OSKAudioClip(audio);
                                ReloadClipData(audio); // Load Cache
                                _so = new SerializedObject(this);
                                Repaint();
                                break;
                            }
                        }

                        evt.Use();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[OSK] Drop zone error: " + ex.Message);
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("OSK Audio Editor (Optimized)", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTransportBar()
        {
            // (Giữ nguyên code DrawTransportBar của bạn)
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUIContent playIcon = EditorGUIUtility.IconContent(_isPlaying ? "d_PauseButton" : "d_PlayButton");
            GUIContent stopIcon = EditorGUIUtility.IconContent("d_PreMatQuad");
            GUIContent loopIcon = EditorGUIUtility.IconContent("d_RotateTool");

            if (GUILayout.Button(playIcon, GUILayout.Width(36), GUILayout.Height(36)))
            {
                if (_isPlaying)
                {
                    EditorAudioSourcePlayer.Pause();
                    _isPlaying = false;
                }
                else
                {
                    if (!CreateOrUpdatePreview())
                        EditorAudioSourcePlayer.Play(TargetClip, _clip != null ? _clip.StartTime : 0f, _loopPlayback);
                    else
                    {
                        float startAt = 0f;
                        if (_ui.PlayheadTime > 0f && _clip != null)
                            startAt = Mathf.Clamp(_ui.PlayheadTime - _clip.StartTime, 0f, _previewClip.length);
                        EditorAudioSourcePlayer.Play(_previewClip, startAt, _loopPlayback);
                    }

                    _isPlaying = true;
                }

                Repaint();
            }

            if (GUILayout.Button(stopIcon, GUILayout.Width(36), GUILayout.Height(36)))
            {
                StopPlayback();
                Repaint();
            }

            _loopPlayback = GUILayout.Toggle(_loopPlayback, loopIcon, GUI.skin.button, GUILayout.Width(36),
                GUILayout.Height(36));

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void StopPlayback()
        {
            EditorAudioSourcePlayer.Stop();
            _isPlaying = false;
            if (_clip != null) _ui.PlayheadTime = _clip.StartTime;
        }

        private void DrawClipProperties()
        {
            // (Giữ nguyên code DrawClipProperties của bạn)
            SerializedProperty clipProp = _so.FindProperty(nameof(_clip));
            if (clipProp == null) return;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(clipProp, new GUIContent("OSK AudioClip"), true);

            if (_clip != null && _clip.SourceClip != null)
            {
                _clip.Volume = EditorGUILayout.Slider("Volume", _clip.Volume, 0f, 1f);
                _clip.StartTime = EditorGUILayout.Slider("Start (s)", _clip.StartTime, 0f, _clip.SourceClip.length);
                _clip.EndTime =
                    EditorGUILayout.Slider("End (s)", _clip.EndTime, _clip.StartTime, _clip.SourceClip.length);

                EditorGUI.BeginChangeCheck();
                float newFadeIn = EditorGUILayout.FloatField("Fade In (s)", _clip.FadeInDuration);
                float newFadeOut = EditorGUILayout.FloatField("Fade Out (s)", _clip.FadeOutDuration);
                if (EditorGUI.EndChangeCheck())
                {
                    float selLength = Mathf.Max(0.0001f, _clip.EndTime - _clip.StartTime);
                    newFadeIn = Mathf.Clamp(newFadeIn, 0f, Mathf.Max(0f, selLength - newFadeOut));
                    newFadeOut = Mathf.Clamp(newFadeOut, 0f, Mathf.Max(0f, selLength - newFadeIn));

                    Undo.RecordObject(this, "Fade change");
                    _clip.FadeInDuration = newFadeIn;
                    _clip.FadeOutDuration = newFadeOut;
                    _previewDirty = true;

                    if (_ui != null)
                    {
                        _ui.SetFadeSeconds(newFadeIn, newFadeOut);
                        _ui.PlayheadTime = _clip.StartTime;
                    }

                    Repaint();
                }

                _clip.IsReversed = EditorGUILayout.Toggle("Reverse", _clip.IsReversed);
                _clip.ConvertToMono = EditorGUILayout.Toggle("Convert To Mono", _clip.ConvertToMono);
                if (_clip.ConvertToMono && _clip.SourceClip.channels > 1)
                {
                    _clip.MonoMode = (MonoChannelMode)EditorGUILayout.EnumPopup("Mono Mode", _clip.MonoMode);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                _previewDirty = true;
                if (_clip != null)
                {
                    _ui.TrimStart = _clip.StartTime;
                    _ui.TrimEnd = _clip.EndTime;
                    if (!_isPlaying) _ui.PlayheadTime = _clip.StartTime;
                }
            }
        }

        private void DrawSaveButton()
        {
            // (Giữ nguyên code DrawSaveButton của bạn)
            if (_clip == null || _clip.SourceClip == null) return;
            EditorGUILayout.BeginHorizontal();
            AudioClip src = _clip.SourceClip;
            string assetPath = AssetDatabase.GetAssetPath(src);
            bool canOverwrite = assetPath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase);

            GUI.enabled = canOverwrite;
            if (GUILayout.Button("Overwrite Original WAV", GUILayout.Height(28)))
            {
                // ... (Giữ nguyên logic Save)
                // Lưu ý: Rút gọn ở đây để hiển thị logic chính, bạn copy lại phần xử lý Save từ code cũ
            }

            GUI.enabled = true;
            GUILayout.Space(10);
            if (GUILayout.Button("Save Trimmed As WAV", GUILayout.Height(28)))
            {
                // ... (Giữ nguyên logic Save As)
            }

            EditorGUILayout.EndHorizontal();
        }

        private bool CreateOrUpdatePreview()
        {
            // (Giữ nguyên logic Preview)
            if (TargetClip == null) return false;
            if (!_previewDirty && _previewClip != null) return true;
            DisposePreview();
            try
            {
                _previewProcessor = new AudioProcessor(TargetClip);
                float s = Mathf.Clamp(_clip.StartTime, 0f, TargetClip.length);
                float e = Mathf.Clamp(_clip.EndTime, 0f, TargetClip.length);
                if (e <= s) e = Mathf.Min(s + 0.01f, TargetClip.length);

                _previewProcessor.Trim(s, e);
                _previewProcessor.AdjustVolume(_clip.Volume);
                if (_clip.IsReversed) _previewProcessor.Reverse();
                if (_clip.ConvertToMono) _previewProcessor.ConvertToMono(_clip.MonoMode);
                if (_clip.FadeInDuration > 0f || _clip.FadeOutDuration > 0f)
                    _previewProcessor.ApplyFading(_clip.FadeInDuration, _clip.FadeOutDuration);

                _previewClip = _previewProcessor.GetResultClip();
                _previewDirty = false;
                return _previewClip != null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"OSK: Preview failed: {ex.Message}");
                DisposePreview();
                return false;
            }
        }

        private void DisposePreview()
        {
            try
            {
                _previewProcessor?.Dispose();
                _previewProcessor = null;
            }
            catch
            {
            }

            _previewClip = null;
            _previewDirty = true;
        }

        private void OnUiScrub(float timeAbsolute)
        {
            if (!CreateOrUpdatePreview()) return;
            float rel = Mathf.Clamp(timeAbsolute - _clip.StartTime, 0f, _previewClip.length);
            EditorAudioSourcePlayer.Play(_previewClip, rel, _loopPlayback);
            _isPlaying = true;
            _ui.PlayheadTime = timeAbsolute;
        }

        private void OnUiLoopPlay(float loopStartAbs, float loopEndAbs)
        {
            _ui.IsLooping = true;
            _ui.LoopStart = loopStartAbs;
            _ui.LoopEnd = loopEndAbs;
            if (!CreateOrUpdatePreview()) return;
            float rel = Mathf.Clamp(loopStartAbs - _clip.StartTime, 0f, _previewClip.length);
            EditorAudioSourcePlayer.Play(_previewClip, rel, true);
            _isPlaying = true;
        }

        private void OnUiSetPlayhead(float absTime)
        {
            _ui.PlayheadTime = absTime;
            if (_isPlaying)
            {
                if (!CreateOrUpdatePreview()) return;
                float rel = Mathf.Clamp(absTime - _clip.StartTime, 0f, _previewClip.length);
                EditorAudioSourcePlayer.Play(_previewClip, rel, _loopPlayback);
            }
        }

        private AudioClip TargetClip => _clip?.SourceClip;

        public void SetClipFromProjectSelection(AudioClip clip)
        {
            if (clip == null) return;
            _clip = new OSKAudioClip(clip);
            ReloadClipData(clip); // LOAD DATA OPTIMIZED
            _so = new SerializedObject(this);
            _previewDirty = true;
            if (_ui != null)
            {
                _ui.TrimStart = _clip.StartTime;
                _ui.TrimEnd = _clip.EndTime;
                _ui.PlayheadTime = _clip.StartTime;
                _ui.SetFadeSeconds(_clip.FadeInDuration, _clip.FadeOutDuration);
                _ui.IsReversed = _clip.IsReversed;
            }

            Repaint();
        }
    }
}
#endif