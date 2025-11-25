using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace OSK
{
    public class XMLSystem : IFile
    {
        private static string EnsureExtension(string fileName, string ext)
        {
            if (Path.HasExtension(fileName)) return fileName;
            return fileName + ext;
        }

        private string ResolvePath(string fileName)
        {
            string filename = EnsureExtension(fileName, ".xml");
            return IOUtility.GetPath(filename);
        }

        public void Save<T>(string fileName, T data, bool encrypt = false)
        {
            string path = ResolvePath(fileName);
            try
            {
                string jsonString = JsonConvert.SerializeObject(data, Formatting.Indented, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                byte[] bytes = Encoding.UTF8.GetBytes(jsonString);
                if (encrypt)
                {
                    bytes = FileSecurity.Encrypt(bytes, IOUtility.encryptKey);
                }
                File.WriteAllBytes(path, bytes);
            }
            catch (Exception ex)
            {
                OSKLogger.LogError("Storage", $"❌ Save JSON Error: {Path.GetFileName(path)} → {ex.Message}");
            }
        }

        public T Load<T>(string fileName, bool isEncrypted = false)
        {
            string path = ResolvePath(fileName);

            if (!File.Exists(path))
            {
                OSKLogger.LogWarning("Storage", $"File not found: {path}");
                return default(T);
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                if (isEncrypted)
                {
                    bytes = FileSecurity.Decrypt(bytes, IOUtility.encryptKey);
                }
                string jsonString = Encoding.UTF8.GetString(bytes);
                return JsonConvert.DeserializeObject<T>(jsonString);
            }
            catch (Exception ex)
            {
                OSKLogger.LogError("Storage", $"❌ Load JSON Error: {path}\n{ex.Message}");
                return default(T);
            }
        }

        public void Delete(string fileName) => IOUtility.DeleteFile(EnsureExtension(fileName, ".xml"));

        public T Query<T>(string fileName, bool condition) => condition ? Load<T>(fileName) : default;

        public bool Exists(string fileName)
        {
            string path = ResolvePath(fileName);
            return File.Exists(path);
        }

        public void WriteAllLines(string fileName, string[] lines)
        {
            OSKLogger.LogError("Storage", $"❌ WriteAllLines only SaveType.File");
        }

        private static void RefreshEditor()
        {
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
    }
}
