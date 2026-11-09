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
            WriteResolved(path, Serialize(data), encrypt);
        }

        private static byte[] Serialize<T>(T data)
        {
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = { new Vector3Converter(), new QuaternionConverter() }
            };

            using (var ms = new MemoryStream())
            {
                // 🚀 ULTRA OPTIMIZATION: Stream straight to bytes, avoid strings completely
                using (var sw = new StreamWriter(ms, new UTF8Encoding(false), 1024, true))
                using (var jw = new JsonTextWriter(sw) { Formatting = Formatting.None })
                {
                    JsonSerializer.CreateDefault(settings).Serialize(jw, data);
                }
                return ms.ToArray();
            }
        }

        // Failures propagate so DataManager can tell the caller the save did not happen.
        private static void WriteResolved(string path, byte[] bytes, bool encrypt)
        {
            string tempPath = path + ".tmp";
            bytes = DataCompressor.Compress(bytes);

            if (encrypt)
            {
                bytes = FileSecurity.Encrypt(bytes, IOUtility.encryptKey);
            }

            using (var fs = new FileStream(tempPath, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(bytes.Length);
                bw.Write(bytes);
            }

            IOUtility.CommitTempFile(tempPath, path);
            MyLogger.Log($"✅ Saved (Safe Save): {path}");
        }

        public async UniTask SaveAsync<T>(string fileName, T data, bool encrypt = false)
        {
            MyLogger.Log($"⏳ [Async] Starting background save for: {fileName}");
            string path = ResolvePath(fileName); // MUST be on Main Thread
            byte[] bytes = Serialize(data); // On the main thread: gameplay may change data while the pool thread runs
            await UniTask.RunOnThreadPool(() => WriteResolved(path, bytes, encrypt));
        }

        public T Load<T>(string fileName, T defaultValue = default, bool isDecrypt = false)
        {
            string path = ResolvePath(fileName);
            return LoadResolved<T>(path, defaultValue, isDecrypt);
        }

        private T LoadResolved<T>(string path, T defaultValue = default, bool decrypt = false)
        {
            if (!File.Exists(path))
            {
                MyLogger.LogError($"❌ File not found: {path}");
                return defaultValue;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                byte[] data = bytes.DecryptSmart(IOUtility.encryptKey);
                if (data == null) throw new IOException("File empty or corrupt");

                MyLogger.Log($"✅ Loaded: {path}");
                var settings = new JsonSerializerSettings { Converters = { new Vector3Converter(), new QuaternionConverter() } };
                
                using (var dataStream = new MemoryStream(data))
                using (var sr = new StreamReader(dataStream, Encoding.UTF8))
                using (var jr = new JsonTextReader(sr))
                {
                    var serializer = JsonSerializer.CreateDefault(settings);
                    return serializer.Deserialize<T>(jr);
                }
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"❌ Load Error: {Path.GetFileName(path)} → {ex.Message}");
                IOUtility.PreserveCorruptFile(path);
                return defaultValue;
            }
        }

        public async UniTask<T> LoadAsync<T>(string fileName, T defaultValue = default, bool isDecrypt = false)
        {
            MyLogger.Log($"⏳ [Async] Starting background load for: {fileName}");
            string path = ResolvePath(fileName); // MUST be on Main Thread
            return await UniTask.RunOnThreadPool(() => LoadResolved<T>(path, defaultValue, isDecrypt));
        }

        public void Delete(string fileName) => IOUtility.DeleteFile(EnsureExtension(fileName, ".dat"));

        public T Query<T>(string fileName, bool condition) => condition ? Load<T>(fileName) : default;

        public void WriteAllLines(string fileName, string[] lines)
        {
            string path = IOUtility.GetPath(EnsureExtension(fileName, ".txt"));
            File.WriteAllLines(path, lines);
            MyLogger.Log($"📝 Wrote lines to: {path}");
        }

        public bool Exists(string fileName)
        {
            string path = ResolvePath(fileName);
            return File.Exists(path);
        }


    }
}
