using System;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace OSK
{
    public class XMLSystem : IFile
    {
        public void Save<T>(string fileName, T data, bool encrypt = false)
        {
            string path = IOUtility.GetPath($"{fileName}.xml");

            try
            {
                using var ms = new MemoryStream();
                new XmlSerializer(typeof(T)).Serialize(ms, data);
                byte[] bytes = ms.ToArray();

                if (encrypt)
                    bytes = FileSecurity.Encrypt(bytes, IOUtility.encryptKey);

                File.WriteAllBytes(path, bytes);
                RefreshEditor();
                OSKLogger.Log("Storage", $"✅ Saved: {path}");
            }
            catch (Exception ex)
            {
                OSKLogger.LogError("Storage", $"❌ Save Error: {fileName}.xml → {ex.Message}");
            }
        }

        public T Load<T>(string fileName, bool decrypt = false)
        {
            string path = IOUtility.GetPath($"{fileName}.xml");
            if (!File.Exists(path))
            {
                OSKLogger.LogError("Storage", $"❌ File not found: {path}");
                return default;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                if (decrypt)
                    bytes = FileSecurity.Decrypt(bytes, IOUtility.encryptKey);

                using var ms = new MemoryStream(bytes);
                var serializer = new XmlSerializer(typeof(T));
                T data = (T)serializer.Deserialize(ms);
                OSKLogger.Log("Storage", $"✅ Loaded: {path}");
                return data;
            }
            catch (Exception ex)
            {
                OSKLogger.LogError("Storage", $"❌ Load Error: {fileName}.xml → {ex.Message}");
                return default;
            }
        }

        public void Delete(string fileName) => IOUtility.DeleteFile($"{fileName}.xml");

        public T Query<T>(string fileName, bool condition) => condition ? Load<T>(fileName) : default;
         

        private static void RefreshEditor()
        {
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
    }
}
