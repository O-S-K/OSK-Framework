using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OSK
{
    public static class ComponentExtensions
    {
        #region Example Usage

        // Get or add:
        // Rigidbody rb = gameObject.GetOrAdd<Rigidbody>();
        // Canvas canvas = transform.GetOrAdd<Canvas>();
        //
        // Interfaces:
        // bool hasPoolable = gameObject.HasComponentOrInterface<IPoolable>();
        // IPoolable poolable = gameObject.GetComponentOrInterface<IPoolable>();
        //
        // Child creation:
        // Image image = root.AddChild<Image>("Icon");
        // GameObject loaded = root.LoadChild("Prefabs/MyView");
        //
        // Active state:
        // component.Deactivate();
        // component.Activate();
        //
        // Hierarchy:
        // bool insideRoot = child.IsParentedBy(root);
        // root.DestroyAllChildrenImmediately();

        #endregion

        #region Get Or Add

        // Gets an existing component or adds one to the GameObject.
        public static T GetOrAdd<T>(this GameObject gameObject) where T : Component
        {
            if (gameObject == null)
            {
                return null;
            }

            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        // Gets an existing component or adds one to the Transform GameObject.
        public static T GetOrAdd<T>(this Transform transform) where T : Component
        {
            return transform != null ? transform.gameObject.GetOrAdd<T>() : null;
        }

        // Gets an existing component or adds one to the MonoBehaviour GameObject.
        public static T GetOrAdd<T>(this MonoBehaviour mono) where T : Component
        {
            return mono != null ? mono.gameObject.GetOrAdd<T>() : null;
        }

        #endregion

        #region Component Or Interface

        // Checks if a GameObject has a component or interface.
        public static bool HasComponentOrInterface<T>(this GameObject obj) where T : class
        {
            return GetComponentOrInterface<T>(obj) != null;
        }

        // Gets first component or interface from a GameObject.
        public static T GetComponentOrInterface<T>(this GameObject obj) where T : class
        {
            if (obj == null)
            {
                return null;
            }

            Component[] components = obj.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                T typed = components[i] as T;
                if (typed != null)
                {
                    return typed;
                }
            }

            return null;
        }

        // Gets all components or interfaces from a GameObject.
        public static IEnumerable<T> GetAllComponentsOrInterfaces<T>(this GameObject obj) where T : class
        {
            if (obj == null)
            {
                yield break;
            }

            Component[] components = obj.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                T typed = components[i] as T;
                if (typed != null)
                {
                    yield return typed;
                }
            }
        }

        // Gets all components or interfaces from children.
        private static IEnumerable<T> GetAllComponentsOrInterfacesInChildren<T>(this GameObject obj) where T : class
        {
            if (obj == null)
            {
                yield break;
            }

            Component[] components = obj.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                T typed = components[i] as T;
                if (typed != null)
                {
                    yield return typed;
                }
            }
        }

        // Checks if a Component GameObject has a component or interface.
        public static bool HasComponentOrInterface<T>(this Component component) where T : class
        {
            return component != null && HasComponentOrInterface<T>(component.gameObject);
        }

        // Gets first component or interface from a Component GameObject.
        public static T GetComponentOrInterface<T>(this Component component) where T : class
        {
            return component != null ? GetComponentOrInterface<T>(component.gameObject) : null;
        }

        // Gets all components or interfaces from a Component GameObject.
        public static IEnumerable<T> GetAllComponentsOrInterfaces<T>(this Component component) where T : class
        {
            return component != null ? GetAllComponentsOrInterfaces<T>(component.gameObject) : Empty<T>();
        }

        // Gets all components or interfaces from a Component GameObject children.
        public static IEnumerable<T> GetAllComponentsOrInterfacesInChildren<T>(this Component component) where T : class
        {
            return component != null ? GetAllComponentsOrInterfacesInChildren<T>(component.gameObject) : Empty<T>();
        }

        #endregion

        #region Children

        // Adds a child GameObject with component T.
        public static T AddChild<T>(this GameObject parent) where T : Component
        {
            return AddChild<T>(parent, typeof(T).Name);
        }

        // Parents an existing child GameObject under parent and returns parent.
        public static GameObject AddChild(this GameObject parent, GameObject child, bool worldPositionStays = false)
        {
            if (parent == null || child == null)
            {
                return parent;
            }

            child.transform.SetParent(parent.transform, worldPositionStays);
            return parent;
        }

        // Adds a named child GameObject with component T.
        public static T AddChild<T>(this GameObject parent, string name) where T : Component
        {
            GameObject obj = AddChild(parent, name, typeof(T));
            return obj != null ? obj.GetComponent<T>() : null;
        }

        // Adds a child GameObject with components.
        public static GameObject AddChild(this GameObject parent, params Type[] components)
        {
            return AddChild(parent, "Game Object", components);
        }

        // Adds a named child GameObject with components.
        public static GameObject AddChild(this GameObject parent, string name, params Type[] components)
        {
            GameObject obj = components == null || components.Length == 0
                ? new GameObject(name)
                : new GameObject(name, components);

            if (parent != null)
            {
                SetParent(obj.transform, parent.transform, obj.transform is RectTransform);
            }

            return obj;
        }

        // Loads a prefab from Resources and parents it under a GameObject.
        public static GameObject LoadChild(this GameObject parent, string resourcePath)
        {
            return parent != null ? LoadChild(parent.transform, resourcePath) : LoadResource(resourcePath);
        }

        // Loads a prefab from Resources and parents it under a Transform.
        public static GameObject LoadChild(this Transform parent, string resourcePath)
        {
            GameObject obj = LoadResource(resourcePath);
            if (obj != null && parent != null)
            {
                SetParent(obj.transform, parent, obj.transform is RectTransform);
            }

            return obj;
        }

        #endregion

        #region Destroy

        // Destroys all children immediately from a GameObject.
        public static void DestroyAllChildrenImmediately(this GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            DestroyAllChildrenImmediately(obj.transform);
        }

        // Destroys all children immediately from a Transform.
        public static void DestroyAllChildrenImmediately(this Transform trans)
        {
            if (trans == null)
            {
                return;
            }

            while (trans.childCount != 0)
            {
                Object.DestroyImmediate(trans.GetChild(0).gameObject);
            }
        }

        #endregion

        #region Active State

        // Sets component GameObject inactive.
        public static void Deactivate(this Component component)
        {
            if (component != null)
            {
                component.gameObject.SetActive(false);
            }
        }

        // Sets component GameObject active.
        public static void Activate(this Component component)
        {
            if (component != null)
            {
                component.gameObject.SetActive(true);
            }
        }

        // Sets component GameObject active state.
        public static void SetActive(this Component component, bool active)
        {
            if (component != null)
            {
                component.gameObject.SetActive(active);
            }
        }

        #endregion

        #region Hierarchy

        // Checks if a GameObject is parented by another GameObject.
        public static bool IsParentedBy(this GameObject obj, GameObject parent)
        {
            if (obj == null || parent == null)
            {
                return false;
            }

            Transform current = obj.transform.parent;
            while (current != null)
            {
                if (current.gameObject == parent)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        // Checks if a Component GameObject is parented by a GameObject.
        public static bool IsParentedBy(this Component component, GameObject parent)
        {
            return component != null && IsParentedBy(component.gameObject, parent);
        }

        // Finds a component in this object or its parents.
        public static T GetComponentInParents<T>(this Component component) where T : Component
        {
            if (component == null)
            {
                return null;
            }

            Transform current = component.transform;
            while (current != null)
            {
                T result = current.GetComponent<T>();
                if (result != null)
                {
                    return result;
                }

                current = current.parent;
            }

            return null;
        }

        #endregion

        #region Internals

        // Loads and instantiates a GameObject resource.
        private static GameObject LoadResource(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                return null;
            }

            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            return prefab != null ? Object.Instantiate(prefab) : null;
        }

        // Sets parent with consistent RectTransform handling.
        private static void SetParent(Transform child, Transform parent, bool worldPositionStays)
        {
            if (child == null || parent == null)
            {
                return;
            }

            child.SetParent(parent, worldPositionStays);
        }

        // Returns an empty enumerable.
        private static IEnumerable<T> Empty<T>()
        {
            yield break;
        }

        #endregion
    }
}
