using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace OSK
{
    public static class PrefUtils
    {
        // ---------- Bool ----------
        public static void SetBool(string key, bool value) => PlayerPrefs.SetInt(key, value ? 1 : 0);
        public static bool GetBool(string key, bool defaultValue = false) => PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;

        // ---------- Float ----------
        public static void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);
        public static float GetFloat(string key, float defaultValue = 0f) => PlayerPrefs.GetFloat(key, defaultValue);

        // ---------- Int ----------
        public static void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
        public static int GetInt(string key, int defaultValue = 0) => PlayerPrefs.GetInt(key, defaultValue);

        // ---------- String ----------
        public static void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
        public static string GetString(string key, string defaultValue = "") => PlayerPrefs.GetString(key, defaultValue);

        // ---------- Vector2 ----------
        public static void SetVector2(string key, Vector2 v) => SetString(key, JsonUtility.ToJson(v,false));
        public static Vector2 GetVector2(string key, Vector2 defaultValue = default)
        {
            string json = GetString(key,null);
            return string.IsNullOrEmpty(json) ? defaultValue : JsonUtility.FromJson<Vector2>(json);
        }

        // ---------- Vector3 ----------
        public static void SetVector3(string key, Vector3 v) => SetString(key, JsonUtility.ToJson(v,false));
        public static Vector3 GetVector3(string key, Vector3 defaultValue = default)
        {
            string json = GetString(key,null);
            return string.IsNullOrEmpty(json) ? defaultValue : JsonUtility.FromJson<Vector3>(json);
        }

        // ---------- Quaternion ----------
        public static void SetQuaternion(string key, Quaternion q) => SetString(key, JsonUtility.ToJson(q,false));
        public static Quaternion GetQuaternion(string key, Quaternion defaultValue = default)
        {
            string json = GetString(key,null);
            return string.IsNullOrEmpty(json) ? defaultValue : JsonUtility.FromJson<Quaternion>(json);
        }

        // ---------- Color ----------
        public static void SetColor(string key, Color c) => SetString(key, JsonUtility.ToJson(c,false));
        public static Color GetColor(string key, Color defaultValue = default)
        {
            string json = GetString(key,null);
            return string.IsNullOrEmpty(json) ? defaultValue : JsonUtility.FromJson<Color>(json);
        }

        // ---------- List<T> ----------
        [System.Serializable] private class ListWrapper<T> { public List<T> list; }
        public static void SetList<T>(string key, List<T> list)
        {
            var wrapper = new ListWrapper<T> { list = list };
            SetString(key, JsonUtility.ToJson(wrapper,false));
        }
        public static List<T> GetList<T>(string key)
        {
            string json = GetString(key,null);
            if(string.IsNullOrEmpty(json)) return new List<T>();
            return JsonUtility.FromJson<ListWrapper<T>>(json).list;
        }

        // ---------- Dictionary<string,int> ----------
        [System.Serializable] private class DictWrapper
        {
            public List<string> keys;
            public List<int> values;
        }
        public static void SetDictionary(string key, Dictionary<string,int> dict)
        {
            var wrapper = new DictWrapper { keys = new List<string>(), values = new List<int>() };
            foreach(var kv in dict) { wrapper.keys.Add(kv.Key); wrapper.values.Add(kv.Value); }
            SetString(key, JsonUtility.ToJson(wrapper,false));
        }
        public static Dictionary<string,int> GetDictionary(string key)
        {
            string json = GetString(key,null);
            if(string.IsNullOrEmpty(json)) return new Dictionary<string,int>();
            var wrapper = JsonUtility.FromJson<DictWrapper>(json);
            var dict = new Dictionary<string,int>();
            for(int i=0;i<wrapper.keys.Count;i++) dict[wrapper.keys[i]] = wrapper.values[i];
            return dict;
        }
    }
}

