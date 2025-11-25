using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    public interface IPoolable
    {
        void OnSpawn();  
        void OnDespawn(); 
    }
}
