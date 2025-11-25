// FileSystem.cs
using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks;

namespace OSK
{
    public class FileSystem : IFile
    {
        private static string EnsureExtension(string fileName, string ext)
        {
            if (Path.HasExtension(fileName)) return fileName;
            return fileName + ext;
        }

        private string ResolvePath(string fileName)
        {
            string filename = EnsureExtension(fileName, ".dat");
            return IOUtility.GetPath(filename);
        }

        public void Save<T>(string fileName, T data, bool encrypt = false)
        {
            string path = ResolvePath(fileName);

            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.None);
                byte[] bytes = Encoding.UTF8.GetBytes(json);

                using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
                {
                    writer.Write(bytes.Length);
                    writer.Write(bytes);
                }

                if (encrypt)
                {
                    var enc = FileSecurity.Encrypt(File.ReadAllBytes(path), IOUtility.encryptKey);
                    File.WriteAllBytes(path, enc);
                }

                RefreshEditor();
                OSKLogger.Log("Storage", $"✅ Saved: {path}");
            }
            catch (Exception ex)
            {
                OSKLogger.LogError("Storage", $"❌ Save Error: {Path.GetFileName(path)} → {ex.Message}");
            }
        }

        public T Load<T>(string fileName, bool decrypt = false)
        {
            string path = ResolvePath(fileName);
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

                using var reader = new BinaryReader(new MemoryStream(bytes));
                int len = reader.ReadInt32();
                string json = Encoding.UTF8.GetString(reader.ReadBytes(len));

                OSKLogger.Log("Storage", $"✅ Loaded: {path}");
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                OSKLogger.LogError("Storage", $"❌ Load Error: {Path.GetFileName(path)} → {ex.Message}");
                return default;
            }
        }

        public void Delete(string fileName) => IOUtility.DeleteFile(EnsureExtension(fileName, ".dat"));

        public T Query<T>(string fileName, bool condition) => condition ? Load<T>(fileName) : default;

        public void WriteAllLines(string fileName, string[] lines)
        {
            string path = IOUtility.GetPath(EnsureExtension(fileName, ".txt"));
            File.WriteAllLines(path, lines);
            OSKLogger.Log("Storage", $"📝 Wrote lines to: {path}");
            RefreshEditor();
        }

        public bool Exists(string fileName)
        {
            string path = ResolvePath(fileName);
            return File.Exists(path);
        }

        private static void RefreshEditor()
        {
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
    }
}
