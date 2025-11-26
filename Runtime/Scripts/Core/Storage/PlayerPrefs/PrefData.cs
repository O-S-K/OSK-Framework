using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace OSK
{
    public static class PrefData
    {
        #region 1. CONFIGURATION & KEYS
        public static bool IsEncrypt = false;
        private static string _encryptionKey = "1234567890123456"; 
        private static string _initializationVector = "8855221144778899";
        
        #endregion

        #region 2. GENERIC API (RECOMMENDED)
        /// <summary>
        /// Set any data type (Int, String, List, Class, Vector...).
        /// Example: PrefData.Set<int>("myIntKey", 42);
        /// </summary>
        public static void Set<T>(string key, T value)
        {
            string content = Serialize(value);

            if (IsEncrypt)
            {
                content = Encrypt(content);
            }

            PlayerPrefs.SetString(key, content);
        }

        /// <summary>
        /// Load any data type (Int, String, List, Class, Vector...).
        /// Example: int myInt = PrefData.Load<int>("myIntKey", 0);
        /// </summary>
        public static T Load<T>(string key, T defaultValue = default)
        {
            if (!PlayerPrefs.HasKey(key)) return defaultValue;

            string content = PlayerPrefs.GetString(key);

            if (IsEncrypt)
            {
                if (string.IsNullOrEmpty(content)) return defaultValue;
                
                try 
                {
                    content = Decrypt(content);
                }
                catch 
                {
                    return defaultValue;
                }
            }

            return Deserialize<T>(content, defaultValue);
        }

        #endregion

        #region 3. MANAGEMENT API
        public static bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public static void Save() => PlayerPrefs.Save();
        public static void Delete(string key) => PlayerPrefs.DeleteKey(key);
        public static void DeleteAll() => PlayerPrefs.DeleteAll();

        #endregion

        #region 4. PRIMITIVE TYPES API (Legacy Support)

        // ---------- Bool ----------
        public static void SetBool(string key, bool value)
        {
            if (IsEncrypt) SetString(key, value.ToString());
            else PlayerPrefs.SetInt(key, value ? 1 : 0);
        }

        public static bool GetBool(string key, bool defaultValue = false)
        {
            if (IsEncrypt)
            {
                string decryptedVal = GetString(key, "");
                if (bool.TryParse(decryptedVal, out bool result)) return result;
                return defaultValue;
            }
            return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
        }

        // ---------- Float ----------
        public static void SetFloat(string key, float value)
        {
            if (IsEncrypt) SetString(key, value.ToString());
            else PlayerPrefs.SetFloat(key, value);
        }

        public static float GetFloat(string key, float defaultValue = 0f)
        {
            if (IsEncrypt)
            {
                string val = GetString(key, "");
                if (float.TryParse(val, out float result)) return result;
                return defaultValue;
            }
            return PlayerPrefs.GetFloat(key, defaultValue);
        }

        // ---------- Int ----------
        public static void SetInt(string key, int value)
        {
            if (IsEncrypt) SetString(key, value.ToString());
            else PlayerPrefs.SetInt(key, value);
        }

        public static int GetInt(string key, int defaultValue = 0)
        {
            if (IsEncrypt)
            {
                string val = GetString(key, "");
                if (int.TryParse(val, out int result)) return result;
                return defaultValue;
            }
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        // ---------- String ----------
        public static void SetString(string key, string value)
        {
            if (IsEncrypt)
            {
                string encryptedValue = Encrypt(value);
                PlayerPrefs.SetString(key, encryptedValue);
            }
            else
            {
                PlayerPrefs.SetString(key, value);
            }
        }

        public static string GetString(string key, string defaultValue = "")
        {
            string value = PlayerPrefs.GetString(key, defaultValue);
            
            if (IsEncrypt)
            {
                if (value == defaultValue) return defaultValue;
                string decrypted = Decrypt(value);
                if (string.IsNullOrEmpty(decrypted) && !string.IsNullOrEmpty(value)) return defaultValue; 
                return decrypted;
            }
            return value;
        }

        #endregion

        #region 5. UNITY TYPES API (Vector, Quaternion, Color)

        // ---------- Vector2 ----------
        public static void SetVector2(string key, Vector2 v) => SetString(key, JsonUtility.ToJson(v, false));
        public static Vector2 GetVector2(string key, Vector2 defaultValue = default)
        {
            string json = GetString(key, null); 
            return string.IsNullOrEmpty(json) ? defaultValue : JsonUtility.FromJson<Vector2>(json);
        }

        // ---------- Vector3 ----------
        public static void SetVector3(string key, Vector3 v) => SetString(key, JsonUtility.ToJson(v, false));
        public static Vector3 GetVector3(string key, Vector3 defaultValue = default)
        {
            string json = GetString(key, null);
            return string.IsNullOrEmpty(json) ? defaultValue : JsonUtility.FromJson<Vector3>(json);
        }

        // ---------- Quaternion ----------
        public static void SetQuaternion(string key, Quaternion q) => SetString(key, JsonUtility.ToJson(q, false));
        public static Quaternion GetQuaternion(string key, Quaternion defaultValue = default)
        {
            string json = GetString(key, null);
            return string.IsNullOrEmpty(json) ? defaultValue : JsonUtility.FromJson<Quaternion>(json);
        }

        // ---------- Color ----------
        public static void SetColor(string key, Color c) => SetString(key, JsonUtility.ToJson(c, false));
        public static Color GetColor(string key, Color defaultValue = default)
        {
            string json = GetString(key, null);
            return string.IsNullOrEmpty(json) ? defaultValue : JsonUtility.FromJson<Color>(json);
        }

        #endregion

        #region 6. COLLECTIONS API (List, Dictionary)

        // ---------- List<T> ----------
        [System.Serializable]
        private class ListWrapper<T> { public List<T> list; }

        public static void SetList<T>(string key, List<T> list)
        {
            var wrapper = new ListWrapper<T> { list = list };
            SetString(key, JsonUtility.ToJson(wrapper, false));
        }

        public static List<T> GetList<T>(string key)
        {
            string json = GetString(key, null);
            if (string.IsNullOrEmpty(json)) return new List<T>();
            return JsonUtility.FromJson<ListWrapper<T>>(json).list;
        }

        // ---------- Dictionary<string,int> ----------
        [System.Serializable]
        private class DictWrapper { public List<string> keys; public List<int> values; }

        public static void SetDictionary(string key, Dictionary<string, int> dict)
        {
            var wrapper = new DictWrapper { keys = new List<string>(), values = new List<int>() };
            foreach (var kv in dict) { wrapper.keys.Add(kv.Key); wrapper.values.Add(kv.Value); }
            SetString(key, JsonUtility.ToJson(wrapper, false));
        }

        public static Dictionary<string, int> GetDictionary(string key)
        {
            string json = GetString(key, null);
            if (string.IsNullOrEmpty(json)) return new Dictionary<string, int>();
            try 
            {
                var wrapper = JsonUtility.FromJson<DictWrapper>(json);
                var dict = new Dictionary<string, int>();
                for (int i = 0; i < wrapper.keys.Count; i++) dict[wrapper.keys[i]] = wrapper.values[i];
                return dict;
            }
            catch { return new Dictionary<string, int>(); }
        }

        #endregion

        #region 7. INTERNAL CORE: SERIALIZATION

        [System.Serializable]
        private class Wrapper<T> { public T data; }

        private static string Serialize<T>(T value)
        {
            if (typeof(T) == typeof(string)) return value.ToString();
            if (typeof(T) == typeof(int)) return value.ToString();
            if (typeof(T) == typeof(float)) return value.ToString();
            if (typeof(T) == typeof(bool)) return value.ToString();

            Wrapper<T> wrapper = new Wrapper<T> { data = value };
            return JsonUtility.ToJson(wrapper);
        }
        
        private static T Deserialize<T>(string content, T defaultValue)
        {
            if (string.IsNullOrEmpty(content)) return defaultValue;

            try
            {
                Type type = typeof(T);
                if (type == typeof(string)) return (T)(object)content;
                if (type == typeof(int)) return (T)(object)int.Parse(content);
                if (type == typeof(float)) return (T)(object)float.Parse(content);
                if (type == typeof(bool)) return (T)(object)bool.Parse(content);
                if (type.IsEnum) return (T)Enum.Parse(type, content);

                Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(content);
                return wrapper != null ? wrapper.data : defaultValue;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PrefData] Parse error for key type {typeof(T)}: {e.Message}. Returning default.");
                return defaultValue;
            }
        }

        #endregion

        #region 8. INTERNAL CORE: ENCRYPTION

        private static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return "";
            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(_encryptionKey);
                    aes.IV = Encoding.UTF8.GetBytes(_initializationVector);
                    using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    {
                        byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                        return Convert.ToBase64String(encryptedBytes);
                    }
                }
            }
            catch { return plainText; }
        }

        private static string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText)) return "";
            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(_encryptionKey);
                    aes.IV = Encoding.UTF8.GetBytes(_initializationVector);
                    using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    {
                        byte[] plainBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                        return Encoding.UTF8.GetString(plainBytes);
                    }
                }
            }
            catch { return ""; }
        }

        #endregion
    }
}