#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace OSK
{
    public class LocalizationTableWindow : EditorWindow
    {
        private Dictionary<(int row, int col), Dictionary<string, string>> placeholderMapForTable;
        public TextAsset fileTranslateCsv;

        private List<string[]> rows = new List<string[]>();
        private string loadedPath = null;
        private string resourcesLoadPath = null;
        
        private Vector2 scroll;
        private List<float> columnWidths = new List<float>();
        
        private int maxColsToShow = 40;
        private float defaultColWidth = 220f;
        private bool dirty = false;

        private float translateDelay = 0.12f;
        private int sourceColumnIndex = 1;

        [MenuItem("OSK-Framework/Localization/Window")]
        public static void OpenWindow()
        {
            var w = GetWindow<LocalizationTableWindow>("Window");
            w.minSize = new Vector2(700, 360);
        }

        private void OnEnable()
        {
            // default empty table
            if (rows == null || rows.Count == 0)
            {
                rows = new List<string[]>();
                rows.Add(new[] { "key", "English" });
                EnsureColumnWidths();
            }

            TryLoadResourcesPathFromMainConfig();
            EnsureRowsFromFileIfAssigned();
        }

        private void EnsureRowsFromFileIfAssigned()
        {
            if (fileTranslateCsv != null)
            {
                // Load file into rows and treat this as source of truth
                rows = LoadCsvFromTextAsset(fileTranslateCsv);

                // update metadata so save works as expected
                loadedPath =
                    AssetDatabase.GetAssetPath(fileTranslateCsv); // absolute asset path in project (Assets/...)
                resourcesLoadPath = null; // we loaded via asset, not Resources.Load

                // reset helpers
                EnsureColumnWidths();
                dirty = false;
            }
        }
        
        private string googleSheetID;

        private void DrawGoogleSheetDownloader()
        {
            GUILayout.Space(10);
            GUILayout.Label("Google Sheet → CSV Downloader", EditorStyles.boldLabel);

            // Load saved ID
            if (string.IsNullOrEmpty(googleSheetID))
                googleSheetID = EditorPrefs.GetString("OSK_Localization_GSheetID", "");

            // Field nhập ID
            EditorGUILayout.BeginHorizontal();
            googleSheetID = EditorGUILayout.TextField("Google Sheet ID:", googleSheetID);
            if (GUILayout.Button("Save ID", GUILayout.Width(80)))
                EditorPrefs.SetString("OSK_Localization_GSheetID", googleSheetID);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            // NÚT DOWNLOAD
            if (GUILayout.Button("⬇ Load CSV from Google Sheet", GUILayout.Height(28)))
            {
                if (string.IsNullOrEmpty(googleSheetID))
                {
                    EditorUtility.DisplayDialog("Error", "Google Sheet ID is empty.", "OK");
                    return;
                }
                DownloadCsvFromGoogleSheet(googleSheetID);
            }
        }
        
        private async void DownloadCsvFromGoogleSheet(string sheetID)
        {
            string url = $"https://docs.google.com/spreadsheets/d/{sheetID}/export?format=csv";

            var req = UnityWebRequest.Get(url);
            var asyncOp = req.SendWebRequest();

            // Progress bar
            while (!asyncOp.isDone)
            {
                EditorUtility.DisplayProgressBar(
                    "Loading CSV",
                    "Fetching data from Google Sheet...",
                    asyncOp.progress
                );
                await Task.Delay(50);
            }
            EditorUtility.ClearProgressBar();

#if UNITY_2020_1_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
    if (req.isNetworkError || req.isHttpError)
#endif
            {
                EditorUtility.DisplayDialog("Download Error", req.error, "OK");
                return;
            }

            string csv = req.downloadHandler.text;
            if (string.IsNullOrEmpty(csv))
            {
                EditorUtility.DisplayDialog("Error", "CSV is empty.", "OK");
                return;
            }

            // Parse and load into editor rows
            ParseCsvText(csv);
            loadedPath = null;
            resourcesLoadPath = null;
            dirty = true;
            EnsureColumnWidths();

            EditorUtility.DisplayDialog("Success", "Loaded CSV successfully!", "OK");
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();
            GUILayout.Space(6);
            //DragCsvToOpenArea();
            DrawGoogleSheetDownloader();
            
            fileTranslateCsv =
                (TextAsset)EditorGUILayout.ObjectField("Translate CSV File", fileTranslateCsv, typeof(TextAsset),
                    false);
            if (GUILayout.Button("Load From Selected File"))
            {
                EnsureRowsFromFileIfAssigned();
            }

            GUILayout.Space(6);

            if (rows == null || rows.Count == 0)
            {
                EditorGUILayout.HelpBox("No CSV loaded. Use Open CSV or drag a CSV file here.", MessageType.Info);
                return;
            }

            EnsureColumnWidths();

            scroll = EditorGUILayout.BeginScrollView(scroll);

            // Header
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(" ", GUILayout.Width(26)); // space for delete button column
            GUILayout.Label("STT", GUILayout.Width(40));

            for (int c = 0; c < rows[0].Length && c < maxColsToShow; c++)
            {
                DrawHeader(c);
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);

            // Rows
            for (int r = 0; r < rows.Count; r++)
            {
                EditorGUILayout.BeginHorizontal();

                // delete button for non-header rows
                if (r == 0)
                {
                    GUILayout.Space(22);
                }
                else
                {
                    if (GUILayout.Button("X", GUILayout.Width(22)))
                    {
                        if (EditorUtility.DisplayDialog("Delete Row", $"Delete row {r}?", "Yes", "No"))
                        {
                            rows.RemoveAt(r);
                            dirty = true;
                            SaveStateAfterEdit();
                            Repaint();
                            return; // stop drawing further (rows changed)
                        }
                    }
                }

                GUILayout.Space(-5);
                // STT label
                GUIStyle sttStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
                GUILayout.Label(r == 0 ? "Key Code " : r.ToString(), sttStyle, GUILayout.Width(60));

                // cells
                for (int c = 0; c < rows[0].Length && c < maxColsToShow; c++)
                {
                    EnsureRowCols(r, c + 1);
                    string old = rows[r][c] ?? "";
                    float w = columnWidths.Count > c ? columnWidths[c] : defaultColWidth;
                    string nv = EditorGUILayout.TextField(old, GUILayout.Width(w));
                    if (nv != old)
                    {
                        rows[r][c] = nv;
                        dirty = true;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            // Footer controls
            EditorGUILayout.BeginHorizontal();
            var gui = new[]
            {
                GUILayout.Width(100),
                GUILayout.Height(30)
            };
            
            if (GUILayout.Button("Add Row", gui)) AddRow(); 
            if (GUILayout.Button("Add Column", gui))  AddColumn(); 
            if (GUILayout.Button("Delete Column", gui)) DeleteSelectedColumnPrompt(); 
            if (GUILayout.Button("Clean All", gui))
            {
                rows.Clear();
                dirty = true;
            }

            if (GUILayout.Button("Gen Enum Key", gui))
            {
                GenKeyEnumFromCurrentTable();
            }
            
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save", gui)) SaveCsv(); 
            if (GUILayout.Button("Save As...", gui)) SaveAsCsv(); 
            if (GUILayout.Button("Export JSON", gui)) ExportJsonSingleFile();
   
            EditorGUILayout.EndHorizontal();

            // show dirty indicator small
            if (dirty)
            {
                EditorGUILayout.HelpBox("Unsaved changes", MessageType.Warning);
            }
        }

        // ---------------- UI helpers ----------------

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Link Language Code", EditorStyles.toolbarButton))
            {
                Application.OpenURL(
                    "https://github.com/O-S-K/OSK-Framework/blob/main/Runtime/Scripts/Core/Localization/LanguageCountryCodeMapper.cs");
            }
            

            GUILayout.FlexibleSpace();
            // simple settings
            GUILayout.Label("SourceCol:", GUILayout.Width(70));
            sourceColumnIndex = EditorGUILayout.IntField(sourceColumnIndex, GUILayout.Width(40));
            GUILayout.Label("Delay(s):", GUILayout.Width(60));
            translateDelay = EditorGUILayout.Slider(translateDelay, 0f, 1f, GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();
        }
        
        private void GenKeyEnumFromCurrentTable()
        {
            if (rows == null || rows.Count < 2)
            {
                EditorUtility.DisplayDialog("Generate Enum", "No data to generate.", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanel("Save Key Enum", Application.dataPath, "LocalizationKeys", "cs");
            if (string.IsNullOrEmpty(path)) return;

            var sb = new StringBuilder();
            sb.AppendLine("public enum LocalizationKey");
            sb.AppendLine("{");
            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row == null || row.Length == 0) continue;
                string key = row.Length > 0 ? row[0]?.Trim() : "";
                if (string.IsNullOrEmpty(key)) continue;

                sb.AppendLine($"    {key},");
            }
            sb.AppendLine("}");

            try
            {
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Generate Enum", "Generated enum to:\n" + path, "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Generate Enum", "Failed to write enum: " + ex.Message, "OK");
            }
        }

        private void DragCsvToOpenArea()
        {
            var rect = GUILayoutUtility.GetRect(0, 36, GUILayout.ExpandWidth(true));
            GUI.Box(rect, "Drag & Drop CSV file HERE to OPEN (resources auto-detect)", EditorStyles.helpBox);
            var evt = Event.current;
            if (!rect.Contains(evt.mousePosition)) return;

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                bool hasCsv = DragAndDrop.paths.Any(p => p.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));
                DragAndDrop.visualMode = hasCsv ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    string csvFile =
                        DragAndDrop.paths.FirstOrDefault(p => p.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(csvFile))
                    {
                        string res = DetectResourcesPath(csvFile);
                        if (!string.IsNullOrEmpty(res))
                        {
                            LoadFromResources(res);
                        }
                        else
                        {
                            LoadFromFile(csvFile);
                        }

                        Repaint();
                    }

                    evt.Use();
                }
            }
        }

        private void DrawHeader(int c)
        {
            float w = columnWidths.Count > c ? columnWidths[c] : defaultColWidth;
            Rect rect = GUILayoutUtility.GetRect(w, 40, GUILayout.Width(w));
            GUI.Box(rect, GUIContent.none);
            Rect inner = new Rect(rect.x + 10, rect.y + 2, rect.width - 80, rect.height);
            string title = rows[0].Length > c ? rows[0][c] : $"Col{c}";
            GUI.Label(inner, title);

            // width controls
            if (GUI.Button(new Rect(inner.x + inner.width, inner.y, 22, 16), "-"))
                columnWidths[c] = Mathf.Max(80f, w - 50f);
            if (GUI.Button(new Rect(inner.x + inner.width + 22, inner.y, 22, 16), "+"))
                columnWidths[c] = Mathf.Min(900f, w + 20f);

            // delete column button
            if (GUI.Button(new Rect(inner.x + inner.width + 44, inner.y, 22, 16), "X"))
            {
                if (EditorUtility.DisplayDialog("Delete Column",
                        $"Delete column '{title}'? This will remove its cells.", "Delete", "Cancel"))
                {
                    DeleteColumnAt(c);
                }
            }

            // translate column button
            if (GUI.Button(new Rect(inner.x + inner.width, inner.y + 18, 66, 18), "Translate"))
            {
                string code = LanguageCountryCodeMapper.GetCountryCode(title);
                _ = TranslateColumnInteractive(c, code);
            }
        }

        // ---------------- Basic operations ----------------

        private void AddRow()
        {
            int cols = rows.Count > 0 ? rows[0].Length : 2;
            var row = new string[cols];
            for (int i = 0; i < cols; i++) row[i] = "";
            rows.Add(row);
            dirty = true;
            EnsureColumnWidths();
        }

        private void AddColumn()
        {
            int old = rows[0].Length;
            for (int r = 0; r < rows.Count; r++)
            {
                var nr = new string[old + 1];
                Array.Copy(rows[r], nr, old);
                nr[old] = "";
                rows[r] = nr;
            }

            rows[0][old] = $"Col{old}";
            columnWidths.Add(defaultColWidth);
            dirty = true;
        }

        private void DeleteColumnAt(int col)
        {
            if (col < 0 || col >= rows[0].Length) return;
            for (int r = 0; r < rows.Count; r++)
            {
                var old = rows[r];
                var nr = new string[old.Length - 1];
                int di = 0;
                for (int c = 0; c < old.Length; c++)
                {
                    if (c == col) continue;
                    nr[di++] = old[c];
                }

                rows[r] = nr;
            }

            if (col < columnWidths.Count) columnWidths.RemoveAt(col);
            dirty = true;
        }

        private void DeleteSelectedColumnPrompt()
        {
            int idx = EditorGUILayout.IntField("Delete column index:", 1);
            if (idx >= 0 && idx < rows[0].Length)
            {
                if (EditorUtility.DisplayDialog("Delete Column", $"Delete column at index {idx} ({rows[0][idx]})?",
                        "Delete", "Cancel"))
                {
                    DeleteColumnAt(idx);
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Delete Column", "Invalid index.", "OK");
            }
        }

        private void EnsureRowCols(int rowIndex, int colsNeeded)
        {
            if (rowIndex < 0 || rowIndex >= rows.Count) return;
            var r = rows[rowIndex];
            if (r.Length >= colsNeeded) return;
            var nr = new string[colsNeeded];
            Array.Copy(r, nr, r.Length);
            for (int i = r.Length; i < colsNeeded; i++) nr[i] = "";
            rows[rowIndex] = nr;
        }

        private void EnsureColumnWidths()
        {
            if (rows == null || rows.Count == 0) return;
            int cols = rows[0].Length;
            while (columnWidths.Count < cols) columnWidths.Add(defaultColWidth);
            while (columnWidths.Count > cols) columnWidths.RemoveAt(columnWidths.Count - 1);
        }

        // ---------------- Load / Save ---------------- 

        private void OpenCsvWithDetect()
        {
            string path = EditorUtility.OpenFilePanel("Open localization CSV", Application.dataPath, "csv");
            if (string.IsNullOrEmpty(path)) return;

            string res = DetectResourcesPath(path);
            if (!string.IsNullOrEmpty(res))
            {
                if (EditorUtility.DisplayDialog("Load via Resources?",
                        $"File looks like inside Resources at '{res}'. Load via Resources.Load?", "Yes",
                        "No (load file)"))
                {
                    LoadFromResources(res);
                    return;
                }
            }

            // ask to copy into Resources
            if (EditorUtility.DisplayDialog("Copy to Resources?",
                    "File is not inside Assets/Resources. Copy into Assets/Resources/Localization and load from there?",
                    "Copy & Load", "Load From File"))
            {
                try
                {
                    var fileName = Path.GetFileName(path);
                    var destDir = Path.Combine(Application.dataPath, "Resources", "Localization");
                    if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
                    var dest = Path.Combine(destDir, fileName);
                    File.Copy(path, dest, true);
                    AssetDatabase.Refresh();
                    string rel = "Localization/" + Path.GetFileNameWithoutExtension(fileName);
                    LoadFromResources(rel);
                    return;
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Copy failed", "Failed to copy: " + ex.Message, "OK");
                }
            }

            LoadFromFile(path);
        }

        private void LoadFromFile(string path)
        {
            try
            {
                var text = File.ReadAllText(path, Encoding.UTF8);
                ParseCsvText(text);
                loadedPath = path;
                resourcesLoadPath = null;
                dirty = false;
                EnsureColumnWidths();
                Debug.Log("[LocalizationTable] Loaded CSV from file: " + path);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                EditorUtility.DisplayDialog("Error", "Failed to load CSV: " + e.Message, "OK");
            }
        }

        private void LoadFromResources(string resourcesRelativePath)
        {
            try
            {
                var ta = Resources.Load<TextAsset>(resourcesRelativePath);
                if (ta == null)
                {
                    EditorUtility.DisplayDialog("Load from Resources",
                        $"Resources.Load(\"{resourcesRelativePath}\") returned null.", "OK");
                    return;
                }

                ParseCsvText(ta.text);
                loadedPath = null;
                resourcesLoadPath = resourcesRelativePath;
                dirty = false;
                EnsureColumnWidths();
                Debug.Log("[LocalizationTable] Loaded CSV from Resources: " + resourcesRelativePath);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                EditorUtility.DisplayDialog("Error", "Failed to load CSV from Resources: " + e.Message, "OK");
            }
        }

        private void ParseCsvText(string text)
        {
            rows.Clear();
            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                rows.Add(ParseCsvLine(line));
            }

            if (rows.Count == 0) rows.Add(new string[] { "key", "English" });
        }

        private void SaveCsv()
        {
            if (!string.IsNullOrEmpty(resourcesLoadPath))
            {
                string full = Path.Combine(Application.dataPath, "Resources", resourcesLoadPath) + ".csv";
                var dir = Path.GetDirectoryName(full);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                WriteCsvToFile(full);
                AssetDatabase.Refresh();
                TryInvokeLoadAllFromMainLocalization();
                dirty = false;
                return;
            }

            if (!string.IsNullOrEmpty(loadedPath))
            {
                WriteCsvToFile(loadedPath);
                AssetDatabase.Refresh();
                TryInvokeLoadAllFromMainLocalization();
                dirty = false;
                return;
            }

            SaveAsCsv();
        }

        private void SaveAsCsv()
        {
            string outPath =
                EditorUtility.SaveFilePanel("Save localization CSV", Application.dataPath, "localization", "csv");
            if (string.IsNullOrEmpty(outPath)) return;
            WriteCsvToFile(outPath);
            loadedPath = outPath;
            AssetDatabase.Refresh();
            TryInvokeLoadAllFromMainLocalization();
            dirty = false;
        }

        private void WriteCsvToFile(string path)
        {
            try
            {
                var sb = new StringBuilder();
                foreach (var row in rows) sb.AppendLine(JoinCsvRow(row));
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                EditorUtility.DisplayDialog("Saved", "CSV saved to: " + path, "OK");
                Debug.Log("[LocalizationTable] Saved CSV: " + path);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                EditorUtility.DisplayDialog("Error", "Failed to save CSV: " + e.Message, "OK");
            }
        }

        // ---------------- Export JSON (single file) ----------------

        private void ExportJsonSingleFile()
        {
            if (rows == null || rows.Count < 2)
            {
                EditorUtility.DisplayDialog("Export JSON", "No data to export.", "OK");
                return;
            }

            var header = rows[0];
            var langCols = new List<int>();
            var spriteCols = new List<int>();
            var audioCols = new List<int>();
            for (int c = 0; c < header.Length; c++)
            {
                var h = header[c].Trim();
                if (c == 0) continue;
                if (Enum.TryParse(typeof(SystemLanguage), h, true, out var _)) langCols.Add(c);
                else
                {
                    if (h.StartsWith("sprite", StringComparison.OrdinalIgnoreCase)) spriteCols.Add(c);
                    if (h.StartsWith("audio", StringComparison.OrdinalIgnoreCase)) audioCols.Add(c);
                }
            }

            string path = EditorUtility.SaveFilePanel("Save localization JSON (single file)", Application.dataPath,
                "localization_all", "json");
            if (string.IsNullOrEmpty(path)) return;

            var sb = new StringBuilder();
            sb.AppendLine("{");
            int itemCount = 0;
            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row == null || row.Length == 0) continue;
                string key = row.Length > 0 ? row[0]?.Trim() : "";
                if (string.IsNullOrEmpty(key)) continue;

                if (itemCount > 0) sb.AppendLine(",");

                sb.Append("  ");
                sb.Append(JsonEscapeString(key));
                sb.AppendLine(": {");

                sb.AppendLine("    \"texts\": {");
                for (int i = 0; i < langCols.Count; i++)
                {
                    int col = langCols[i];
                    string val = col < row.Length ? row[col] : "";
                    sb.Append("      ");
                    sb.Append(JsonEscapeString(rows[0][col]));
                    sb.Append(": ");
                    sb.Append(JsonEscapeString(val));
                    if (i < langCols.Count - 1) sb.AppendLine(",");
                    else sb.AppendLine();
                }

                sb.AppendLine("    }");

                sb.AppendLine("  }");
                itemCount++;
            }

            sb.AppendLine();
            sb.AppendLine("}");

            try
            {
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Export JSON", "Exported JSON to:\n" + path, "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Export JSON", "Failed to write JSON: " + ex.Message, "OK");
            }
        }

        private string JsonEscapeString(string s)
        {
            if (s == null) s = "";
            var sb = new StringBuilder();
            sb.Append("\"");
            foreach (var ch in s)
            {
                switch (ch)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '\"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (ch < 32) sb.AppendFormat("\\u{0:X4}", (int)ch);
                        else sb.Append(ch);
                        break;
                }
            }

            sb.Append("\"");
            return sb.ToString();
        }

        // ---------------- Translate (column) ----------------

        private async Task TranslateColumnInteractive(int colIndex, string targetLangCode)
        {
            if (colIndex < 0 || colIndex >= rows[0].Length)
            {
                EditorUtility.DisplayDialog("Translate", "Invalid column", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("Translate Column",
                    $"Translate column '{rows[0][colIndex]}' to '{targetLangCode}' ?", "Translate", "Cancel"))
            {
                await TranslateWholeColumn(colIndex, targetLangCode);
            }
        }

        private async Task TranslateWholeColumn(int colIndex, string targetLangCode)
        {
            if (rows == null || rows.Count <= 1) return;
            // ensure placeholder map container exists
            if (placeholderMapForTable == null)
                placeholderMapForTable = new Dictionary<(int row, int col), Dictionary<string, string>>();

            int total = rows.Count - 1;
            int done = 0;
            for (int r = 1; r < rows.Count; r++)
            {
                string src = rows[r].Length > sourceColumnIndex ? rows[r][sourceColumnIndex] : "";
                if (string.IsNullOrEmpty(src))
                {
                    EnsureRowCols(r, colIndex + 1);
                    rows[r][colIndex] = "";
                    done++;
                    EditorUtility.DisplayProgressBar("Translating column", $"{done}/{total}", (float)done / total);
                    continue;
                }

                // skip existing translation if you want (comment out to force overwrite)
                if (!string.IsNullOrEmpty(rows[r][colIndex]))
                {
                    done++;
                    EditorUtility.DisplayProgressBar("Translating column", $"{done}/{total}", (float)done / total);
                    continue;
                }

                // ----- PREPROCESS: replace tags with placeholders -----
                Dictionary<string, string> cellPlaceholders;
                string pre = LocalizationTagPreserver.PreprocessCell(src, out cellPlaceholders);
                // save map so we can postprocess later (optional, but helpful if you batch)
                placeholderMapForTable[(r, colIndex)] = cellPlaceholders;

                // ----- TRANSLATE the preprocessed string -----
                string translatedPre = await TranslateFree(pre, targetLangCode); // use your existing TranslateFree

                // ----- POSTPROCESS: restore original tags into translated string -----
                string final = LocalizationTagPreserver.PostprocessCell(translatedPre, cellPlaceholders);

                // ----- WRITE BACK -----
                EnsureRowCols(r, colIndex + 1);
                rows[r][colIndex] = final ?? "";

                done++;
                EditorUtility.DisplayProgressBar("Translating column", $"{done}/{total}", (float)done / total);

                // small delay to avoid rate limit
                await Task.Delay(TimeSpan.FromSeconds(translateDelay));
            }

            EditorUtility.ClearProgressBar();
            dirty = true;
            SaveStateAfterEdit();
            TryInvokeSaveToResourcesIfConfigured();
            EditorUtility.DisplayDialog("Translate", "Column translate finished", "OK");
        }

        private async Task<string> TranslateFree(string text, string to)
        {
            string url =
                $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl={UnityWebRequest.EscapeURL(to)}&dt=t&q={UnityWebRequest.EscapeURL(text)}";
            using (var req = UnityWebRequest.Get(url))
            {
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Delay(10);

#if UNITY_2020_1_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                {
                    Debug.LogWarning("[LocalizationTable] translate request failed: " + req.error);
                    return "";
                }

                string raw = req.downloadHandler.text;
                try
                {
                    int idx = raw.IndexOf("[[[", StringComparison.Ordinal);
                    if (idx < 0) idx = raw.IndexOf('[');
                    int p = raw.IndexOf("\"", idx);
                    if (p < 0) return "";
                    int q = raw.IndexOf("\"", p + 1);
                    if (q <= p) return "";
                    string t = raw.Substring(p + 1, q - p - 1);
                    return System.Net.WebUtility.HtmlDecode(t);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[LocalizationTable] parse translate response: " + ex.Message + " raw=" + raw);
                    return "";
                }
            }
        }


        // ---------------- Helpers ----------------

        private string DetectResourcesPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return null;
            fullPath = fullPath.Replace("\\", "/");
            string project = Application.dataPath.Replace("\\", "/");
            string resFolder = project + "/Resources/";
            if (fullPath.StartsWith(resFolder, StringComparison.OrdinalIgnoreCase))
            {
                string rel = fullPath.Substring(resFolder.Length);
                if (rel.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)) rel = rel.Substring(0, rel.Length - 4);
                return rel;
            }

            return null;
        }

        private void SaveStateAfterEdit()
        {
            // mark dirty and optionally update UI or other integrations
            dirty = true;
        }

        private void TryInvokeSaveToResourcesIfConfigured()
        {
            if (!string.IsNullOrEmpty(resourcesLoadPath))
            {
                // save into resources path automatically
                string full = Path.Combine(Application.dataPath, "Resources", resourcesLoadPath) + ".csv";
                var dir = Path.GetDirectoryName(full);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                WriteCsvToFile(full);
                AssetDatabase.Refresh();
                TryInvokeLoadAllFromMainLocalization();
                dirty = false;
                Debug.Log("[LocalizationTable] Auto-saved CSV to Resources: " + full);
            }
        }

        private void TryLoadResourcesPathFromMainConfig()
        {
            try
            {
                var mainType = Type.GetType("Main,Assembly-CSharp");
                if (mainType == null) return;
                var instanceField = mainType.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceField == null) return;
                var mainInstance = instanceField.GetValue(null);
                if (mainInstance == null) return;
                var cfgField = mainType.GetField("configInit", BindingFlags.Public | BindingFlags.Instance);
                if (cfgField == null) return;
                var cfg = cfgField.GetValue(mainInstance);
                if (cfg == null) return;
                var pathField = cfg.GetType().GetField("path", BindingFlags.Public | BindingFlags.Instance);
                if (pathField == null) return;
                var pathObj = pathField.GetValue(cfg);
                if (pathObj == null) return;
                var loadFileCsvField = pathObj.GetType()
                    .GetField("pathLoadFileCsv", BindingFlags.Public | BindingFlags.Instance);
                if (loadFileCsvField == null) return;
                var p = loadFileCsvField.GetValue(pathObj) as string;
                if (!string.IsNullOrEmpty(p) && p.StartsWith("Resources/"))
                    resourcesLoadPath = p["Resources/".Length..];
            }
            catch
            {
            }
        }

        private void TryInvokeLoadAllFromMainLocalization()
        {
            try
            {
                var mainType = Type.GetType("Main,Assembly-CSharp");
                if (mainType == null) return;
                var instanceField = mainType.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceField == null) return;
                var mainInstance = instanceField.GetValue(null);
                if (mainInstance == null) return;
                var locField = mainType.GetField("Localization", BindingFlags.Public | BindingFlags.Instance);
                var loc = locField?.GetValue(mainInstance);
                if (loc == null) return;

                var method = loc.GetType().GetMethod("LoadAllFromCsv", BindingFlags.Public | BindingFlags.Instance);
                method?.Invoke(loc, null);
            }
            catch
            {
            }
        }

        // CSV helpers
        private List<string[]> LoadCsvFromTextAsset(TextAsset ta)
        {
            var list = new List<string[]>();
            if (ta == null) return list;
            var lines = ta.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                list.Add(ParseCsvLine(line));
            }

            if (list.Count == 0) list.Add(new string[] { "key", "English" });
            return list;
        }

        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var sb = new StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else sb.Append(c);
            }

            result.Add(sb.ToString());
            return result.ToArray();
        }

        private string JoinCsvRow(string[] row)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < row.Length; i++)
            {
                string cell = row[i] ?? "";
                bool needQuote = cell.Contains(",") || cell.Contains("\"") || cell.Contains("\n") ||
                                 cell.Contains("\r");
                if (needQuote)
                {
                    cell = cell.Replace("\"", "\"\"");
                    sb.Append('"').Append(cell).Append('"');
                }
                else sb.Append(cell);

                if (i < row.Length - 1) sb.Append(',');
            }

            return sb.ToString();
        }
    }
}
#endif