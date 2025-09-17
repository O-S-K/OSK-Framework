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

        // =============================
        // Members
        // =============================
        private static readonly Dictionary<string, Color> channelToColour = new()
        {
            { "OSK", new Color(0.3f, 0.8f, 0.49f) },
            { "UI", Color.cyan },
            { "Sound", Color.green },
            { "Pool", Color.yellow },
            { "Data", Color.magenta },
            { "Localization", Color.grey },
            { "Main", Color.blue },
            { "MonoTick", new Color(1f, 0.5f, 0f) },
            { "Resource", new Color(0.5f, 0f, 1f) },
            { "Director", new Color(0f, 1f, 0.5f) },
            { "Storage", new Color(1f, 0f, 0.5f) },
        };

        private static readonly Dictionary<string, bool> channelEnabled = new();

        public static bool IsLogEnabled = true;
        public static void SetLogEnabled(bool value) => IsLogEnabled = value;

        public delegate void OnLogFunc(string channel, int priority, string message);

        public static event OnLogFunc OnLog;

        // =============================
        // Channel Control
        // =============================
        public static void AddChannelDefinition(string channel, Color? defaultColor = null)
        {
            if (string.IsNullOrEmpty(channel)) return;
            EnsureChannel(channel, defaultColor ?? Color.white);
        }

        public static void EnsureChannel(string channel, Color defaultColor)
        {
            // Color
            if (!channelToColour.ContainsKey(channel))
            {
#if UNITY_EDITOR
                string savedHex = UnityEditor.EditorPrefs.GetString($"OSK.Logg.ChannelColor.{channel}",
                    ColorUtility.ToHtmlStringRGB(defaultColor));
                if (ColorUtility.TryParseHtmlString("#" + savedHex, out var col))
                    channelToColour[channel] = col;
                else
                    channelToColour[channel] = defaultColor;
#else
                channelToColour[channel] = defaultColor;
#endif
            }

            // Enabled state
            if (!channelEnabled.ContainsKey(channel))
            {
#if UNITY_EDITOR
                bool saved = UnityEditor.EditorPrefs.GetBool($"OSK.Logg.Channel.{channel}", true);
                channelEnabled[channel] = saved;
#else
                channelEnabled[channel] = true;
#endif
            }
        }

        public static void SetChannelEnabled(string channel, bool enabled)
        {
            EnsureChannel(channel, Color.white);
            channelEnabled[channel] = enabled;
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetBool($"OSK.Logg.Channel.{channel}", enabled);
#endif
        }

        public static bool IsChannelActive(string channel)
        {
            return channelEnabled.TryGetValue(channel, out var enabled) && enabled;
        }

        public static IEnumerable<string> GetAllChannels() => channelToColour.Keys;

        public static Color GetChannelColor(string channel)
        {
            if (channelToColour.TryGetValue(channel, out var col))
                return col;
            return Color.white;
        }

        public static void SetChannelColor(string channel, Color color)
        {
            EnsureChannel(channel, color);
            channelToColour[channel] = color;
#if UNITY_EDITOR
            string hex = ColorUtility.ToHtmlStringRGB(color);
            UnityEditor.EditorPrefs.SetString($"OSK.Logg.ChannelColor.{channel}", hex);
#endif
        }

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

            EnsureChannel(channel, Color.white);
            if (!IsChannelActive(channel)) return;

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
            Color col = GetChannelColor(channel);
            string channelHex = ColorUtility.ToHtmlStringRGB(col);

            string levelColor = level switch
            {
                "Warning" => "orange",
                "Error" => "red",
                "Fatal" => "red",
                _ => "white"
            };

            return $"<b><color=#{channelHex}>[{channel}]</color></b> <color={levelColor}>{message}</color>";
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