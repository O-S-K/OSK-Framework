#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace OSK
{
    [CustomEditor(typeof(ListSoundSO))]
    public class SoundDataEditor : OdinEditor
    {
        private Dictionary<string, Dictionary<SoundType, bool>> soundTypeFoldoutsPerGroup =
            new Dictionary<string, Dictionary<SoundType, bool>>();

        private Dictionary<string, bool> groupFoldouts = new Dictionary<string, bool>();

        [ListDrawerSettings(Expanded = true, DraggableItems = false, ShowIndexLabels = true)]
        private static List<string> groupNames = new List<string>() { "Music", "UI" };
        private ListSoundSO listSoundSo;
        private SoundData newSoundDraft = null;

        private bool showTable = true;
        private bool showGroupNames = true;

        public override void OnInspectorGUI()
        {
            listSoundSo = (ListSoundSO)target;
            DrawDefaultInspector();

            if (listSoundSo.showSoundSettings)
            {
                if (GUILayout.Button("Sort To Type"))
                    SortToType(listSoundSo);

                if (GUILayout.Button("Set ID For Name Clip"))
                    SetIDForNameClip();
            }

            EditorGUILayout.Space(20);
            GUI.color = Color.yellow;
            EditorGUILayout.LabelField("⚠️ Please enable sound in scene game to test play sound.", EditorStyles.boldLabel);
            GUI.color = Color.white;
            EditorGUILayout.Space(10);

            showTable = EditorGUILayout.Foldout(showTable, "Show Sound Info Table",true, EditorStyles.boldLabel);
            if (!showTable) return;

            GUILayout.Space(10);
            showGroupNames = EditorGUILayout.Foldout(showGroupNames, "Show Group Names");
            if (showGroupNames)
                DrawGroupNames();

            if (groupNames == null || groupNames.Count == 0)
                groupNames?.Add("Default");

            // Gom group
            var groups = listSoundSo.ListSoundInfos
                .Select(x => string.IsNullOrEmpty(x.group) ? "Default" : x.group)
                .Distinct()
                .OrderBy(x => x);

            EditorGUILayout.Space(10);
            foreach (var group in groups)
            {
                if (!groupFoldouts.ContainsKey(group))
                    groupFoldouts[group] = true;
                if (!soundTypeFoldoutsPerGroup.ContainsKey(group))
                    soundTypeFoldoutsPerGroup[group] = new Dictionary<SoundType, bool>();

                GUI.color = Color.white;
                groupFoldouts[group] = EditorGUILayout.Foldout(groupFoldouts[group], $"Group: {group}", true, new GUIStyle()
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 14,
                    normal = new GUIStyleState() { textColor = ColorUtils.LawnGreen }
                });
                GUI.color = Color.white;
                
                if (!groupFoldouts[group]) continue;

                EditorGUI.indentLevel++;
                foreach (SoundType type in System.Enum.GetValues(typeof(SoundType)))
                {
                    if (!listSoundSo.ListSoundInfos.Any(x => x.type == type && x.group == group))
                        continue;

                    if (!soundTypeFoldoutsPerGroup[group].ContainsKey(type))
                        soundTypeFoldoutsPerGroup[group][type] = true;

                    soundTypeFoldoutsPerGroup[group][type] =
                        EditorGUILayout.Foldout(soundTypeFoldoutsPerGroup[group][type], type.ToString(), true);

                    if (!soundTypeFoldoutsPerGroup[group][type]) continue;
                    DrawTableHeaders();
                    for (int i = 0; i < listSoundSo.ListSoundInfos.Count; i++)
                    {
                        SoundData soundData = listSoundSo.ListSoundInfos[i];
                        if (soundData.type != type || soundData.group != group) continue;

                        DrawRow(soundData, i);
                    }

                    DrawRowBorder();
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(20);
             
            GUI.color = Color.green;
            if (GUILayout.Button("Add New Sound Info", GUILayout.Width(200)))
            {
                newSoundDraft = new SoundData
                {
                    audioClip = null,
                    id = "",
                    group = groupNames[0],
                    type = SoundType.SFX,
                    volume = 1f,
                    pitch = new MinMaxFloat(1f, 1f)
                };
            }
            GUI.color = Color.white;

            if (newSoundDraft != null)
                DrawNewSoundDraft();

            DrawLoadFolderSection();
            DrawEnumGenSection();

            if (GUI.changed)
                EditorUtility.SetDirty(target);
        }

        #region Draw Helpers

        private void DrawRow(SoundData soundData, int index)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();

            // AudioClip
            soundData.audioClip = (AudioClip)EditorGUILayout.ObjectField(soundData.audioClip,
                typeof(AudioClip), false, GUILayout.Width(200));
            soundData.UpdateId();

            // Group Dropdown
            int currentGroupIndex = Mathf.Max(0, groupNames.IndexOf(soundData.group));
            currentGroupIndex = EditorGUILayout.Popup(currentGroupIndex, groupNames.ToArray(), GUILayout.Width(120));
            soundData.group = groupNames[currentGroupIndex];

            // Type Dropdown
            soundData.type = (SoundType)EditorGUILayout.EnumPopup(soundData.type, GUILayout.Width(100));

            // Volume Slider
            GUILayout.Label(EditorGUIUtility.IconContent("d_AudioSource Icon"), GUILayout.Width(20),
                GUILayout.Height(20));
            soundData.volume = GUILayout.HorizontalSlider(soundData.volume, 0f, 1f, GUILayout.Width(70));
            GUILayout.Label(soundData.volume.ToString("F1"), GUILayout.Width(25));

            // Pitch 
            GUILayout.Space(-10);
            float oldMin = soundData.pitch.min;
            float oldMax = soundData.pitch.max;
            // Pitch Slider
            Rect sliderRect = GUILayoutUtility.GetRect(100, 20, GUILayout.ExpandWidth(false));
            float newMin = oldMin;
            float newMax = oldMax;
            EditorGUI.MinMaxSlider(sliderRect, ref newMin, ref newMax, 0.1f, 2.0f);
            string minStr = newMin.ToString("F1");
            string maxStr = newMax.ToString("F1");
            
            GUILayout.Space(-25);
            minStr = EditorGUILayout.DelayedTextField(minStr, GUILayout.Width(70));
            GUILayout.Space(-25);
            maxStr = EditorGUILayout.DelayedTextField(maxStr, GUILayout.Width(70));
            if (float.TryParse(minStr, out float parsedMin))
            {
                newMin = Mathf.Clamp(Mathf.Round(parsedMin * 10f) / 10f, 0.1f, newMax);
            }

            if (float.TryParse(maxStr, out float parsedMax))
            {
                newMax = Mathf.Clamp(Mathf.Round(parsedMax * 10f) / 10f, newMin, 2.0f);
            }

            if (Mathf.Abs(newMin - oldMin) > 0.01f || Mathf.Abs(newMax - oldMax) > 0.01f)
            {
                var newPitch = new MinMaxFloat(newMin, newMax);
                soundData.pitch = newPitch;
                soundData.SetPitch(newPitch);
            }

            // Play / Stop
            GUILayout.Space(20);
            GUI.enabled = soundData.audioClip != null && !soundData.IsPlaying();
            GUI.color = soundData.IsPlaying() ? Color.green : Color.white;
            if (GUILayout.Button("▶", GUILayout.Width(40)))
                soundData.Play(soundData.pitch);
            GUI.color = Color.white;

            GUI.enabled = soundData.audioClip != null && soundData.IsPlaying();
            if (GUILayout.Button("■", GUILayout.Width(40)))
                soundData.Stop();

            GUI.enabled = true;

            // Remove (red nếu duplicate)
            bool isDuplicate = listSoundSo.ListSoundInfos
                .Count(x => x.audioClip == soundData.audioClip && x.audioClip != null) > 1;
            if (isDuplicate) GUI.color = Color.red;
            if (GUILayout.Button("X", GUILayout.Width(50)))
            {
                listSoundSo.ListSoundInfos.RemoveAt(index);
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
                return;
            }

            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        private void DrawRowBorder()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, Color.gray);
        }

        private void DrawTableHeaders()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Audio Clip", GUILayout.Width(220));
            EditorGUILayout.LabelField("Group", GUILayout.Width(120));
            EditorGUILayout.LabelField("Type", GUILayout.Width(70));
            EditorGUILayout.LabelField("Volume", GUILayout.Width(140));
            EditorGUILayout.LabelField("Pitch", GUILayout.Width(75));
            EditorGUILayout.LabelField("Min", GUILayout.Width(45));
            EditorGUILayout.LabelField("Max", GUILayout.Width(75));
            
            GUILayout.Label("Play", GUILayout.Width(40));
            GUILayout.Label("Stop", GUILayout.Width(40));
            GUILayout.Label("Remove", GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
            DrawRowBorder();
        }

        private void DrawGroupNames()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🎵 Group Names", EditorStyles.boldLabel);
            for (int i = 0; i < groupNames.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                groupNames[i] = EditorGUILayout.TextField(groupNames[i]);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    groupNames.RemoveAt(i);
                    i--;
                    continue;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Group"))
                groupNames.Add("NewGroup");
            EditorGUILayout.EndVertical();
        }

        private void DrawNewSoundDraft()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("➕ New Sound Draft", EditorStyles.boldLabel);

            newSoundDraft.audioClip =
                (AudioClip)EditorGUILayout.ObjectField("Audio Clip", newSoundDraft.audioClip, typeof(AudioClip), false);
            int groupIndex = Mathf.Max(0, groupNames.IndexOf(newSoundDraft.group ?? groupNames[0]));
            groupIndex = EditorGUILayout.Popup("Group", groupIndex, groupNames.ToArray());
            newSoundDraft.group = groupNames[groupIndex];
            newSoundDraft.type = (SoundType)EditorGUILayout.EnumPopup("Type", newSoundDraft.type);
            newSoundDraft.volume = EditorGUILayout.Slider("Volume", newSoundDraft.volume, 0f, 1f);
            float min = newSoundDraft.pitch.min, max = newSoundDraft.pitch.max;
            EditorGUILayout.MinMaxSlider("Pitch", ref min, ref max, 0.1f, 2f);
            newSoundDraft.pitch = new MinMaxFloat(min, max);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Confirm Add", GUILayout.Width(120)))
            {
                if (newSoundDraft.audioClip != null)
                {
                    newSoundDraft.id = newSoundDraft.audioClip.name;
                    listSoundSo.ListSoundInfos.Add(newSoundDraft);
                    newSoundDraft = null;
                    EditorUtility.SetDirty(listSoundSo);
                }
            }

            if (GUILayout.Button("Cancel", GUILayout.Width(80)))
                newSoundDraft = null;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Utility

        private void SortToType(ListSoundSO so) =>
            so.ListSoundInfos.Sort((a, b) => a.type.CompareTo(b.type));

        private void SetIDForNameClip()
        {
            foreach (var sound in listSoundSo.ListSoundInfos)
                if (sound.audioClip != null)
                    sound.id = sound.audioClip.name;
        }

        private void DrawLoadFolderSection()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Load All sounds In Path", EditorStyles.boldLabel);
            if (GUILayout.Button("Load Folder Sounds", GUILayout.Width(150)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
                if (string.IsNullOrEmpty(path)) return;

                path = "Assets" + path.Replace(Application.dataPath, "");
                var exts = new[] { ".wav", ".mp3", ".ogg" };
                var clips = Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                    .Where(f => exts.Contains(Path.GetExtension(f).ToLower()))
                    .Select(f => AssetDatabase.LoadAssetAtPath<AudioClip>(f.Replace("\\", "/")))
                    .Where(c => c != null).ToList();

                foreach (var clip in clips)
                {
                    if (!listSoundSo.ListSoundInfos.Any(s => s.audioClip != null && s.audioClip.name == clip.name))
                    {
                        listSoundSo.ListSoundInfos.Add(new SoundData
                        {
                            audioClip = clip,
                            id = clip.name,
                            group = "Default",
                            type = SoundType.SFX,
                            volume = 1f,
                            pitch = new MinMaxFloat(1f, 1f)
                        });
                    }
                }

                EditorUtility.SetDirty(listSoundSo);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private void DrawEnumGenSection()
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Gen enum SoundID", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Open File"))
            {
                string path = EditorUtility.SaveFilePanel("Select File Path", "Assets", "SoundID.cs", "cs");
                if (string.IsNullOrEmpty(path)) return;
                path = "Assets" + path.Replace(Application.dataPath, "");
                listSoundSo.filePathSoundID = path;
                EditorUtility.SetDirty(listSoundSo);
            }
            
            EditorGUILayout.LabelField("File Path:", GUILayout.Width(70));
            EditorGUILayout.LabelField(listSoundSo.filePathSoundID);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Generate Enum ID"))
            {
                var names = listSoundSo.ListSoundInfos
                    .Where(x => x.audioClip != null)
                    .Select(x => x.id)
                    .Distinct()
                    .ToList();

                string filePath = listSoundSo.filePathSoundID;
                var sb = new StringBuilder();
                sb.AppendLine("public enum SoundID {");
                foreach (var n in names)
                    sb.AppendLine($"    {n},");
                sb.AppendLine("}");
                File.WriteAllText(filePath, sb.ToString());
                AssetDatabase.Refresh();
            }
       
        }

        #endregion
    }
}
#endif