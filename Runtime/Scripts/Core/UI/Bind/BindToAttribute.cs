using System;

namespace OSK
{

    [AttributeUsage(AttributeTargets.Field)]
    public class BindToAttribute : Attribute
    {
        public string PropertyName;
        public string Target;   // Tên field phụ (Text, TMP_Text...)
        public string Format;   // "{0:0.0%}", "{0:N0}", ...

        public BindToAttribute(string propertyName, string target = null, string format = null)
        {
            PropertyName = propertyName;
            Target = target;
            Format = format;
        }
    }
}
