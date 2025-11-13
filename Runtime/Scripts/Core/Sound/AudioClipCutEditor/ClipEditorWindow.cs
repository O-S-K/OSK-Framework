#if  UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OSK
{
    public class ClipEditorWindow : EditorWindow
    {
        private const string MenuPath = "OSK-Framework/Tools/OSK Audio Clip Editor";
        private static Vector2 defaultSize = new Vector2(500, 650);
       
        [SerializeField]
        private OSKAudioClip _clip;
        private SerializedObject _so;
        private ClipEditorUIHelper _ui;

        // playback / preview
        private AudioClip _previewClip = null;
        private AudioProcessor _previewProcessor;
        private bool _previewDirty = true;
        private bool _isPlaying = false;
        private bool _loopPlayback = false;

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
            _ui.OnTrimChanged += OnTrimChanged; // update _oskClip.StartTime/_oskClip.EndTime
            _ui.OnFadeChanged += OnFadeChanged; // update _oskClip.FadeInDuration/_oskClip.FadeOutDuration
            _ui.OnRequestScrub += OnUiScrub; // play scrub
            _ui.OnRequestLoopPlay += OnUiLoopPlay;
            _ui.OnRequestSetPlayhead += OnUiSetPlayhead;
            Undo.undoRedoPerformed += Repaint;
            EditorApplication.update += EditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= EditorUpdate;
            _ui.Dispose();
            StopPlayback();
            DisposePreview();
            Undo.undoRedoPerformed -= Repaint;
        }

        private void OnTrimChanged(float s, float e)
        {
            if (_clip == null) return;
            Undo.RecordObject(this, "Trim change");
            _clip.StartTime = Mathf.Clamp(s, 0f, _clip.SourceClip != null ? _clip.SourceClip.length : e);
            _clip.EndTime = Mathf.Clamp(e, _clip.StartTime + 0.001f,
                _clip.SourceClip != null ? _clip.SourceClip.length : e);
            _previewDirty = true;
            Repaint();
        }

        private void OnFadeChanged(float newIn, float newOut)
        {
            if (_clip == null) return;

            Undo.RecordObject(this, "Fade change (drag)");
            // clamp defensively with selection
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
            // update playhead time while playing using EditorAudioSourcePlayer
            if (_isPlaying && _previewClip != null)
            {
                float t = EditorAudioSourcePlayer.CurrentTime;
                // playhead time relative to original = _oskClip.StartTime + t
                if (_clip != null) _ui.PlayheadTime = _clip.StartTime + t;
                else _ui.PlayheadTime = t;

                // loop region handling (UI does visual, here we enforce)
                if (_ui.IsLooping && _ui.PlayheadTime >= _ui.LoopEnd)
                {
                    // restart inside loop
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
                // ensure preview clip created only when needed (Play or Scrub) - but we draw waveform from original clip
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
            try
            {
                // 전체 rect
                Rect dropRect = EditorGUILayout.GetControlRect(false, 72, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(dropRect, new Color(0.10f, 0.10f, 0.10f));

                // left box width (thumbnail + info + buttons)
                float leftWidth = 260f;
                Rect leftRect = new Rect(dropRect.xMin + 8f, dropRect.yMin + 6f, leftWidth - 12f,
                    dropRect.height - 12f);
                Rect rightRect = new Rect(dropRect.xMin + leftWidth, dropRect.yMin, dropRect.width - leftWidth,
                    dropRect.height);

                // --- LEFT: waveform thumbnail + info + buttons (BroAudio-like) ---
                // draw thumbnail background
                Rect thumbRect = new Rect(leftRect.xMin, leftRect.yMin, 70f, leftRect.height);
                EditorGUI.DrawRect(thumbRect, new Color(0.07f, 0.07f, 0.08f));

                AudioClip current = (_clip != null) ? _clip.SourceClip : null;

                // waveform preview (if clip available)
                if (current != null)
                {
                    // fallback - show audio icon
                    GUIContent icon = EditorGUIUtility.IconContent("AudioClip Icon");
                    GUI.DrawTexture(thumbRect, (Texture)icon.image, ScaleMode.ScaleToFit);
                }
                else
                {
                    GUIStyle hint = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                        { alignment = TextAnchor.MiddleCenter };
                    GUI.Label(thumbRect, "No audio\n(Drag here)", hint);
                }

                // right of thumbnail: text info + buttons
                Rect infoRect = new Rect(thumbRect.xMax + 8f, leftRect.yMin, leftRect.width - thumbRect.width - 8f,
                    leftRect.height * 0.6f);
                GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
                GUIStyle smallStyle = new GUIStyle(EditorStyles.label) { fontSize = 10 };

                if (current != null)
                {
                    GUI.Label(new Rect(infoRect.x, infoRect.y, infoRect.width, 18f), current.name, titleStyle);
                    string meta =
                        $"Length: {current.length:F3}s    Channels: {current.channels}    Frequency: {current.frequency} Hz";
                    GUI.Label(new Rect(infoRect.x, infoRect.y + 18f, infoRect.width, 16f), meta, smallStyle);

                    // buttons row
                    float btnH = 20f;
                    float bw = 70f;
                    Rect btn1 = new Rect(infoRect.x, infoRect.y + 42f, bw, btnH);
                    Rect btn2 = new Rect(infoRect.x + bw + 6f, infoRect.y + 42f, bw, btnH);
                    if (GUI.Button(btn1, "Load"))
                    {
                        // open object picker for AudioClip
                        EditorGUIUtility.ShowObjectPicker<AudioClip>(current, false, "", 12345);
                    }

                    if (GUI.Button(btn2, "Clear"))
                    {
                        // clear
                        _clip = null;
                        _previewDirty = true;
                        try
                        {
                            _so = new SerializedObject(this);
                        }
                        catch
                        {
                            // ignored
                        }

                        Repaint();
                        return;
                    }
                }
                else
                {
                    // no clip: big Load button + small tip
                    float btnH = 28f;
                    Rect btn = new Rect(infoRect.x, infoRect.y + 6f, infoRect.width * 0.6f, btnH);
                    if (GUI.Button(btn, "Load Audio"))
                    {
                        EditorGUIUtility.ShowObjectPicker<AudioClip>(null, false, "", 12345);
                    }

                    GUI.Label(new Rect(infoRect.x, infoRect.y + 36f, infoRect.width, 16f),
                        "Or drag audio to the right area", smallStyle);
                }

                // Handle object picker results (both for current and null)
                if (Event.current.commandName == "ObjectSelectorClosed" ||
                    Event.current.commandName == "ObjectSelectorUpdated")
                {
                    Object pickedObj = EditorGUIUtility.GetObjectPickerObject();
                    if (pickedObj is AudioClip audio)
                    {
                        try
                        {
                            EditorAudioPlayer.StopAllClips();
                        }
                        catch
                        {
                        }

                        _clip = new OSKAudioClip(audio);
                        _previewDirty = true;
                        try
                        {
                            _so = new SerializedObject(this);
                        }
                        catch
                        {
                        }

                        Repaint();
                        return;
                    }
                }

                // --- RIGHT: Drop / label area (drag & drop) ---
                GUIStyle dropLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 14
                };

                string dropText = "Drag & Drop an AudioClip here";
                if (current != null)
                    dropText = $"Loaded: {current.name}\n(Drag another clip to replace)";

                EditorGUI.LabelField(rightRect, dropText, dropLabelStyle);

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
                                try
                                {
                                    EditorAudioPlayer.StopAllClips();
                                }
                                catch
                                {
                                }

                                this._clip = new OSKAudioClip(audio);
                                _previewDirty = true;
                                try
                                {
                                    _so = new SerializedObject(this);
                                }
                                catch
                                {
                                }

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
                Debug.LogWarning("[OSK] Drop zone error (ignored): " + ex.Message);
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("OSK Audio Editor (BroAudio-like)", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            // if (GUILayout.Button("Reset View", GUILayout.Width(100)))
            // {
            //     _ui.ResetView();
            //     Repaint();
            // }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTransportBar()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUIContent playIcon = EditorGUIUtility.IconContent(_isPlaying ? "d_PauseButton" : "d_PlayButton");
            GUIContent stopIcon = EditorGUIUtility.IconContent("d_PreMatQuad");
            GUIContent loopIcon = EditorGUIUtility.IconContent("d_RotateTool");

            if (GUILayout.Button(playIcon, GUILayout.Width(36), GUILayout.Height(36)))
            {
                if (_isPlaying)
                {
                    // pause
                    EditorAudioSourcePlayer.Pause();
                    _isPlaying = false;
                }
                else
                {
                    // ensure preview clip exists
                    if (!CreateOrUpdatePreview())
                    {
                        Debug.LogWarning("[OSK] Preview build failed — playing raw clip from StartTime.");
                        EditorAudioSourcePlayer.Play(TargetClip, _clip != null ? _clip.StartTime : 0f,
                            _loopPlayback);
                    }
                    else
                    {
                        // previewClip is trimmed to start..end; play from 0 + UI offset
                        float startAt = 0f;
                        // if UI playhead is inside trimmed window, play from that offset
                        if (_ui.PlayheadTime > 0f && _clip != null)
                        {
                            float rel = _ui.PlayheadTime - _clip.StartTime;
                            startAt = Mathf.Clamp(rel, 0f, _previewClip.length);
                        }

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
            // reset playhead to trim start
            if (_clip != null) _ui.PlayheadTime = _clip.StartTime;
        }

        private void DrawClipProperties()
        {
            SerializedProperty clipProp = _so.FindProperty(nameof(_clip));
            if (clipProp == null) return;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(clipProp, new GUIContent("OSK AudioClip"), true);

            if (_clip != null && _clip.SourceClip != null)
            {
                _clip.Volume = EditorGUILayout.Slider("Volume", _clip.Volume, 0f, 1f);
                _clip.StartTime =
                    EditorGUILayout.Slider("Start (s)", _clip.StartTime, 0f, _clip.SourceClip.length);
                _clip.EndTime = EditorGUILayout.Slider("End (s)", _clip.EndTime, _clip.StartTime,
                    _clip.SourceClip.length);
                EditorGUI.BeginChangeCheck();
                float newFadeIn = EditorGUILayout.FloatField("Fade In (s)", _clip.FadeInDuration);
                float newFadeOut = EditorGUILayout.FloatField("Fade Out (s)", _clip.FadeOutDuration);
                if (EditorGUI.EndChangeCheck())
                {
                    // clamp server-side and assign to model
                    float selLength = (_clip.SourceClip != null)
                        ? Mathf.Max(0.0001f, _clip.EndTime - _clip.StartTime)
                        : Mathf.Max(0.0001f, newFadeIn + newFadeOut);
                    newFadeIn = Mathf.Clamp(newFadeIn, 0f, Mathf.Max(0f, selLength - newFadeOut));
                    newFadeOut = Mathf.Clamp(newFadeOut, 0f, Mathf.Max(0f, selLength - newFadeIn));

                    Undo.RecordObject(this, "Fade change");
                    _clip.FadeInDuration = newFadeIn;
                    _clip.FadeOutDuration = newFadeOut;

                    // mark preview dirty
                    _previewDirty = true;

                    // sync visual helper so handles move immediately
                    if (_ui != null)
                    {
                        _ui.SetFadeSeconds(newFadeIn, newFadeOut);
                        _ui.PlayheadTime = _clip.StartTime; // optional: keep playhead inside selection
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
                // mark preview dirty so next play rebuilds
                _previewDirty = true;
                // update ui trims
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
            if (_clip == null || _clip.SourceClip == null) return;

            EditorGUILayout.BeginHorizontal();

            // ---------------------------
            //     1) OVERWRITE ORIGINAL
            // ---------------------------
            AudioClip src = _clip.SourceClip;
            string assetPath = AssetDatabase.GetAssetPath(src);
            bool canOverwrite = assetPath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase);

            GUI.enabled = canOverwrite;
            if (GUILayout.Button("Overwrite Original WAV", GUILayout.Height(28)))
            {
                if (!canOverwrite)
                {
                    Debug.LogError("[OSK] Original asset is not a WAV file, cannot overwrite.");
                }
                else
                {
                    bool ok = EditorUtility.DisplayDialog(
                        "Overwrite Original WAV?",
                        $"This will permanently replace:\n\n{assetPath}\n\nAre you sure?",
                        "Overwrite", "Cancel"
                    );

                    if (ok)
                    {
                        using (var p = new AudioProcessor(src))
                        {
                            p.Trim(_clip.StartTime, _clip.EndTime);
                            p.AdjustVolume(_clip.Volume);
                            if (_clip.IsReversed) p.Reverse();
                            if (_clip.ConvertToMono) p.ConvertToMono(_clip.MonoMode);
                            if (_clip.FadeInDuration > 0f || _clip.FadeOutDuration > 0f)
                                p.ApplyFading(_clip.FadeInDuration, _clip.FadeOutDuration);

                            AudioClip outClip = p.GetResultClip();

                            // Write directly to original file
                            if (WavWriter.Save(assetPath, outClip))
                            {
                                AssetDatabase.Refresh();

                                // Reload asset
                                AudioClip newClip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                                _clip = new OSKAudioClip(newClip);

                                // Ping the asset in Project window
                                EditorGUIUtility.PingObject(newClip);

                                Debug.Log($"[OSK] Overwritten original file: {assetPath}");
                            }
                        }
                    }
                }
            }
            GUI.enabled = true;

            GUILayout.Space(10);

            // ---------------------------
            //     2) SAVE AS NEW FILE
            // ---------------------------
            if (GUILayout.Button("Save Trimmed As WAV", GUILayout.Height(28)))
            {
                if (src == null) return;

                string path = EditorUtility.SaveFilePanelInProject(
                    "Save trimmed audio", 
                    src.name + "_trimmed", 
                    "wav",
                    ""
                );
                if (string.IsNullOrEmpty(path)) return;

                using (var p = new AudioProcessor(src))
                {
                    p.Trim(_clip.StartTime, _clip.EndTime);
                    p.AdjustVolume(_clip.Volume);
                    if (_clip.IsReversed) p.Reverse();
                    if (_clip.ConvertToMono) p.ConvertToMono(_clip.MonoMode);
                    if (_clip.FadeInDuration > 0f || _clip.FadeOutDuration > 0f)
                        p.ApplyFading(_clip.FadeInDuration, _clip.FadeOutDuration);

                    AudioClip outClip = p.GetResultClip();
                    if (WavWriter.Save(path, outClip))
                    {
                        AssetDatabase.Refresh();
                        var newClip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

                        EditorGUIUtility.PingObject(newClip);
                        Debug.Log($"Saved new clip: {path}");
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }


        // ---------------- Preview builder (runtime clip via AudioProcessor) ----------------
        private bool CreateOrUpdatePreview()
        {
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
                Debug.LogError($"OSK: Create preview failed: {ex.Message}");
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

            // Do not DestroyImmediate asset loaded from project; our previewClip is runtime-created
            _previewClip = null;
            _previewDirty = true;
        }

        // --------- UI Helper callbacks -----------
        private void OnUiScrub(float timeAbsolute)
        {
            // timeAbsolute = seconds relative to original clip
            // build preview then play at offset
            if (!CreateOrUpdatePreview()) return;

            float rel = Mathf.Clamp(timeAbsolute - _clip.StartTime, 0f, _previewClip.length);
            EditorAudioSourcePlayer.Play(_previewClip, rel, _loopPlayback);
            _isPlaying = true;
            _ui.PlayheadTime = timeAbsolute;
        }

        private void OnUiScrubEnd()
        {
            // optionally stop or continue
            _isPlaying = false;
            EditorAudioSourcePlayer.Stop();
        }

        private void OnUiLoopPlay(float loopStartAbs, float loopEndAbs)
        {
            // store loop in helper; start looped playback
            _ui.IsLooping = true;
            _ui.LoopStart = loopStartAbs;
            _ui.LoopEnd = loopEndAbs;

            if (!CreateOrUpdatePreview()) return;
            // play at loopStart relative
            float rel = Mathf.Clamp(loopStartAbs - _clip.StartTime, 0f, _previewClip.length);
            EditorAudioSourcePlayer.Play(_previewClip, rel, true);
            _isPlaying = true;
        }

        private void OnUiSetPlayhead(float absTime)
        {
            _ui.PlayheadTime = absTime;
            if (_isPlaying)
            {
                // move playback to that position
                if (!CreateOrUpdatePreview()) return;
                float rel = Mathf.Clamp(absTime - _clip.StartTime, 0f, _previewClip.length);
                EditorAudioSourcePlayer.Play(_previewClip, rel, _loopPlayback);
            }
        }

        // Helper
        private AudioClip TargetClip => _clip?.SourceClip;
        
        // public helper so menu can set clip safely
        public void SetClipFromProjectSelection(AudioClip clip)
        {
            if (clip == null) return;
            _clip = new OSKAudioClip(clip);
            _so = new SerializedObject(this);
            _previewDirty = true;
            // sync UI helper
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