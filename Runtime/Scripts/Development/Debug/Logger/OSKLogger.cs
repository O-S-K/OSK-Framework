#define UNITY_DIALOGS

namespace OSK
{
    using UnityEngine;

    public class OSKLogger
    {
        private static OSKLogger _instance;
        private static OSKLogger Instance => _instance ??= new OSKLogger();

        private OSKLogger() { }

        public static bool IsLogEnabled = true;
        public static void SetLogEnabled(bool value) => IsLogEnabled = value;

        // Updated delegate to include context
        public delegate void OnLogFunc(string channel, int priority, string message, Object context);
        public static event OnLogFunc OnLog;

        // ========================================================================
        // 1. STANDARD LOGGING (With Context Overloads)
        // ========================================================================
        
        // Log
        public static void Log(string message, Object context = null) 
            => FinalLog("OSK", "Log", message, context);
        public static void Log(string channel, string message, Object context = null) 
            => FinalLog(channel, "Log", message, context);

        // Warning
        public static void LogWarning(string message, Object context = null) 
            => FinalLog("OSK", "Warning", message, context);
        public static void LogWarning(string channel, string message, Object context = null) 
            => FinalLog(channel, "Warning", message, context);

        // Error
        public static void LogError(string message, Object context = null) 
            => FinalLog("OSK", "Error", message, context);
        public static void LogError(string channel, string message, Object context = null) 
            => FinalLog(channel, "Error", message, context);

        // Fatal
        public static void LogFatal(string message, Object context = null) 
            => FinalLog("OSK", "Fatal", message, context);
        public static void LogFatal(string channel, string message, Object context = null) 
            => FinalLog(channel, "Fatal", message, context);

        // ========================================================================
        // 2. LOG IF (Conditional Logging)
        // ========================================================================

        public static void LogIf(bool condition, string message, Object context = null)
        {
            if (condition) FinalLog("OSK", "Log", message, context);
        }

        public static void LogWarningIf(bool condition, string message, Object context = null)
        {
            if (condition) FinalLog("OSK", "Warning", message, context);
        }

        public static void LogErrorIf(bool condition, string message, Object context = null)
        {
            if (condition) FinalLog("OSK", "Error", message, context);
        }

        // ========================================================================
        // 3. LOG FORMAT (String.Format style)
        // ========================================================================

        public static void LogFormat(string format, params object[] args) 
            => FinalLog("OSK", "Log", string.Format(format, args));

        public static void LogWarningFormat(string format, params object[] args) 
            => FinalLog("OSK", "Warning", string.Format(format, args));

        public static void LogErrorFormat(string format, params object[] args) 
            => FinalLog("OSK", "Error", string.Format(format, args));

        // ========================================================================
        // 4. LOG NOT NULL / NULL (Quick Debugging)
        // ========================================================================

        /// <summary>
        /// Logs an error if the target object is null.
        /// </summary>
        public static void LogIfNull(object target, string message, Object context = null)
        {
            if (target == null || (target is UnityEngine.Object obj && !obj)) 
                FinalLog("OSK", "Error", message, context);
        }

        // ========================================================================
        // INTERNAL LOGIC
        // ========================================================================

        #if UNITY_6000_OR_NEWER
        [HideInCallstack]
        #endif
        private static void FinalLog(string channel, string level, string message, Object context = null)
        {
            if (!IsLogEnabled) return;

            string finalMessage = ConstructFinalString(channel, level, message);

            // Updated to pass 'context' to Unity Debug. This allows you to click 
            // the log in Console and ping the object in the Hierarchy.
            switch (level)
            {
                case "Fatal":
                case "Error":
                    if (context != null) Debug.LogError(finalMessage, context);
                    else Debug.LogError(finalMessage);
                    break;
                case "Warning":
                    if (context != null) Debug.LogWarning(finalMessage, context);
                    else Debug.LogWarning(finalMessage);
                    break;
                default:
                    if (context != null) Debug.Log(finalMessage, context);
                    else Debug.Log(finalMessage);
                    break;
            }

            OnLog?.Invoke(channel, level == "Log" ? 0 : level == "Warning" ? 1 : 2, message, context);
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

            return $"[{channel.Bold()}]<color={levelColor}> -> {message}</color>";
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