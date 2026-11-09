using System;
using UnityEngine;
using System.Collections.Generic;

namespace OSK
{
    public enum SaveType
    {
        Json,
        File,
        Xml,
        PlayerPrefs
    }

    public class DataManager : GameFrameworkComponent
    {
        [Header("Global Settings")] public bool isEncrypt = false;

        private readonly JsonSystem _json = new JsonSystem();
        private readonly FileSystem _file = new FileSystem();
        private readonly XMLSystem _xml = new XMLSystem();
        private readonly PlayerPrefsSystem _prefs = new PlayerPrefsSystem();

        private readonly Dictionary<SaveType, IFile> _typeMap = new Dictionary<SaveType, IFile>();

        public override void OnInit()
        {
            CleanupTempFiles();

#if UNITY_WEBGL
            var _web = new WebJsonSystem();
            Register(SaveType.Json,_web);
            Register(SaveType.File, _web);
            Register(SaveType.Xml, _web);
            Register(SaveType.PlayerPrefs, _prefs); // PlayerPrefs is natively cross-platform
#else
            Register(SaveType.Json, _json);
            Register(SaveType.File, _file);
            Register(SaveType.Xml, _xml);
            Register(SaveType.PlayerPrefs, _prefs);
#endif
        }

        // ---------- Registration API (extensibility) ----------
        public void Register(SaveType key, IFile impl)
        {
            _typeMap[key] = impl ?? throw new ArgumentNullException(nameof(impl));
        }

        public void Unregister(SaveType key) => _typeMap.Remove(key);

        // ---------- Synchronous APIs (enum-based) ----------
        /// <returns>False when the save did not happen; the previous file is left untouched.</returns>
        public bool Save<T>(SaveType type, string fileName, T data)
        {
            var fs = Resolve(type);

            if (fs == null)
            {
                MyLogger.LogError($"DataManager.Save: Unknown SaveType {type}");
                return false;
            }

            try
            {
                fs.Save(fileName, data, isEncrypt);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"DataManager.Save ERROR: {fileName} ({type})\n{ex}");
                return false;
            }
        }

        public T Load<T>(SaveType type, string fileName, T defaultValue = default)
        {
            var fs = Resolve(type);
            if (fs == null)
            {
                MyLogger.LogError($"DataManager.Load: Unknown SaveType {type}");
                return defaultValue;
            }

            try
            {
                if (!fs.Exists(fileName)) return defaultValue;
                return fs.Load<T>(fileName, defaultValue, isEncrypt);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"DataManager.Load ERROR: {fileName} ({type})\n{ex}");
                return defaultValue;
            }
        }

        // ---------- Asynchronous APIs ----------
        public async Cysharp.Threading.Tasks.UniTask<bool> SaveAsync<T>(SaveType type, string fileName, T data)
        {
            var fs = Resolve(type);
            if (fs == null)
            {
                MyLogger.LogError($"DataManager.SaveAsync: Unknown SaveType {type}");
                return false;
            }

            try
            {
                await fs.SaveAsync(fileName, data, isEncrypt);
                return true;
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"DataManager.SaveAsync ERROR: {fileName} ({type})\n{ex}");
                return false;
            }
        }

        public async Cysharp.Threading.Tasks.UniTask<T> LoadAsync<T>(SaveType type, string fileName,
            T defaultValue = default)
        {
            var fs = Resolve(type);
            if (fs == null)
            {
                MyLogger.LogError($"DataManager.LoadAsync: Unknown SaveType {type}");
                return defaultValue;
            }

            try
            {
                if (!fs.Exists(fileName)) return defaultValue;
                return await fs.LoadAsync<T>(fileName, defaultValue, isEncrypt);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"DataManager.LoadAsync ERROR: {fileName} ({type})\n{ex}");
                return defaultValue;
            }
        }

        public bool Exists(SaveType type, string fileName)
        {
            var fs = Resolve(type);
            if (fs == null)
            {
                MyLogger.LogError($"DataManager.Exists: Unknown SaveType {type}");
                return false;
            }

            try
            {
                return fs.Exists(fileName);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"DataManager.Exists ERROR: {fileName} ({type})\n{ex}");
                return false;
            }
        }

        public void Delete(SaveType type, string fileName)
        {
            var fs = Resolve(type);
            if (fs == null)
            {
                MyLogger.LogError($"DataManager.Delete: Unknown SaveType {type}");
                return;
            }

            try
            {
                if (!fs.Exists(fileName)) return;
                fs.Delete(fileName);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"DataManager.Delete ERROR: {fileName} ({type})\n{ex}");
            }
        }

        public T Query<T>(SaveType type, string fileName, bool condition) =>
            condition ? Load<T>(type, fileName) : default;

        public void WriteAllText(string fileName, string[] lines)
        {
            try
            {
                _file.WriteAllLines(fileName, lines);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"DataManager.WriteAllText ERROR: {fileName}\n{ex}");
            }
        }

        public void WriteAllLines(SaveType type, string fileName, string[] lines)
        {
            var fs = Resolve(type);
            if (fs == null)
            {
                MyLogger.LogError($"DataManager.WriteAllLines: Unknown SaveType {type}");
                return;
            }

            try
            {
                fs.WriteAllLines(fileName, lines);
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"DataManager.WriteAllLines ERROR: {fileName} ({type})\n{ex}");
            }
        }

        // ---------- Internal helpers ----------
        private IFile Resolve(SaveType type) => _typeMap.GetValueOrDefault(type);

        private void CleanupTempFiles()
        {
            try
            {
                string dir = IOUtility.GetDirectory();
                if (System.IO.Directory.Exists(dir))
                {
                    string[] tempFiles = System.IO.Directory.GetFiles(dir, "*.tmp");
                    foreach (var file in tempFiles)
                    {
                        System.IO.File.Delete(file);
                        MyLogger.Log($"🧹 Deleted orphaned temp file: {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"CleanupTempFiles ERROR: {ex.Message}");
            }
        }
    }
}