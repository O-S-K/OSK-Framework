using System;
using UnityEngine;
using System.Collections.Generic;

namespace OSK
{
    public enum SaveType { Json, File, Xml }

    public class DataManager : GameFrameworkComponent
    {
        [Header("Global Settings")]
        public bool isEncrypt = false;

        private readonly JsonSystem _json = new JsonSystem();
        private readonly FileSystem _file = new FileSystem();
        private readonly XMLSystem _xml = new XMLSystem();

        private readonly Dictionary<SaveType, IFile> _typeMap = new Dictionary<SaveType, IFile>();

        public override void OnInit()
        {
#if UNITY_WEBGL
            var _web = new WebJsonSystem();
            Register(SaveType.Json,_web);
            Register(SaveType.File, _web);
            Register(SaveType.Xml, _web);
#else
            Register(SaveType.Json, _json);
            Register(SaveType.File, _file);
            Register(SaveType.Xml, _xml);
#endif
        }

        // ---------- Registration API (extensibility) ----------
        public void Register(SaveType key, IFile impl)
        {
            _typeMap[key] = impl ?? throw new ArgumentNullException(nameof(impl));
        }

        public void Unregister(SaveType key) => _typeMap.Remove(key);

        // ---------- Synchronous APIs (enum-based) ----------
        public void Save(SaveType type, string fileName, object data)
        {
            Debug .Log($"DataManager.Save: {fileName} ({type})");
            var fs = Resolve(type);
            Debug .Log($"DataManager.fs: {fs})");
            
            if (fs == null)
            {
                Debug.LogError($"DataManager.Save: Unknown SaveType {type}");
                return;
            }
            try
            {
                fs.Save(fileName, data, isEncrypt);
            }
            catch (Exception ex)
            {
                Debug.LogError($"DataManager.Save ERROR: {fileName} ({type})\n{ex}");
            }
        }

        public T Load<T>(SaveType type, string fileName)
        {
            var fs = Resolve(type);
            if (fs == null)
            {
                Debug.LogError($"DataManager.Load: Unknown SaveType {type}");
                return default;
            }

            try
            {
                if (!fs.Exists(fileName)) return default;
                return fs.Load<T>(fileName, isEncrypt);
            }
            catch (Exception ex)
            {
                Debug.LogError($"DataManager.Load ERROR: {fileName} ({type})\n{ex}");
                return default;
            }
        }

        public void Delete(SaveType type, string fileName)
        {
            var fs = Resolve(type);
            if (fs == null)
            {
                Debug.LogError($"DataManager.Delete: Unknown SaveType {type}");
                return;
            }

            try
            {
                if (!fs.Exists(fileName)) return;
                fs.Delete(fileName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"DataManager.Delete ERROR: {fileName} ({type})\n{ex}");
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
                Debug.LogError($"DataManager.WriteAllText ERROR: {fileName}\n{ex}");
            }
        }

        public void WriteAllLines(SaveType type, string fileName, string[] lines)
        {
            var fs = Resolve(type);
            if (fs == null)
            {
                Debug.LogError($"DataManager.WriteAllLines: Unknown SaveType {type}");
                return;
            }
            try
            {
                fs.WriteAllLines(fileName, lines);
            }
            catch (Exception ex)
            {
                Debug.LogError($"DataManager.WriteAllLines ERROR: {fileName} ({type})\n{ex}");
            }
        }
 
        // ---------- Internal helpers ----------
        private IFile Resolve(SaveType type) => _typeMap.GetValueOrDefault(type);
    }
}
