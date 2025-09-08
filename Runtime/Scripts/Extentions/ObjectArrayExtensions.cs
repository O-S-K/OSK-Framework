using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json; 

public static class ObjectArrayExtensions
{
    /// <summary>
    /// Lấy giá trị từ object[] theo index, ép kiểu an toàn sang T.
    /// Hỗ trợ kiểu cơ bản, class, struct, Unity types, List, Dictionary và JSON parse.
    /// </summary>
    public static T Get<T>(this object[] data, int index, T defaultValue = default)
    {
        // Nếu mảng null hoặc index sai → trả default
        if (data == null || index < 0 || index >= data.Length)
            return defaultValue;

        object value = data[index];

        // Nếu null → trả default
        if (value == null)
            return defaultValue;

        try
        {
            // Nếu đã đúng kiểu → trả luôn
            if (value is T tValue)
                return tValue;

            // Nếu là List<T>
            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementType = typeof(T).GetGenericArguments()[0];

                // Nếu value là IList → convert sang List<T>
                if (value is IList list)
                {
                    var resultList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
                    foreach (var item in list)
                        resultList.Add(ConvertElement(item, elementType));
                    return (T)resultList;
                }

                // Nếu value là JSON string → parse thành List<T>
                if (value is string jsonList)
                    return JsonConvert.DeserializeObject<T>(jsonList);

                return defaultValue;
            }

            // Nếu là Dictionary<TKey, TValue>
            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var keyType = typeof(T).GetGenericArguments()[0];
                var valueType = typeof(T).GetGenericArguments()[1];

                // Nếu value là IDictionary → convert sang Dictionary<TKey, TValue>
                if (value is IDictionary dict)
                {
                    var resultDict = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(keyType, valueType));
                    foreach (var key in dict.Keys)
                    {
                        var convertedKey = ConvertElement(key, keyType);
                        var convertedValue = ConvertElement(dict[key], valueType);
                        resultDict.Add(convertedKey, convertedValue);
                    }
                    return (T)resultDict;
                }

                // Nếu value là JSON string → parse thành Dictionary<TKey, TValue>
                if (value is string jsonDict)
                    return JsonConvert.DeserializeObject<T>(jsonDict);

                return defaultValue;
            }

            // Nếu có thể convert cơ bản (int, float, string, bool…)
            if (IsSimpleType(typeof(T)))
                return (T)Convert.ChangeType(value, typeof(T));

            // Nếu là Unity struct → xử lý riêng
            if (TryParseUnityType<T>(value, out T unityResult))
                return unityResult;

            // Nếu là string → thử parse thành class hoặc struct custom
            if (value is string strValue)
            {
                // Nếu T có TryParse(string, out T)
                var tryParse = typeof(T).GetMethod("TryParse",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new[] { typeof(string), typeof(T).MakeByRefType() },
                    null);

                if (tryParse != null)
                {
                    object[] parameters = { strValue, default(T) };
                    bool success = (bool)tryParse.Invoke(null, parameters);
                    if (success)
                        return (T)parameters[1];
                }

                // Nếu T có Parse(string)
                var parse = typeof(T).GetMethod("Parse",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new[] { typeof(string) },
                    null);

                if (parse != null)
                    return (T)parse.Invoke(null, new object[] { strValue });

                // Nếu T có constructor nhận string
                var ctor = typeof(T).GetConstructor(new[] { typeof(string) });
                if (ctor != null)
                    return (T)ctor.Invoke(new object[] { strValue });

                // Nếu T là class → thử parse JSON vào object T
                try
                {
                    return JsonConvert.DeserializeObject<T>(strValue);
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Thử lấy giá trị, trả về true nếu thành công.
    /// </summary>
    public static bool TryGet<T>(this object[] data, int index, out T value)
    {
        value = default;
        try
        {
            value = data.Get<T>(index);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ===========================
    // Helpers
    // ===========================

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive
               || type.IsEnum
               || type.Equals(typeof(string))
               || type.Equals(typeof(decimal))
               || type.Equals(typeof(DateTime));
    }

    private static object ConvertElement(object value, Type targetType)
    {
        if (value == null) return null;

        try
        {
            if (value.GetType() == targetType)
                return value;

            if (IsSimpleType(targetType))
                return Convert.ChangeType(value, targetType);

            return JsonConvert.DeserializeObject(JsonConvert.SerializeObject(value), targetType);
        }
        catch
        {
            return null;
        }
    }

    private static bool TryParseUnityType<T>(object value, out T result)
    {
        result = default;

        if (typeof(T) == typeof(Vector2))
        {
            if (value is Vector2 v2) { result = (T)(object)v2; return true; }
            if (value is string s && TryParseVector2(s, out var vec2)) { result = (T)(object)vec2; return true; }
        }
        else if (typeof(T) == typeof(Vector3))
        {
            if (value is Vector3 v3) { result = (T)(object)v3; return true; }
            if (value is string s && TryParseVector3(s, out var vec3)) { result = (T)(object)vec3; return true; }
        }
        else if (typeof(T) == typeof(Color))
        {
            if (value is Color c) { result = (T)(object)c; return true; }
            if (value is string s && ColorUtility.TryParseHtmlString(s, out var color)) { result = (T)(object)color; return true; }
        }
        else if (typeof(T) == typeof(Quaternion))
        {
            if (value is Quaternion q) { result = (T)(object)q; return true; }
            if (value is string s && TryParseQuaternion(s, out var quat)) { result = (T)(object)quat; return true; }
        }

        return false;
    }

    private static bool TryParseVector2(string s, out Vector2 result)
    {
        result = default;
        try
        {
            s = s.Trim('(', ')');
            var parts = s.Split(',');
            result = new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
            return true;
        }
        catch { return false; }
    }

    private static bool TryParseVector3(string s, out Vector3 result)
    {
        result = default;
        try
        {
            s = s.Trim('(', ')');
            var parts = s.Split(',');
            result = new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
            return true;
        }
        catch { return false; }
    }

    private static bool TryParseQuaternion(string s, out Quaternion result)
    {
        result = default;
        try
        {
            s = s.Trim('(', ')');
            var parts = s.Split(',');
            result = new Quaternion(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
            return true;
        }
        catch { return false; }
    }
}
