using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;

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
            WriteResolved(path, Serialize(data), encrypt);
        }

        private static byte[] Serialize<T>(T data)
        {
            var serializer = new XmlSerializer(typeof(T));
            using (var memoryStream = new MemoryStream())
            {
                serializer.Serialize(memoryStream, data);
                return memoryStream.ToArray();
            }
        }

        // Failures propagate so DataManager can tell the caller the save did not happen.
        private static void WriteResolved(string path, byte[] bytes, bool encrypt)
        {
            string tempPath = path + ".tmp";

            if (encrypt)
            {
                bytes = FileSecurity.Encrypt(bytes, IOUtility.encryptKey);
            }

            File.WriteAllBytes(tempPath, bytes);
            IOUtility.CommitTempFile(tempPath, path);
            MyLogger.Log($"✅ Saved XML (Safe Save): {path}");
        }

        public async Cysharp.Threading.Tasks.UniTask SaveAsync<T>(string fileName, T data, bool encrypt = false)
        {
            MyLogger.Log($"⏳ [Async] Starting background save for: {fileName}");
            string path = ResolvePath(fileName); // MUST be on Main Thread
            byte[] bytes = Serialize(data); // On the main thread: gameplay may change data while the pool thread runs
            await Cysharp.Threading.Tasks.UniTask.RunOnThreadPool(() => WriteResolved(path, bytes, encrypt));
        }

        public T Load<T>(string fileName, T defaultValue = default, bool isDecrypt = false)
        {
            string path = ResolvePath(fileName);
            return LoadResolved<T>(path, defaultValue, isDecrypt);
        }

        private T LoadResolved<T>(string path, T defaultValue = default, bool isEncrypted = false)
        {
            if (!File.Exists(path))
            {
                MyLogger.LogWarning($"File not found: {path}");
                return defaultValue;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(path).DecryptSmart(IOUtility.encryptKey);
                if (bytes == null) throw new IOException("File empty or corrupt");
                
                var serializer = new XmlSerializer(typeof(T));
                using (var memoryStream = new MemoryStream(bytes))
                {
                    MyLogger.Log($"✅ Loaded XML: {path}");
                    return (T)serializer.Deserialize(memoryStream);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Load XML Error: {path}\n{ex.Message}");
                IOUtility.PreserveCorruptFile(path);
                return defaultValue;
            }
        }

        public async Cysharp.Threading.Tasks.UniTask<T> LoadAsync<T>(string fileName, T defaultValue = default, bool isDecrypt = false)
        {
            MyLogger.Log($"⏳ [Async] Starting background load for: {fileName}");
            string path = ResolvePath(fileName); // MUST be on Main Thread
            return await Cysharp.Threading.Tasks.UniTask.RunOnThreadPool(() => LoadResolved<T>(path, defaultValue, isDecrypt));
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
            MyLogger.LogError($"❌ WriteAllLines only SaveType.File");
        }
    }
}
