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
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                if (FormatDecimals)
                    json = FormatJsonDecimals(json, DecimalPlaces);

                if (encrypt)
                {
                    var bytes = Encoding.UTF8.GetBytes(json);
                    File.WriteAllBytes(path, Obfuscator.Encrypt(bytes, IOUtility.encryptKey));
                }
                else File.WriteAllText(path, json);

                RefreshEditor();
                MyLogger.Log($"✅ Saved: {path}");
            }
            catch (System.Exception ex)
            {
                MyLogger.LogError($"❌ Save Error: {Path.GetFileName(path)} → {ex.Message}");
            }
        }

        public T Load<T>(string fileName, bool decrypt = false)
        {
            string path = ResolvePath(fileName);
            if (!File.Exists(path))
            {
                MyLogger.LogError($"❌ File not found: {path}");
                return default;
            }

            try
            {
                string json = decrypt
                    ? Encoding.UTF8.GetString(Obfuscator.Decrypt(File.ReadAllBytes(path), IOUtility.encryptKey))
                    : File.ReadAllText(path);

                if (string.IsNullOrWhiteSpace(json))
                    throw new IOException("File empty or corrupt");

                T data = JsonConvert.DeserializeObject<T>(json);
                if (data == null) throw new IOException("Deserialize returned null");

                MyLogger.Log($"✅ Loaded: {path}");
                return data;
            }
            catch (System.Exception ex)
            {
                MyLogger.LogError($"❌ Load Error: {Path.GetFileName(path)} → {ex.Message}");
                return default;
            }
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

        private static void RefreshEditor()
        {
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
    }
}
