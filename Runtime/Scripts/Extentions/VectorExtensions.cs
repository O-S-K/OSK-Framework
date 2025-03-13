using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    public static class VectorExtensions 
    {
        // example : transform.position =  transform.position.With(y: 5);
        public static Vector3 With(this Vector3 vector3, float? x = null, float? y = null, float? z = null)
        {
            return new Vector3(x ?? vector3.x, y ?? vector3.y, z ?? vector3.z);
        }

        public static Vector3 WithX(this Vector3 vector3, float x)
        {
            return new Vector3(x, vector3.y, vector3.z);
        }

        public static Vector3 WithY(this Vector3 vector3, float y)
        {
            return new Vector3(vector3.x, y, vector3.z);
        }

        public static Vector3 WithZ(this Vector3 vector3, float z)
        {
            return new Vector3(vector3.x, vector3.y, z);
        }

        // example : transform.position =  transform.position.Add(x: 1, y: 5);
        public static Vector3 Add(this Vector3 vector3, float? x = null, float? y = null, float? z = null)
        {
            return new Vector3(vector3.x + (x ?? 0), vector3.y + (y ?? 0), vector3.z + (z ?? 0));
        }
        
        
        public static float Remap(this float value, float from1, float to1, float from2, float to2)
        {
            float v = (value - from1) / (to1 - from1) * (to2 - from2) + from2;
            return v;
        }
    }
}
