using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    internal static class InputInjector
    {
        private static readonly Dictionary<string, bool> injectedButtons = new();

        public static void Inject(string id, bool pressed) => injectedButtons[id] = pressed;

        public static bool Get(string id) => injectedButtons.TryGetValue(id, out var v) && v;
        
        public static void Clear()
        {
            injectedButtons.Clear();
        }
    }
}