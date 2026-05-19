using System;
using UnityEngine;

namespace OSK
{
    [CreateAssetMenu(fileName = "IntToStringConverter", menuName = "OSK/Converters/Int To String")]
    public class IntToStringConverter : ValueConverter
    {
        public string format = "{0}";

        public override object Convert(object value, Type targetType)
        {
            if (value is int intValue)
            {
                return string.Format(format, intValue);
            }
            return value?.ToString() ?? "";
        }

        public override object ConvertBack(object value, Type targetType)
        {
            if (value is string strValue && int.TryParse(strValue, out int result))
            {
                return result;
            }
            return 0;
        }
    }

    [CreateAssetMenu(fileName = "BoolToActiveConverter", menuName = "OSK/Converters/Bool To Active")]
    public class BoolToActiveConverter : ValueConverter
    {
        public bool invert = false;

        public override object Convert(object value, Type targetType)
        {
            if (value is bool boolValue)
            {
                return invert ? !boolValue : boolValue;
            }
            return false;
        }

        public override object ConvertBack(object value, Type targetType)
        {
            if (value is bool boolValue)
            {
                return invert ? !boolValue : boolValue;
            }
            return false;
        }
    }
}
