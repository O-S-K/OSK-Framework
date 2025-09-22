#define UNITY_DIALOGS // Comment out to disable dialogs for fatal errors

namespace OSK
{
    using UnityEngine;
    using System.Collections.Generic;

    public class OSKLogger
    {
        private static OSKLogger instance;
        private static OSKLogger Instance => instance ??= new OSKLogger();

        private OSKLogger()
        {
        }


        public static bool IsLogEnabled = true;
        public static void SetLogEnabled(bool value) => IsLogEnabled = value;

        public delegate void OnLogFunc(string channel, int priority, string message);

        public static event OnLogFunc OnLog;

        // =============================
        // Logging
        // =============================
        public static void Log(string message) => FinalLog("OSK", "Log", message);
        public static void Log(string channel, string message) => FinalLog(channel, "Log", message);

        public static void LogWarning(string message) => FinalLog("OSK", "Warning", message);
        public static void LogWarning(string channel, string message) => FinalLog(channel, "Warning", message);

        public static void LogError(string message) => FinalLog("OSK", "Error", message);
        public static void LogError(string channel, string message) => FinalLog(channel, "Error", message);

        public static void LogFatal(string message) => FinalLog("OSK", "Fatal", message);
        public static void LogFatal(string channel, string message) => FinalLog(channel, "Fatal", message);

        #if UNITY_6000_OR_NEWER
        [HideInCallstack]
        #endif
        private static void FinalLog(string channel, string level, string message)
        {
            if (!IsLogEnabled) return;

            string finalMessage = ConstructFinalString(channel, level, message);

            switch (level)
            {
                case "Fatal":
                case "Error":
                    Debug.LogError(finalMessage);
                    break;
                case "Warning":
                    Debug.LogWarning(finalMessage);
                    break;
                default:
                    Debug.Log(finalMessage);
                    break;
            }

            if (OnLog != null)
            {
                OnLog.Invoke(channel, level == "Log" ? 0 : level == "Warning" ? 1 : 2, message);
            }
        }

        private static string ConstructFinalString(string channel, string level, string message)
        {
            string levelColor = level switch
            {
                "Warning" => "orange",
                "Error" => "red",
                "Fatal" => "red",
                _ => "white"
            };

            return $"[{channel.Bold()}] <color={levelColor}>{message}</color>";
        }
    }


    public static class ExLog
    {
        public static string Bold(this string str)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            return $"<b>{str}</b>";
        }

        public static string Color(this string text, Color color)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            if (color == default)
                color = UnityEngine.Color.white;

            return text.GetColorHtml(color);
        }

        public static string Size(this string text, int size)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return $"<size={size}>{text}</size>";
        }

        public static string Italic(this string str)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            return $"<i>{str}</i>";
        }

        public static string Time(this string str)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            return $"<time>{str}</time>";
        }
    }
}