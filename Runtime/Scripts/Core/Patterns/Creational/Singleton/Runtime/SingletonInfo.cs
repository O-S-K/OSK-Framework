using System;
using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    [Serializable]
    public class SingletonInfo
    {
        public MonoBehaviour instance;
        public List<string> allowedScenes; // null = global
        public DateTime createdTime;

        public bool IsGlobal => allowedScenes == null || allowedScenes.Count == 0;
        public bool IsValidInScene(string scene) => IsGlobal || allowedScenes.Contains(scene);

        public SingletonInfo(MonoBehaviour instance, List<string> allowedScenes = null)
        {
            this.instance = instance;
            this.allowedScenes = allowedScenes;
            createdTime = DateTime.Now;
        }
    }
}