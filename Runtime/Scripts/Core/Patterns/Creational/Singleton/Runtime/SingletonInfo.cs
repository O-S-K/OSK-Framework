using System;
using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    [Serializable]
    public class SingletonInfo
    {
        public MonoBehaviour instance;
        public DateTime createdTime;

        public SingletonInfo(MonoBehaviour instance)
        {
            this.instance = instance;
            createdTime = DateTime.Now;
        }
    }
}