#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace OSK
{
    public class SoundManagerWindow : EditorWindow
    {
        private ListSoundSO listSoundSo;
        private SoundData newSoundDraft = null;

        private bool showTable = true;
        private bool showGroupNames = true;

        private Dictionary<string, Dictionary<SoundType, bool>> soundTypeFoldoutsPerGroup =
            new Dictionary<string, Dictionary<SoundType, bool>>();

        private Dictionary<string, bool> groupFoldouts = new Dictionary<string, bool>();

        // editable group names (persist as you like)
        private static List<string> groupNames = new List<string>() { "Music", "UI", "Default" };

        // --- library selection
        private string selectedGroup = null;

        // --- layout settings (change to taste)
        private const float LeftSidebarWidth = 260f;
        private const float RightPanelMinWidth = 700f; // MIN width for right panel (Option C)
        private Vector2 leftScroll;
        private Vector2 rightScroll;

        [MenuItem("OSK-Framework/Sound/Manager Window")]
        public static void ShowWindow()
        {
            var w = GetWindow<SoundManagerWindow>("Sound Manager");
            w.minSize = new Vector2(LeftSidebarWidth + 400, 360);
        }

        private void OnEnable()
        {
            // Try to auto-find the first ListSoundSO in the project
            if (listSoundSo == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:ListSoundSO");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    listSoundSo = AssetDatabase.LoadAssetAtPath<ListSoundSO>(path);
                }
            }
        }

        private void OnGUI()
        { 
            EditorGUI.BeginChangeCheck();
            listSoundSo = (ListSoundSO)EditorGUILayout.ObjectField("ListSoundSO", listSoundSo, typeof(ListSoundSO), false);
            if (EditorGUI.EndChangeCheck())
            {
                soundTypeFoldoutsPerGroup.Clear();
                groupFoldouts.Clear();
            }

            if (listSoundSo == null)
            {
                EditorGUILayout.HelpBox("No ListSoundSO assigned. Drag one here or ensure a ListSoundSO exists in project (auto-loads first found).", MessageType.Info);
            }

            // --- responsive layout with fixed-left and right panel min width (Option C)
            EditorGUILayout.BeginHorizontal();

            // LEFT: Library Sidebar (fixed width)
            GUILayout.BeginVertical(GUILayout.Width(LeftSidebarWidth));
            GUILayout.Space(4);
            leftScroll = EditorGUILayout.BeginScrollView(leftScroll, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Width(LeftSidebarWidth), GUILayout.ExpandHeight(true));
            DrawLibrarySidebarContents();
            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();

            // compute right panel width from window size but respect minimum
            float padding = 16f;
            float computedRight = Mathf.Max(RightPanelMinWidth, position.width - LeftSidebarWidth - padding);
            // if window is smaller than Left+MinRight, computedRight will equal MinRight and overall window will scroll horizontally in the editor
            GUILayout.BeginVertical(GUILayout.Width(computedRight));
            rightScroll = EditorGUILayout.BeginScrollView(rightScroll, GUILayout.Width(computedRight), GUILayout.ExpandHeight(true));

            // main right content
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            if (listSoundSo != null)
            {
                EditorGUILayout.Space(6);
                if (listSoundSo.showSoundSettings)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Sort To Type", GUILayout.Width(120)))
                        SortToType(listSoundSo);
                    if (GUILayout.Button("Set ID For Name Clip", GUILayout.Width(160)))
                        SetIDForNameClip();
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox("⚠️ Please enable sound in scene game to test play sound.", MessageType.Warning);

                showTable = EditorGUILayout.Foldout(showTable, selectedGroup == null ? "Show Sound Info Table (All Groups)" : $"Show Sound Info Table (Group: {selectedGroup})", true, EditorStyles.foldoutHeader);
                if (showTable)
                {
                    EditorGUILayout.Space(6);
                    // showGroupNames = EditorGUILayout.Foldout(showGroupNames, "Show Group Names", true);
                    // if (showGroupNames)
                    //     DrawGroupNames();

                    if (groupNames == null || groupNames.Count == 0)
                        groupNames = new List<string>() { "Default" };

                    IEnumerable<string> groups;
                    if (selectedGroup == null)
                    {
                        groups = listSoundSo.ListSoundInfos
                            .Select(x => string.IsNullOrEmpty(x.group) ? "Default" : x.group)
                            .Distinct()
                            .OrderBy(x => x);
                    }
                    else
                    {
                        groups = new List<string> { selectedGroup };
                    }

                    EditorGUILayout.Space(10);
                    foreach (var group in groups)
                    {
                        if (!groupFoldouts.ContainsKey(group))
                            groupFoldouts[group] = true;
                        if (!soundTypeFoldoutsPerGroup.ContainsKey(group))
                            soundTypeFoldoutsPerGroup[group] = new Dictionary<SoundType, bool>();

                        // GUI.color = Color.white;
                        // groupFoldouts[group] = EditorGUILayout.Foldout(groupFoldouts[group], $"Group: {group}", true, new GUIStyle()
                        // {
                        //     fontStyle = FontStyle.Bold,
                        //     fontSize = 12,
                        //     normal = new GUIStyleState() { textColor = Color.green }
                        // });
                        // GUI.color = Color.white;

                        if (!groupFoldouts[group]) continue;

                        EditorGUI.indentLevel++;
                        foreach (SoundType type in Enum.GetValues(typeof(SoundType)))
                        {
                            if (!listSoundSo.ListSoundInfos.Any(x => x.type == type && (string.IsNullOrEmpty(x.group) ? "Default" : x.group) == group))
                                continue;

                            if (!soundTypeFoldoutsPerGroup[group].ContainsKey(type))
                                soundTypeFoldoutsPerGroup[group][type] = true;

                            soundTypeFoldoutsPerGroup[group][type] =
                                EditorGUILayout.Foldout(soundTypeFoldoutsPerGroup[group][type], type.ToString(), true);

                            if (!soundTypeFoldoutsPerGroup[group][type]) continue;

                            // Table headers (fixed width per column so they don't push total width)
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Audio Clip", GUILayout.Width(220));
                            EditorGUILayout.LabelField("Group", GUILayout.Width(120));
                            EditorGUILayout.LabelField("Type", GUILayout.Width(70));
                            EditorGUILayout.LabelField("Volume", GUILayout.Width(145));
                            EditorGUILayout.LabelField("Pitch", GUILayout.Width(75));
                            EditorGUILayout.LabelField("Min", GUILayout.Width(45));
                            EditorGUILayout.LabelField("Max", GUILayout.Width(75));
                            GUILayout.Label("Play", GUILayout.Width(40));
                            GUILayout.Label("Stop", GUILayout.Width(40));
                            GUILayout.Label("Remove", GUILayout.Width(50));
                            EditorGUILayout.EndHorizontal();
                            DrawRowBorder();

                            // Rows
                            // iterate by index because we may remove items
                            for (int i = 0; i < listSoundSo.ListSoundInfos.Count; i++)
                            {
                                SoundData soundData = listSoundSo.ListSoundInfos[i];
                                string itemGroup = string.IsNullOrEmpty(soundData.group) ? "Default" : soundData.group;
                                if (soundData.type != type || itemGroup != group) continue;

                                EditorGUILayout.BeginHorizontal();
                                // AudioClip field
                                soundData.audioClip = (AudioClip)EditorGUILayout.ObjectField(soundData.audioClip, typeof(AudioClip), false, GUILayout.Width(220));
                                soundData.UpdateId();

                                // Group popup
                                int currentGroupIndex = Mathf.Max(0, groupNames.IndexOf(soundData.group ?? "Default"));
                                if (currentGroupIndex < 0) currentGroupIndex = 0;
                                currentGroupIndex = EditorGUILayout.Popup(currentGroupIndex, groupNames.ToArray(), GUILayout.Width(120));
                                soundData.group = groupNames[currentGroupIndex];

                                // Type
                                soundData.type = (SoundType)EditorGUILayout.EnumPopup(soundData.type, GUILayout.Width(75));

                                // Volume
                                GUILayout.Label(EditorGUIUtility.IconContent("d_AudioSource Icon"), GUILayout.Width(20), GUILayout.Height(20));
                                soundData.volume = GUILayout.HorizontalSlider(soundData.volume, 0f, 1f, GUILayout.Width(75));
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

                                GUILayout.Space(-5);
                                minStr = EditorGUILayout.DelayedTextField(minStr, GUILayout.Width(65));
                                GUILayout.Space(-15);
                                maxStr = EditorGUILayout.DelayedTextField(maxStr, GUILayout.Width(65));
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
                                
                                GUILayout.Space(5);

                                // Play / Stop
                                GUI.enabled = soundData.audioClip != null && !soundData.IsPlaying();
                                if (GUILayout.Button("▶", GUILayout.Width(40))) soundData.Play(soundData.pitch);
                                GUI.enabled = soundData.audioClip != null && soundData.IsPlaying();
                                if (GUILayout.Button("■", GUILayout.Width(40))) soundData.Stop();
                                GUI.enabled = true;

                                // Remove
                                if (GUILayout.Button("X", GUILayout.Width(50)))
                                {
                                    Undo.RecordObject(listSoundSo, "Remove SoundData");
                                    listSoundSo.ListSoundInfos.RemoveAt(i);
                                    EditorUtility.SetDirty(listSoundSo);
                                    EditorGUILayout.EndHorizontal();
                                    break; // stop iterating rows since collection changed
                                }

                                EditorGUILayout.EndHorizontal();
                            }

                            DrawRowBorder();
                        }

                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.Space(12);

                    GUI.color = Color.green;
                    if (GUILayout.Button("Add New Sound Info", GUILayout.Width(200)))
                    {
                        newSoundDraft = new SoundData
                        {
                            audioClip = null,
                            id = "",
                            group = string.IsNullOrEmpty(selectedGroup) ? (groupNames.Count > 0 ? groupNames[0] : "Default") : selectedGroup,
                            type = SoundType.SFX,
                            volume = 1f,
                            pitch = new MinMaxFloat(1f, 1f)
                        };
                    }
                    GUI.color = Color.white;

                    if (newSoundDraft != null)
                        DrawNewSoundDraft();

                    EditorGUILayout.Space(10);
                    DrawLoadFolderSection();
                    DrawEnumGenSection();
                }
            }
            EditorGUILayout.EndVertical(); // end ExpandWidth(true)

            EditorGUILayout.EndScrollView(); // right scroll
            GUILayout.EndVertical(); // right column

            EditorGUILayout.EndHorizontal(); // main horz

            // persist
            if (GUI.changed && listSoundSo != null)
            {
                EditorUtility.SetDirty(listSoundSo);
            }
        }

        // Sidebar contents (separated so we can call inside left scroll view)
        private void DrawLibrarySidebarContents()
        {
            EditorGUILayout.LabelField("Library Groups", EditorStyles.boldLabel);

            var groupsInData = listSoundSo != null
                ? listSoundSo.ListSoundInfos.Select(x => string.IsNullOrEmpty(x.group) ? "Default" : x.group).Distinct().OrderBy(x => x).ToList()
                : new List<string>();

            var combined = new List<string>(groupNames);
            foreach (var g in groupsInData)
                if (!combined.Contains(g))
                    combined.Add(g);

            // group buttons (fixed-width sidebar)
            for (int i = 0; i < combined.Count; i++)
            {
                string g = combined[i];
                EditorGUILayout.BeginHorizontal();

                GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
                btnStyle.alignment = TextAnchor.MiddleLeft;
                if (g == selectedGroup)
                {
                    GUI.backgroundColor = Color.Lerp(Color.green, Color.black, 0.6f);
                    GUILayout.Button(g, btnStyle, GUILayout.Height(26), GUILayout.ExpandWidth(true));
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    if (GUILayout.Button(g, btnStyle, GUILayout.Height(26), GUILayout.ExpandWidth(true)))
                    {
                        selectedGroup = g;
                    }
                }

                if (groupNames.Contains(g))
                {
                    if (GUILayout.Button("x", GUILayout.Width(22), GUILayout.Height(22)))
                    {
                        bool ok = EditorUtility.DisplayDialog("Remove Group?", $"Remove group '{g}' from the editable list? (This does NOT modify existing items)", "Remove", "Cancel");
                        if (ok)
                        {
                            groupNames.Remove(g);
                            if (selectedGroup == g) selectedGroup = null;
                            i--;
                            EditorGUILayout.EndHorizontal();
                            continue;
                        }
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Add Group", GUILayout.Height(24)))
            {
                AddGroupWindow.ShowPopup((string enteredName) =>
                {
                    string name = enteredName.Trim();
                    if (string.IsNullOrEmpty(name)) return;
                    if (groupNames.Any(g => string.Equals(g, name, StringComparison.OrdinalIgnoreCase)))
                    {
                        EditorUtility.DisplayDialog("Group exists", $"Group '{name}' already exists.", "OK");
                        return;
                    }

                    groupNames.Add(name);
                    selectedGroup = name;
                    EditorUtility.SetDirty(listSoundSo); // optional
                }, "NewGroup");
            }

            if (GUILayout.Button("Clear Selection", GUILayout.Height(24)))
            {
                selectedGroup = null;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);
            using (new EditorGUI.DisabledScope(selectedGroup == null || listSoundSo == null))
            {
                if (GUILayout.Button("Add Selected Clips to Group", GUILayout.Height(28)))
                {
                    AddSelectedClipsToGroup(selectedGroup);
                }
            }
            
            if (GUILayout.Button("Open Clip editor", GUILayout.Height(28)))
            {
                OSKProjectMenu.OpenInEditor(true);
            }

            GUILayout.FlexibleSpace(); 
        }

        #region Draw Helpers

        private void DrawRowBorder()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, Color.gray);
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

            int groupIndex = Mathf.Max(0, groupNames.IndexOf(newSoundDraft.group ?? (selectedGroup ?? groupNames[0])));
            if (groupIndex < 0) groupIndex = 0;
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
                if (newSoundDraft.audioClip != null && listSoundSo != null)
                {
                    Undo.RecordObject(listSoundSo, "Add SoundData");
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

        #region Load / Enum / Utilities

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
                        Undo.RecordObject(listSoundSo, "Add Sounds From Folder");
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

            if (GUILayout.Button("Open File", GUILayout.Width(120)))
            {
                string absPath = EditorUtility.OpenFilePanel("Select File (in Project)", Application.dataPath, "cs");
                if (!string.IsNullOrEmpty(absPath))
                {
                    if (absPath.StartsWith(Application.dataPath))
                    {
                        string relPath = "Assets" + absPath.Replace(Application.dataPath, "");
                        listSoundSo.filePathSoundID = relPath.Replace("\\", "/");
                        EditorUtility.SetDirty(listSoundSo);
                    }
                    else
                    {
                        string savePath = EditorUtility.SaveFilePanel("Save SoundID.cs to Project", Application.dataPath, "SoundID", "cs");
                        if (!string.IsNullOrEmpty(savePath) && savePath.StartsWith(Application.dataPath))
                        {
                            string relSave = "Assets" + savePath.Replace(Application.dataPath, "");
                            listSoundSo.filePathSoundID = relSave.Replace("\\", "/");
                            EditorUtility.SetDirty(listSoundSo);
                        }
                    }
                }
            }

            if (GUILayout.Button("Open In Windows", GUILayout.Width(140)))
            {
                if (!string.IsNullOrEmpty(listSoundSo.filePathSoundID))
                {
                    string rel = listSoundSo.filePathSoundID.Replace("/", Path.DirectorySeparatorChar.ToString());
                    string abs = Path.Combine(Application.dataPath, rel.Substring("Assets".Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                    if (File.Exists(abs))
                    {
                        EditorUtility.RevealInFinder(abs);
                        try
                        {
                            var psi = new ProcessStartInfo
                            {
                                FileName = abs,
                                UseShellExecute = true
                            };
                            Process.Start(psi);
                        }
                        catch (Exception ex)
                        {
                            EditorUtility.DisplayDialog("Cannot open file", $"Failed to open file with default app: {ex.Message}", "OK");
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("File not found", $"Could not find file at '{listSoundSo.filePathSoundID}'.", "OK");
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("No file selected", "Please select or save a file first.", "OK");
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("File Path:", GUILayout.Width(70));
            EditorGUILayout.LabelField(string.IsNullOrEmpty(listSoundSo.filePathSoundID) ? "<Not set>" : listSoundSo.filePathSoundID);

            EditorGUILayout.Space(6);

            if (GUILayout.Button("Generate Enum ID"))
            {
                var names = listSoundSo.ListSoundInfos
                    .Where(x => x.audioClip != null)
                    .Select(x => x.id)
                    .Distinct()
                    .ToList();

                string filePath = listSoundSo.filePathSoundID;

                if (string.IsNullOrEmpty(filePath))
                {
                    string savePath = EditorUtility.SaveFilePanel("Save SoundID.cs", Application.dataPath, "SoundID", "cs");
                    if (string.IsNullOrEmpty(savePath)) return;
                    if (!savePath.StartsWith(Application.dataPath))
                    {
                        EditorUtility.DisplayDialog("Invalid location", "Please save the file inside the project's Assets folder.", "OK");
                        return;
                    }

                    filePath = "Assets" + savePath.Replace(Application.dataPath, "");
                    listSoundSo.filePathSoundID = filePath.Replace("\\", "/");
                    EditorUtility.SetDirty(listSoundSo);
                }

                var sb = new StringBuilder();
                sb.AppendLine("// Auto-generated SoundID enum");
                sb.AppendLine("public enum SoundID");
                sb.AppendLine("{");
                foreach (var n in names)
                {
                    string safe = MakeSafeEnumName(n);
                    sb.AppendLine($"    {safe},");
                }
                sb.AppendLine("}");

                string absWritePath = Path.Combine(Application.dataPath, filePath.Substring("Assets".Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                File.WriteAllText(absWritePath, sb.ToString(), Encoding.UTF8);

                AssetDatabase.Refresh();
            }
        }

        private void SortToType(ListSoundSO so) =>
            so.ListSoundInfos.Sort((a, b) => a.type.CompareTo(b.type));

        private void SetIDForNameClip()
        {
            foreach (var sound in listSoundSo.ListSoundInfos)
                if (sound.audioClip != null)
                    sound.id = sound.audioClip.name;
            EditorUtility.SetDirty(listSoundSo);
        }

        private void AddSelectedClipsToGroup(string group)
        {
            if (listSoundSo == null || string.IsNullOrEmpty(group)) return;
            var selectedClips = Selection.GetFiltered<AudioClip>(SelectionMode.Assets);
            if (selectedClips == null || selectedClips.Length == 0)
            {
                EditorUtility.DisplayDialog("No clips selected", "Please select one or more AudioClips in the Project window.", "OK");
                return;
            }

            int added = 0;
            foreach (var clip in selectedClips)
            {
                if (!listSoundSo.ListSoundInfos.Any(s => s.audioClip == clip))
                {
                    Undo.RecordObject(listSoundSo, "Add Selected Clips");
                    listSoundSo.ListSoundInfos.Add(new SoundData
                    {
                        audioClip = clip,
                        id = clip.name,
                        group = group,
                        type = SoundType.SFX,
                        volume = 1f,
                        pitch = new MinMaxFloat(1f, 1f)
                    });
                    added++;
                }
            }

            if (added > 0)
            {
                EditorUtility.SetDirty(listSoundSo);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog("Add Selected Clips", $"Added {added} clip(s) to group '{group}'.", "OK");
        }

        // safe enum name helper
        private static string MakeSafeEnumName(string s)
        {
            if (string.IsNullOrEmpty(s)) return "_UNKNOWN";
            var sb = new StringBuilder();
            foreach (char c in s)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
                else if (char.IsWhiteSpace(c) || c == '-' || c == '.')
                    sb.Append('_');
            }
            string outStr = sb.ToString();
            if (string.IsNullOrEmpty(outStr)) outStr = "_UNKNOWN";
            if (!char.IsLetter(outStr[0]) && outStr[0] != '_') outStr = "_" + outStr;
            return outStr;
        }

        #endregion
    }

    // small modal popup to create new group
    public class AddGroupWindow : EditorWindow
    {
        private string newName = "NewGroup";
        private Action<string> onCreate;

        public static void ShowPopup(Action<string> onCreate, string defaultName = "NewGroup")
        {
            var win = CreateInstance<AddGroupWindow>();
            win.titleContent = new GUIContent("Add Group");
            win.newName = defaultName;
            win.onCreate = onCreate;
            win.minSize = new Vector2(360, 80);
            win.maxSize = new Vector2(360, 80);
            win.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Create new group", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            EditorGUI.BeginChangeCheck();
            newName = EditorGUILayout.TextField("Group Name", newName);
            if (EditorGUI.EndChangeCheck())
            {
                newName = newName.Trim();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create", GUILayout.Height(26)))
            {
                string safe = newName.Trim();
                if (string.IsNullOrEmpty(safe))
                {
                    EditorUtility.DisplayDialog("Invalid Name", "Please enter a valid group name.", "OK");
                    return;
                }

                onCreate?.Invoke(safe);
                Close();
            }

            if (GUILayout.Button("Cancel", GUILayout.Height(26)))
            {
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
