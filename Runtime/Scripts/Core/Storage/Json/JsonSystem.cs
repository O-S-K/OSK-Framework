using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace OSK
{
    public class JsonSystem : IFile
    {
        public static bool FormatDecimals = true;
        public static int DecimalPlaces = 4;

        private static string EnsureExtension(string fileName, string ext)
        {
            if (Path.HasExtension(fileName)) return fileName;
            return fileName + ext;
        }

        private string ResolvePath(string fileName)
        {
            string filename = EnsureExtension(fileName, ".json");
            return IOUtility.GetPath(filename);
        }

        public void Save<T>(string fileName, T data, bool encrypt = false)
        {
            string path = ResolvePath(fileName);
            SaveResolved(path, data, encrypt);
        }

        private void SaveResolved<T>(string path, T data, bool encrypt = false)
        {
            try
            {
                string tempPath = path + ".tmp";
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Converters = { new Vector3Converter(), new QuaternionConverter() }
                };

                if (encrypt)
                {
                    string json = JsonConvert.SerializeObject(data, Formatting.Indented, settings);
                    if (FormatDecimals && json.Length < 1000000)
                        json = FormatJsonDecimals(json, DecimalPlaces);
                    var bytes = Encoding.UTF8.GetBytes(json);
                    bytes = DataCompressor.Compress(bytes);
                    File.WriteAllBytes(tempPath, FileSecurity.Encrypt(bytes, IOUtility.encryptKey));
                }
                else
                {
                    using (StreamWriter sw = new StreamWriter(tempPath, false, Encoding.UTF8))
                    using (JsonTextWriter jw = new JsonTextWriter(sw))
                    {
                        jw.Formatting = Formatting.Indented;
                        var serializer = JsonSerializer.CreateDefault(settings);
                        serializer.Serialize(jw, data);
                    }
                }

                // Safe Save: Swap temp file with real file
                if (File.Exists(path)) File.Delete(path);
                File.Move(tempPath, path);

                MyLogger.Log($"✅ Saved (Safe Save): {path}");
            }
            catch (System.Exception ex)
            {
                MyLogger.LogError($"❌ Save Error: {Path.GetFileName(path)} → {ex.Message}");
            }
        }

        public async Cysharp.Threading.Tasks.UniTask SaveAsync<T>(string fileName, T data, bool encrypt = false)
        {
            MyLogger.Log($"⏳ [Async] Starting background save for: {fileName}");
            string path = ResolvePath(fileName); // MUST be on Main Thread
            await Cysharp.Threading.Tasks.UniTask.RunOnThreadPool(() => SaveResolved(path, data, encrypt));
        }

        public T Load<T>(string fileName, T defaultValue = default, bool decrypt = false)
        {
            string path = ResolvePath(fileName);
            return LoadResolved<T>(path,defaultValue, decrypt);
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
                var settings = new JsonSerializerSettings { Converters = { new Vector3Converter(), new QuaternionConverter() } };
                T data;

                if (decrypt)
                {
                    byte[] bytes = File.ReadAllBytes(path).DecryptSmart(IOUtility.encryptKey);
                    string json = Encoding.UTF8.GetString(bytes);
                    if (string.IsNullOrWhiteSpace(json)) throw new IOException("File empty or corrupt");
                    data = JsonConvert.DeserializeObject<T>(json, settings);
                }
                else
                {
                    // 🚀 ULTRA OPTIMIZATION: Stream directly from disk!
                    using (StreamReader sr = new StreamReader(path, Encoding.UTF8))
                    using (JsonTextReader jr = new JsonTextReader(sr))
                    {
                        var serializer = JsonSerializer.CreateDefault(settings);
                        data = serializer.Deserialize<T>(jr);
                    }
                }

                if (data == null) throw new IOException("Deserialize returned null");

                MyLogger.Log($"✅ Loaded: {path}");
                return data;
            }
            catch (System.Exception ex)
            {
                MyLogger.LogError($"❌ Load Error: {Path.GetFileName(path)} → {ex.Message}");
                return defaultValue;
            }
        }

        public async Cysharp.Threading.Tasks.UniTask<T> LoadAsync<T>(string fileName, T defaultValue = default, bool isDecrypt = false)
        {
            MyLogger.Log($"⏳ [Async] Starting background load for: {fileName}");
            string path = ResolvePath(fileName); // MUST be on Main Thread
            return await Cysharp.Threading.Tasks.UniTask.RunOnThreadPool(() => LoadResolved<T>(path, defaultValue, isDecrypt));
        }

        public void Delete(string fileName) => IOUtility.DeleteFile(EnsureExtension(fileName, ".json"));

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

        private string FormatJsonDecimals(string json, int places)
        {
            return Regex.Replace(json, @"\d+\.\d+", match =>
                double.TryParse(match.Value, out double n)
                    ? n.ToString($"F{places}", System.Globalization.CultureInfo.InvariantCulture)
                    : match.Value);
        }


    }
}
