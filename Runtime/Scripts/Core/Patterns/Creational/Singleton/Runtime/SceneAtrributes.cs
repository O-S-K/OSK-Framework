using System;

namespace OSK
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SingletonGlobalAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class SingletonSceneAttribute : Attribute
    {
        public string[] Scenes { get; }
        public SingletonSceneAttribute(params string[] scenes)
        {
            Scenes = scenes;
        }
    }
}
