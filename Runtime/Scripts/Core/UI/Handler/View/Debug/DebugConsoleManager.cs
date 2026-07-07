using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OSK
{
    internal sealed class DebugConsoleManager
    {
        private readonly List<LogEntry> _logs = new List<LogEntry>();
        private readonly StringBuilder _builder = new StringBuilder(4096);
        private int _maxLogCount;
        private float _smoothDeltaTime;
        private bool _showLog = true;
        private bool _showWarning = true;
        private bool _showError = true;

        public bool ConsoleDirty { get; private set; } = true;

        public DebugConsoleManager(int maxLogCount)
        {
            _maxLogCount = Mathf.Max(1, maxLogCount);
        }

        // Updates runtime timing used by the summary line.
        public void Tick(float unscaledDeltaTime)
        {
            _smoothDeltaTime += (unscaledDeltaTime - _smoothDeltaTime) * 0.1f;
        }

        // Updates the maximum stored log count.
        public void SetMaxLogCount(int maxLogCount)
        {
            _maxLogCount = Mathf.Max(1, maxLogCount);
            TrimLogs();
            ConsoleDirty = true;
        }

        // Enables or disables plain logs in the console filter.
        public void SetShowLog(bool value)
        {
            _showLog = value;
            ConsoleDirty = true;
        }

        // Enables or disables warnings in the console filter.
        public void SetShowWarning(bool value)
        {
            _showWarning = value;
            ConsoleDirty = true;
        }

        // Enables or disables errors in the console filter.
        public void SetShowError(bool value)
        {
            _showError = value;
            ConsoleDirty = true;
        }

        // Stores Unity log messages.
        public void CaptureLog(string condition, string stackTrace, LogType type)
        {
            _logs.Add(new LogEntry(condition, stackTrace, type));
            TrimLogs();
            ConsoleDirty = true;
        }

        // Clears captured logs.
        public void ClearLogs()
        {
            _logs.Clear();
            ConsoleDirty = true;
        }

        // Marks the console as clean after the view finishes drawing current rows.
        public void MarkConsoleClean()
        {
            ConsoleDirty = false;
        }

        // Returns currently visible log entries after filter rules.
        public List<LogEntry> GetVisibleLogs()
        {
            List<LogEntry> result = new List<LogEntry>();
            int start = Mathf.Max(0, _logs.Count - _maxLogCount);
            for (int i = start; i < _logs.Count; i++)
            {
                LogEntry entry = _logs[i];
                if (ShouldShow(entry.Type))
                {
                    result.Add(entry);
                }
            }

            return result;
        }

        // Builds the legacy text console output.
        public string BuildConsoleText()
        {
            _builder.Clear();
            List<LogEntry> visibleLogs = GetVisibleLogs();
            for (int i = 0; i < visibleLogs.Count; i++)
            {
                _builder.AppendLine(FormatLog(visibleLogs[i]));
            }

            return _builder.Length == 0 ? "No logs." : _builder.ToString();
        }

        // Copies captured logs to the clipboard.
        public void CopyLogs()
        {
            _builder.Clear();
            for (int i = 0; i < _logs.Count; i++)
            {
                LogEntry entry = _logs[i];
                _builder.AppendLine(FormatLog(entry));
                if (!string.IsNullOrEmpty(entry.StackTrace))
                {
                    _builder.AppendLine(entry.StackTrace);
                }
            }

            GUIUtility.systemCopyBuffer = _builder.ToString();
        }

        // Builds the top summary line.
        public string BuildSummaryText()
        {
            float fps = _smoothDeltaTime > 0f ? 1f / _smoothDeltaTime : 0f;
            long ramMb = GC.GetTotalMemory(false) / 1024 / 1024;
            int moduleCount = Main.SGameFrameworkComponents != null ? Main.SGameFrameworkComponents.Count : 0;
            return $"FPS {Mathf.RoundToInt(fps)}  |  RAM {ramMb} MB  |  Modules {moduleCount}";
        }

        // Builds the system information block.
        public string BuildSystemInfoText()
        {
            Scene scene = SceneManager.GetActiveScene();
            _builder.Clear();
            _builder.AppendLine("Scene: " + scene.name);
            _builder.AppendLine("Time Scale: " + Time.timeScale.ToString("0.00"));
            _builder.AppendLine("Resolution: " + Screen.width + " x " + Screen.height);
            _builder.AppendLine("Platform: " + Application.platform);
            _builder.AppendLine("Device: " + SystemInfo.deviceModel);
            _builder.AppendLine("Graphics: " + SystemInfo.graphicsDeviceName);
            _builder.AppendLine("System Memory: " + SystemInfo.systemMemorySize + " MB");
            return _builder.ToString();
        }

        // Pauses framework execution.
        public void PauseFramework()
        {
            Main.SetPause(true);
        }

        // Resumes framework execution.
        public void ResumeFramework()
        {
            Main.SetPause(false);
        }

        // Restarts the active scene.
        public void RestartScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0)
            {
                SceneManager.LoadScene(scene.buildIndex);
                return;
            }

            SceneManager.LoadScene(scene.name);
        }

        // Shuts the framework down.
        public void Quit()
        {
            Main.Shutdown(ShutdownType.Quit);
        }

        // Formats one console row.
        public static string FormatLog(LogEntry entry)
        {
            return "[" + entry.Type + "] " + entry.Message;
        }

        // Estimates row height so longer logs stay readable inside the console scroll view.
        public static float GetLogRowHeight(string message)
        {
            int length = string.IsNullOrEmpty(message) ? 0 : message.Length;
            int lines = Mathf.Clamp(1 + length / 58, 1, 4);
            return 18f * lines;
        }

        // Returns the log color used by the runtime console.
        public static Color GetLogColor(LogType type)
        {
            switch (type)
            {
                case LogType.Warning:
                    return new Color(1f, 0.78f, 0.25f, 1f);
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    return new Color(1f, 0.32f, 0.32f, 1f);
                default:
                    return Color.white;
            }
        }

        private void TrimLogs()
        {
            while (_logs.Count > _maxLogCount)
            {
                _logs.RemoveAt(0);
            }
        }

        private bool ShouldShow(LogType type)
        {
            switch (type)
            {
                case LogType.Warning:
                    return _showWarning;
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    return _showError;
                default:
                    return _showLog;
            }
        }

        public struct LogEntry
        {
            public readonly string Message;
            public readonly string StackTrace;
            public readonly LogType Type;

            public LogEntry(string message, string stackTrace, LogType type)
            {
                Message = message;
                StackTrace = stackTrace;
                Type = type;
            }
        }
    }
}
