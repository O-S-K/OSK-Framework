using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace OSK
{
    public static class EnumUtils
    {
        #region Example Usage

        // Read enum info:
        // string name = EnumUtils.GetName(ModuleType.UI);
        // ModuleType[] values = EnumUtils.GetValues<ModuleType>();
        // int index = EnumUtils.GetIndex(ModuleType.UI);
        //
        // Parse enum text:
        // if (EnumUtils.TryParse("UI", out ModuleType module)) { Debug.Log(module); }
        //
        // Random enum:
        // ModuleType random = EnumUtils.Random<ModuleType>();
        //
        // Flags enum:
        // flags = EnumUtils.AddFlag(flags, MyFlags.A);
        // bool hasFlag = EnumUtils.HasFlag(flags, MyFlags.A);

        #endregion

        #region Fields

        private static readonly System.Random m_random = new System.Random();

        #endregion

        #region Info

        // Gets the enum name from a value.
        public static string GetName<T>(T value)
        {
            ValidateEnumType<T>();
            return Enum.GetName(typeof(T), value);
        }

        // Gets all enum names.
        public static string[] GetNames<T>()
        {
            ValidateEnumType<T>();
            return Enum.GetNames(typeof(T));
        }

        // Gets all enum values.
        public static T[] GetValues<T>()
        {
            ValidateEnumType<T>();
            Array values = Enum.GetValues(typeof(T));
            T[] result = new T[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                result[i] = (T)values.GetValue(i);
            }

            return result;
        }

        // Gets total enum value count.
        public static int GetLength<T>()
        {
            ValidateEnumType<T>();
            return Enum.GetValues(typeof(T)).Length;
        }

        // Gets the zero-based index of an enum value in Enum.GetValues order.
        public static int GetIndex<T>(T value)
        {
            T[] values = GetValues<T>();
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < values.Length; i++)
            {
                if (comparer.Equals(values[i], value))
                {
                    return i;
                }
            }

            return -1;
        }

        // Gets enum value by index in Enum.GetValues order.
        public static T GetValueAt<T>(int index)
        {
            T[] values = GetValues<T>();
            if (values.Length == 0)
            {
                return default(T);
            }

            index = Math.Max(0, Math.Min(index, values.Length - 1));
            return values[index];
        }

        #endregion

        #region Text

        // Gets DescriptionAttribute text, or enum name when there is no description.
        public static string GetDescription<T>(T value)
        {
            ValidateEnumType<T>();
            string name = GetName(value);
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            object[] attributes = typeof(T).GetField(name).GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attributes.Length == 0)
            {
                return name;
            }

            return ((DescriptionAttribute)attributes[0]).Description;
        }

        // Converts enum name to readable text by adding spaces before capital letters.
        public static string ToReadableName<T>(T value)
        {
            string name = GetName(value);
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            List<char> chars = new List<char>(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (i > 0 && char.IsUpper(c) && !char.IsWhiteSpace(name[i - 1]))
                {
                    chars.Add(' ');
                }

                chars.Add(c);
            }

            return new string(chars.ToArray());
        }

        #endregion

        #region Random

        // Gets a random value from a provided collection.
        public static T RandomAt<T>(params T[] collection)
        {
            if (collection == null || collection.Length == 0)
            {
                return default(T);
            }

            return collection[m_random.Next(0, collection.Length)];
        }

        // Gets a random enum value.
        public static T Random<T>()
        {
            T[] values = GetValues<T>();
            return RandomAt(values);
        }

        // Gets a random enum value excluding provided values.
        public static T RandomExcept<T>(params T[] excludedValues)
        {
            T[] values = GetValues<T>();
            if (values.Length == 0)
            {
                return default(T);
            }

            List<T> availableValues = new List<T>(values);
            if (excludedValues != null)
            {
                EqualityComparer<T> comparer = EqualityComparer<T>.Default;
                for (int i = availableValues.Count - 1; i >= 0; i--)
                {
                    for (int j = 0; j < excludedValues.Length; j++)
                    {
                        if (comparer.Equals(availableValues[i], excludedValues[j]))
                        {
                            availableValues.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            return availableValues.Count == 0 ? default(T) : availableValues[m_random.Next(0, availableValues.Count)];
        }

        #endregion

        #region Parse

        // Parses text into an enum value.
        public static T Parse<T>(string value)
        {
            ValidateEnumType<T>();
            return (T)Enum.Parse(typeof(T), value);
        }

        // Parses text into an enum value, optionally ignoring case.
        public static T Parse<T>(string value, bool ignoreCase)
        {
            ValidateEnumType<T>();
            return (T)Enum.Parse(typeof(T), value, ignoreCase);
        }

        // Tries to parse text into an enum value, ignoring case by default.
        public static bool TryParse<T>(string value, out T result)
        {
            return TryParse(value, true, out result);
        }

        // Tries to parse text into an enum value.
        public static bool TryParse<T>(string value, bool ignoreCase, out T result)
        {
            ValidateEnumType<T>();
            result = default(T);
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            try
            {
                result = (T)Enum.Parse(typeof(T), value, ignoreCase);
                return Enum.IsDefined(typeof(T), result) || HasFlagsAttribute<T>();
            }
            catch
            {
                result = default(T);
                return false;
            }
        }

        // Checks if text can be parsed into an enum value.
        public static bool IsEnum<T>(string value)
        {
            T result;
            return TryParse(value, out result);
        }

        #endregion

        #region Convert

        // Converts an integer to enum value.
        public static T ToObject<T>(int value)
        {
            ValidateEnumType<T>();
            return (T)Enum.ToObject(typeof(T), value);
        }

        // Converts enum value to int.
        public static int ToInt<T>(T value)
        {
            ValidateEnumType<T>();
            return Convert.ToInt32(value);
        }

        // Checks if an enum value is defined on the enum type.
        public static bool IsDefined<T>(T value)
        {
            ValidateEnumType<T>();
            return Enum.IsDefined(typeof(T), value);
        }

        #endregion

        #region Flags

        // Checks if an enum type has FlagsAttribute.
        public static bool HasFlagsAttribute<T>()
        {
            ValidateEnumType<T>();
            return Attribute.IsDefined(typeof(T), typeof(FlagsAttribute));
        }

        // Checks if a flags enum contains a flag.
        public static bool HasFlag<T>(T value, T flag)
        {
            ValidateEnumType<T>();
            ulong valueNumber = Convert.ToUInt64(value);
            ulong flagNumber = Convert.ToUInt64(flag);
            return (valueNumber & flagNumber) == flagNumber;
        }

        // Adds a flag to a flags enum.
        public static T AddFlag<T>(T value, T flag)
        {
            ValidateEnumType<T>();
            ulong result = Convert.ToUInt64(value) | Convert.ToUInt64(flag);
            return (T)Enum.ToObject(typeof(T), result);
        }

        // Removes a flag from a flags enum.
        public static T RemoveFlag<T>(T value, T flag)
        {
            ValidateEnumType<T>();
            ulong result = Convert.ToUInt64(value) & ~Convert.ToUInt64(flag);
            return (T)Enum.ToObject(typeof(T), result);
        }

        // Toggles a flag on a flags enum.
        public static T ToggleFlag<T>(T value, T flag)
        {
            ValidateEnumType<T>();
            ulong result = Convert.ToUInt64(value) ^ Convert.ToUInt64(flag);
            return (T)Enum.ToObject(typeof(T), result);
        }

        #endregion

        #region Validation

        // Throws when T is not an enum type.
        private static void ValidateEnumType<T>()
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("EnumUtils requires an enum type. Type: " + typeof(T).Name);
            }
        }

        #endregion
    }
}
