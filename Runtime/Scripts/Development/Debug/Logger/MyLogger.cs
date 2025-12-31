using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace OSK
{
    public class LogEntry
    {
        public string Message;
        public string Level;
        public int Line;
        public string File;
    }

    public class LogStats
    {
        public bool Enabled = true;
        public string FileName;
        public int LineNumber;
        public List<LogEntry> Entries = new List<LogEntry>();
        public int LogCount, WarningCount, ErrorCount;

        public void AddEntry(LogEntry entry)
        {
            // Chỉ giữ lại log mới nhất để tránh lag khi log trong Update
            if (Entries.Count > 0 && Entries[0].Message == entry.Message)
            {
                Entries[0] = entry;
                return;
            }

            Entries.Add(entry);
            if (entry.Level == "Log") LogCount++;
            else if (entry.Level == "Warning") WarningCount++;
            else ErrorCount++;

            if (Entries.Count > 100) Entries.RemoveAt(0); // Giới hạn bộ nhớ
        }
    }

    public static class DebugFilterData
    {
        // Cấu trúc mới: Dictionary<ClassName, Dictionary<MethodName, LogStats>>
        public static Dictionary<string, Dictionary<string, LogStats>> TreeData = new();
        public static Dictionary<string, bool> ClassStates = new(); // Thay thế cho ChannelStates
        public static bool IsCollapsed = false;

        public static void ClearAll()
        {
            TreeData.Clear();
            ClassStates.Clear();
        }

        public static void ClearClass(string cls) => TreeData.Remove(cls);
        public static void ClearMethod(string cls, string meth) => TreeData[cls].Remove(meth);

        public static bool RegisterAndCheck(string className, string methodName, string msg, string level, string file, int line)
        {
            // Tự động lấy Class Name làm Channel gốc
            if (!TreeData.ContainsKey(className))
            {
                TreeData[className] = new();
                ClassStates[className] = true;
            }

            if (!TreeData[className].ContainsKey(methodName))
                TreeData[className][methodName] = new LogStats();

            var stats = TreeData[className][methodName];

            // Lưu thông tin vị trí để Jump To Method
            stats.FileName = file;
            stats.LineNumber = line;

            var entry = new LogEntry { Message = msg, Level = level, File = file, Line = line };
            stats.AddEntry(entry);

            return ClassStates[className] && stats.Enabled;
        }
    }

    public static class MyLogger
    {
        public static bool IsLogEnabled = true;

        public delegate void OnLogFunc(string className, string message, Object context);

        public static event OnLogFunc OnLog;

        // Basic Logs
        public static void Log(string message, Object context = null) => FinalLog("Log", message, context);
        public static void LogWarning(string message, Object context = null) => FinalLog("Warning", message, context);
        public static void LogError(string message, Object context = null) => FinalLog("Error", message, context);
        
        // Conditional Logs
        public static void LogIf(bool condition, string message, Object context = null)
        {
            if (condition)
                FinalLog("Log", message, context);
        }
        public static void LogWarningIf(bool condition, string message, Object context = null)
        {
            if (condition)
                FinalLog("Warning", message, context);
        }
        public static void LogErrorIf(bool condition, string message, Object context = null)
        {
            if (condition)
                FinalLog("Error", message, context);
        }
        
        // Formatted Logs
        public static void LogFormat(string format, params object[] args) => FinalLog("Log", string.Format(format, args));
        public static void LogWarningFormat(string format, params object[] args) => FinalLog("Warning", string.Format(format, args));
        public static void LogErrorFormat(string format, params object[] args) => FinalLog("Error", string.Format(format, args));
        
        // JSON Logs
        public static void LogJson(object obj, Object context = null)
        {
            string json = JsonUtility.ToJson(obj, true);
            FinalLog("Log", json, context);
        }
      
        // Log newtonsoft 
        public static void LogJsonNewtonsoft(object obj, Object context = null)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
            FinalLog("Log", json, context);
        }

        private static void FinalLog(string level, string message, Object context = null)
        {
            var st = new StackTrace(2, true);
            var frame = st.GetFrame(0);
            if (frame == null) return;

            var method = frame.GetMethod();
            if (method.DeclaringType != null)
            {
                string className = method.DeclaringType.Name;
                string methodName = method.Name;
                string file = frame.GetFileName();
                int line = frame.GetFileLineNumber();

                bool canLog = DebugFilterData.RegisterAndCheck(className, methodName, message, level, file, line);
                if (!IsLogEnabled || !canLog) return;

                string finalMsg;

                if (level == "Error" || level == "Fatal")
                {
                    finalMsg = $"<b><color=#FF5555><size=13>[{className}] -> {message}</size></color></b>";
                    Debug.Log(finalMsg, context);
                }
                else if (level == "Warning")
                {
                    finalMsg = $"<b><color=#FFD600><size=12>[{className}] -> {message}</size></color></b>";
                    Debug.Log(finalMsg, context);
                }
                else
                {
                    finalMsg = $"<b><color=#FFFFFF><size=12>[{className}] -> {message}</size></color></b>";
                    Debug.Log(finalMsg, context);
                }

                OnLog?.Invoke(className, message, context);
            }
        }
    }

    public static class ExLog
    {
        public static string Bold(this string str) => string.IsNullOrEmpty(str) ? string.Empty : $"<b>{str}</b>";

        public static string Size(this string text, int size) => string.IsNullOrEmpty(text) ? string.Empty : $"<size={size}>{text}</size>";

        public static string Italic(this string str) => string.IsNullOrEmpty(str) ? string.Empty : $"<i>{str}</i>";

        public static string Time(this string str) => string.IsNullOrEmpty(str) ? string.Empty : $"<time>{str}</time>";

        public static string Color(this string text, Color color)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (color == default) color = UnityEngine.Color.white;
            return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";
        }
    }
}