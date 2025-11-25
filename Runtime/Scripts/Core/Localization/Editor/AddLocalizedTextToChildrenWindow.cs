#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OSK
{
    /// <summary>
    /// AddLocalizedTextToChildrenWindow - updated
    /// - Add LocalizedText to children (previous behavior)
    /// - Find children that HAVE text components but MISSING LocalizedText
    /// - Optionally add missing LocalizedText
    /// - Optionally fill empty 'key' fields on LocalizedText using rules:
    ///     * FromTextContent (take text content)
    ///     * FromGameObjectPath (e.g. Root/Child/MyText)
    /// - Export found list to CSV
    /// - Uses Undo so changes are revertable
    /// </summary>
    public class AddLocalizedTextToChildrenWindow : EditorWindow
    {
        private GameObject root;
        private bool includeInactive = false;
        private bool setKeyFromExistingText = true;
        private bool overwriteExisting = false;
        private bool pingCreatedObjects = true;

        // new features
        private enum KeyFillRule
        {
            FromTextContent,
            FromGameObjectPath
        }

        private KeyFillRule keyRule = KeyFillRule.FromTextContent;
        private bool autoAddMissing = false;
        private bool autoFillEmptyKeys = false;
        private Vector2 scroll;
        private List<GameObject> lastFoundMissing = new List<GameObject>();

        [MenuItem("OSK-Framework/Localization/Add LocalizedText")]
        public static void OpenWindow()
        {
            var w = GetWindow<AddLocalizedTextToChildrenWindow>("Add LocalizedText");
            w.minSize = new Vector2(520, 360);
        }

        private void OnGUI()
        {
            GUILayout.Label("Add / Find LocalizedText to children", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical("box");
            root = (GameObject)EditorGUILayout.ObjectField("Root GameObject", root, typeof(GameObject), true);
            includeInactive = EditorGUILayout.ToggleLeft("Include inactive children", includeInactive);
            overwriteExisting =
                EditorGUILayout.ToggleLeft("Overwrite existing LocalizedText if present", overwriteExisting);
            pingCreatedObjects = EditorGUILayout.ToggleLeft("Ping created objects after run", pingCreatedObjects);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Basic actions
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add LocalizedText to Children", GUILayout.Height(30)))
            {
                if (root == null) EditorUtility.DisplayDialog("No root", "Please assign a root GameObject.", "OK");
                else DoAddLocalizedText();
            }

            if (GUILayout.Button("Remove LocalizedText from Children", GUILayout.Height(30)))
            {
                if (root == null) EditorUtility.DisplayDialog("No root", "Please assign a root GameObject.", "OK");
                else DoRemoveLocalizedText();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // New: Find missing + auto add/fill options
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Find & Auto-Fix", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Find Missing LocalizedText", GUILayout.Width(200)))
            {
                if (root == null) EditorUtility.DisplayDialog("No root", "Please assign a root GameObject.", "OK");
                else FindMissingLocalizedText();
            }

            if (GUILayout.Button("Export Found To CSV", GUILayout.Width(160)))
            {
                ExportFoundMissingToCsv();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            autoAddMissing =
                EditorGUILayout.ToggleLeft("Auto-add LocalizedText to missing (when Found)", autoAddMissing);
            EditorGUILayout.Space();

            GUILayout.Label("Key fill options", EditorStyles.boldLabel);
            keyRule = (KeyFillRule)EditorGUILayout.EnumPopup("Fill rule:", keyRule);
            autoFillEmptyKeys =
                EditorGUILayout.ToggleLeft("Auto-fill empty keys (when adding or found)", autoFillEmptyKeys);

            // small note
            EditorGUILayout.HelpBox(
                "Key fill rule:\n- FromTextContent: use current text value as key (sanitized)\n- FromGameObjectPath: use hierarchy path 'Root/Child/Name'\n",
                MessageType.Info);

            // Fill button
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Fill Empty Keys For Found", GUILayout.Height(28)))
            {
                if (lastFoundMissing == null || lastFoundMissing.Count == 0)
                {
                    EditorUtility.DisplayDialog("No results",
                        "No previously found objects. Click 'Find Missing LocalizedText' first.", "OK");
                }
                else
                {
                    FillEmptyKeysForFound();
                }
            }

            if (GUILayout.Button("Add & Fill Found (one-click)", GUILayout.Height(28)))
            {
                if (root == null) EditorUtility.DisplayDialog("No root", "Please assign a root GameObject.", "OK");
                else
                {
                    FindMissingLocalizedText();
                    if (lastFoundMissing.Count > 0)
                    {
                        DoAddLocalizedTextToList(lastFoundMissing, overwriteExisting, autoFillEmptyKeys, keyRule);
                    }
                    else EditorUtility.DisplayDialog("Nothing to add", "No missing objects found.", "OK");
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Summary + results
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Last found results:", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(120));
            if (lastFoundMissing != null && lastFoundMissing.Count > 0)
            {
                foreach (var g in lastFoundMissing)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Ping", GUILayout.Width(40)))
                        EditorGUIUtility.PingObject(g);
                    GUILayout.Label(GetFriendlyObjectLabel(g), EditorStyles.label);
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label("No found objects yet.", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ------- Helpers -------

        private void DoAddLocalizedText()
        {
            var transforms = root.GetComponentsInChildren<Transform>(includeInactive);
            var created = new List<UnityEngine.Object>();
            int added = 0, skipped = 0, overwritten = 0;

            foreach (var t in transforms)
            {
                if (t == null) continue;
                var go = t.gameObject;
                var uiText = go.GetComponent<Text>();
                var tmp = go.GetComponent<TMP_Text>();
                var textMesh = go.GetComponent<TextMesh>();
                if (uiText == null && tmp == null && textMesh == null)
                {
                    skipped++;
                    continue;
                }

                var existing = go.GetComponent<LocalizedText>();
                if (existing != null && !overwriteExisting)
                {
                    skipped++;
                    continue;
                }

                if (existing != null && overwriteExisting)
                {
                    DestroyImmediate(existing, true);
                    overwritten++;
                }

                var addedComp = Undo.AddComponent<LocalizedText>(go);
                added++;
                created.Add(addedComp);

                // optionally set key from text content
                if (setKeyFromExistingText)
                {
                    string extracted = (uiText != null)
                        ? uiText.text
                        : (tmp != null ? tmp.text : (textMesh != null ? textMesh.text : ""));
                    if (!string.IsNullOrEmpty(extracted))
                    {
                        string key = SanitizeKey(extracted);
                        var so = new SerializedObject(addedComp);
                        var keyProp = so.FindProperty("key");
                        if (keyProp != null)
                        {
                            keyProp.stringValue = key;
                            so.ApplyModifiedProperties();
                        }
                    }
                }

                // TMP: set isUpdateOnStart = true
                if (tmp != null)
                {
                    var so = new SerializedObject(addedComp);
                    var boolProp = so.FindProperty("isUpdateOnStart");
                    if (boolProp != null)
                    {
                        boolProp.boolValue = true;
                        so.ApplyModifiedProperties();
                    }
                }
            }

            // summary
            Debug.Log(
                $"[AddLocalizedText] Added: {added}, Overwritten: {overwritten}, Skipped(non-text or kept): {skipped}. Created components: {created.Count}");
            if (pingCreatedObjects && created.Count > 0)
            {
                EditorGUIUtility.PingObject(created[0]);
                Selection.objects = created.ToArray();
            }

            if (!Application.isPlaying) UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            EditorUtility.DisplayDialog("Done", $"Added: {added}\nOverwritten: {overwritten}\nSkipped: {skipped}",
                "OK");
        }

        private void DoRemoveLocalizedText()
        {
            var transforms = root.GetComponentsInChildren<Transform>(includeInactive);
            int removed = 0;
            var removedObjs = new List<UnityEngine.Object>();
            foreach (var t in transforms)
            {
                var go = t.gameObject;
                var existing = go.GetComponent<LocalizedText>();
                if (existing != null)
                {
                    Undo.DestroyObjectImmediate(existing);
                    removed++;
                    removedObjs.Add(go);
                }
            }

            Debug.Log($"[AddLocalizedText] Removed {removed} LocalizedText components.");
            if (removed > 0) EditorGUIUtility.PingObject(removedObjs[0]);
            if (!Application.isPlaying) UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            EditorUtility.DisplayDialog("Done", $"Removed {removed} LocalizedText components.", "OK");
        }

        // Find children that have text components but missing LocalizedText
        private void FindMissingLocalizedText()
        {
            lastFoundMissing.Clear();
            if (root == null)
            {
                EditorUtility.DisplayDialog("No root", "Please assign a root GameObject.", "OK");
                return;
            }

            var transforms = root.GetComponentsInChildren<Transform>(includeInactive);
            foreach (var t in transforms)
            {
                if (t == null) continue;
                var go = t.gameObject;
                var hasText = (go.GetComponent<Text>() != null) || (go.GetComponent<TMP_Text>() != null) ||
                              (go.GetComponent<TextMesh>() != null);
                if (!hasText) continue;
                if (go.GetComponent<LocalizedText>() == null)
                {
                    lastFoundMissing.Add(go);
                }
            }

            EditorUtility.DisplayDialog("Find complete",
                $"Found {lastFoundMissing.Count} objects missing LocalizedText.", "OK");
            if (lastFoundMissing.Count > 0)
            {
                Selection.objects = lastFoundMissing.ToArray();
                EditorGUIUtility.PingObject(lastFoundMissing[0]);
            }

            // if auto-add enabled, add them now
            if (autoAddMissing && lastFoundMissing.Count > 0)
            {
                DoAddLocalizedTextToList(lastFoundMissing, overwriteExisting, autoFillEmptyKeys, keyRule);
            }
        }

        // Add LocalizedText to a specific list of GameObjects (used for found list)
        private void DoAddLocalizedTextToList(List<GameObject> list, bool overwrite, bool fillEmptyKeys,
            KeyFillRule rule)
        {
            var created = new List<UnityEngine.Object>();
            int added = 0, overwritten = 0;
            foreach (var go in list)
            {
                if (go == null) continue;
                var existing = go.GetComponent<LocalizedText>();
                if (existing != null && !overwrite) continue;
                if (existing != null && overwrite)
                {
                    Undo.DestroyObjectImmediate(existing);
                    overwritten++;
                }

                var comp = Undo.AddComponent<LocalizedText>(go);
                created.Add(comp);
                added++;

                // optionally fill key
                if (fillEmptyKeys)
                {
                    FillKeyForLocalizedTextOnGO(go, comp, rule);
                }

                // TMP detection
                if (go.GetComponent<TMP_Text>() != null)
                {
                    var so = new SerializedObject(comp);
                    var p = so.FindProperty("isUpdateOnStart");
                    if (p != null)
                    {
                        p.boolValue = true;
                        so.ApplyModifiedProperties();
                    }
                }
            }

            Debug.Log($"[AddLocalizedText] Added from list: {added}, overwritten: {overwritten}");
            if (created.Count > 0 && pingCreatedObjects)
            {
                Selection.objects = created.ToArray();
                EditorGUIUtility.PingObject(created[0]);
            }

            if (!Application.isPlaying) UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            EditorUtility.DisplayDialog("Done", $"Added: {added}\nOverwritten: {overwritten}", "OK");
        }

        // Fill empty keys for LocalizedText components found in lastFoundMissing
        private void FillEmptyKeysForFound()
        {
            if (lastFoundMissing == null || lastFoundMissing.Count == 0)
            {
                EditorUtility.DisplayDialog("No found", "No found results.", "OK");
                return;
            }

            int filled = 0;
            foreach (var go in lastFoundMissing)
            {
                if (go == null) continue;
                var comp = go.GetComponent<LocalizedText>();
                if (comp == null) continue; // skip non-added items
                var so = new SerializedObject(comp);
                var keyProp = so.FindProperty("key");
                if (keyProp == null) continue;
                if (!string.IsNullOrEmpty(keyProp.stringValue)) continue; // skip existing keys

                FillKeyForLocalizedTextOnGO(go, comp, keyRule);
                filled++;
            }

            EditorUtility.DisplayDialog("Fill keys", $"Filled keys for {filled} components.", "OK");
        }

        // helper: fill key on a single GO/comp according to rule
        private void FillKeyForLocalizedTextOnGO(GameObject go, LocalizedText comp, KeyFillRule rule)
        {
            string newKey = "";
            switch (rule)
            {
                case KeyFillRule.FromTextContent:
                    string content = "";
                    var uiText = go.GetComponent<Text>();
                    var tmp = go.GetComponent<TMP_Text>();
                    var tmesh = go.GetComponent<TextMesh>();
                    if (uiText != null) content = uiText.text;
                    else if (tmp != null) content = tmp.text;
                    else if (tmesh != null) content = tmesh.text;
                    newKey = SanitizeKey(content);
                    break;
                case KeyFillRule.FromGameObjectPath:
                    newKey = SanitizeKey(GetGameObjectPath(go));
                    break;
            }

            if (comp != null)
            {
                var so = new SerializedObject(comp);
                var keyProp = so.FindProperty("key");
                if (keyProp != null)
                {
                    keyProp.stringValue = newKey;
                    so.ApplyModifiedProperties();
                }
            }
        }

        // Export found missing list to CSV file (desktop)
        private void ExportFoundMissingToCsv()
        {
            if (lastFoundMissing == null || lastFoundMissing.Count == 0)
            {
                EditorUtility.DisplayDialog("No data", "No found items to export. Click Find first.", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanel("Export missing LocalizedText to CSV",
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "missing_localizedtext.csv", "csv");
            if (string.IsNullOrEmpty(path)) return;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("GameObjectPath,HasTextComponent,HasLocalizedText");
            foreach (var go in lastFoundMissing)
            {
                string p = GetGameObjectPath(go).Replace(",", "_");
                bool hasText = (go.GetComponent<Text>() != null) || (go.GetComponent<TMP_Text>() != null) ||
                               (go.GetComponent<TextMesh>() != null);
                bool hasLoc = go.GetComponent<LocalizedText>() != null;
                sb.AppendLine($"{p},{hasText},{hasLoc}");
            }

            File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
            EditorUtility.RevealInFinder(path);
            EditorUtility.DisplayDialog("Exported", $"Exported {lastFoundMissing.Count} rows to:\n{path}", "OK");
        }

        // Utility: sanitize key string
        private string SanitizeKey(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            string k = raw.Trim();
            k = k.Replace("\n", " ").Replace("\r", " ").Trim();
            // remove quotes and commas
            k = k.Replace("\"", "").Replace("'", "").Replace(",", " ");
            // truncate to reasonable length
            if (k.Length > 120) k = k.Substring(0, 120);
            // optionally replace spaces with underscores (optional)
            // k = Regex.Replace(k, @"\s+", "_");
            return k;
        }

        // Utility: get full path of GameObject
        private string GetGameObjectPath(GameObject go)
        {
            if (go == null) return "";
            string path = go.name;
            Transform t = go.transform;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }

            return path;
        }

        private string GetFriendlyObjectLabel(GameObject go)
        {
            return
                $"{GetGameObjectPath(go)}  (text:{(go.GetComponent<Text>() != null ? "UI.Text" : "")}{(go.GetComponent<TMP_Text>() != null ? " TMP" : "")}{(go.GetComponent<TextMesh>() != null ? " TextMesh" : "")})";
        }
    }
}
#endif