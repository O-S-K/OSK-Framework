using System.IO;
using System.Text;
using UnityEngine;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace OSK
{
    public class JsonSystem : IFile
    {
        // Todo: Call For Mobile Devices
        // onApplicationPause() => SaveJson
        public static bool IsFormatDecimals = true;
        public static int DecimalPlaces = 4;

        public void Save<T>(string fileName, T data, bool ableEncrypt = false)
        {
            var filePath = IOUtility.FilePath(fileName + ".json");
            try
            {
                string saveJson = JsonConvert.SerializeObject(data, Formatting.Indented);
                if (IsFormatDecimals)
                    saveJson = FormatJsonDecimals(saveJson, DecimalPlaces);

                if (ableEncrypt)
                {
                    var saveBytes = Encoding.UTF8.GetBytes(saveJson);
                    File.WriteAllBytes(filePath, Obfuscator.Encrypt(saveBytes, IOUtility.encryptKey));
                }
                else
                {
                    File.WriteAllText(filePath, saveJson);
                }

                RefreshEditor();

                OSK.OSKLogger.Log("Storage",$"[Save File Success]: {fileName + ".json"} \n {filePath}");
            }
            catch (System.Exception ex)
            {
                OSK.OSKLogger.LogError("Storage",$"[Save File Exception]: {fileName + ".json"}  {ex.Message}");
            }
        }

        private string FormatJsonDecimals(string json, int decimalPlaces)
        {
            var dataRegex = Regex.Replace(json, @"\d+\.\d+",
                match => double.TryParse(match.Value, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double number)
                    ? number.ToString("F" + decimalPlaces, System.Globalization.CultureInfo.InvariantCulture)
                    : match.Value);
            OSK.OSKLogger.Log("Storage",$"[Format Json Decimals]: {dataRegex}");
            return dataRegex;
        }

        public T Load<T>(string fileName, bool ableEncrypt = false)
        {
            var path = IOUtility.FilePath(fileName + ".json");
            if (!File.Exists(path))
            {
                OSK.OSKLogger.LogError("Storage",$"[Load File Error]: {fileName + ".json"} NOT found at {path}");
                return default;
            }

            try
            {
                string loadJson;

                if (ableEncrypt)
                {
                    byte[] encryptedBytes = File.ReadAllBytes(path);
                    if (encryptedBytes.Length == 0)
                    {
                        OSK.OSKLogger.LogError("Storage",$"[Load File Error]: {fileName}.json is empty or corrupt");
                        return default;
                    }

                    byte[] decryptedBytes = Obfuscator.Decrypt(encryptedBytes, IOUtility.encryptKey);
                    loadJson = Encoding.UTF8.GetString(decryptedBytes);
                }
                else
                {
                    loadJson = File.ReadAllText(path);
                }

                if (string.IsNullOrWhiteSpace(loadJson))
                {
                    OSK.OSKLogger.LogError("Storage",$"[Load File Error]: {fileName}.json is empty");
                    return default;
                }

                T data = JsonConvert.DeserializeObject<T>(loadJson);
                if (data == null)
                {
                    OSK.OSKLogger.LogError("Storage",$"[Load File Error]: {fileName}.json deserialized to null");
                    return default;
                }

                OSK.OSKLogger.Log("Storage",$"[Load File Success]: {fileName}.json\n{path}");
                return data;
            }
            catch (System.Exception ex)
            {
                OSK.OSKLogger.LogError("Storage",$"[Load File Exception]: {fileName}.json\n{ex.Message}");
                return default;
            }
        }

        public void Delete(string fileName)
        {
            IOUtility.DeleteFile(fileName + ".json");
        }

        public T Query<T>(string fileName, bool condition)
        {
            if (condition)
            {
                return Load<T>(fileName);
            }

            return default;
        }

        private void RefreshEditor()
        {
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
    }
}