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

        public void Save<T>(string fileName, T data, bool encrypt = false)
        {
            string path = IOUtility.FilePath($"{fileName}.json");
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
                OSKLogger.Log("Storage", $"✅ Saved: {path}");
            }
            catch (System.Exception ex)
            {
                OSKLogger.LogError("Storage", $"❌ Save Error: {fileName}.json → {ex.Message}");
            }
        }

        public T Load<T>(string fileName, bool decrypt = false)
        {
            string path = IOUtility.FilePath($"{fileName}.json");
            if (!File.Exists(path))
            {
                OSKLogger.LogError("Storage", $"❌ File not found: {path}");
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

                OSKLogger.Log("Storage", $"✅ Loaded: {path}");
                return data;
            }
            catch (System.Exception ex)
            {
                OSKLogger.LogError("Storage", $"❌ Load Error: {fileName}.json → {ex.Message}");
                return default;
            }
        }

        public void Delete(string fileName) => IOUtility.DeleteFile($"{fileName}.json");

        public T Query<T>(string fileName, bool condition) => condition ? Load<T>(fileName) : default;

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
