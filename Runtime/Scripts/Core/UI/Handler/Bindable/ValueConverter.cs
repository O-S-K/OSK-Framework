using System;
using UnityEngine;

namespace OSK
{
    public abstract class ValueConverter : ScriptableObject
    {
        public abstract object Convert(object value, Type targetType);
        public abstract object ConvertBack(object value, Type targetType);
    }
}
