using System;
using UnityEngine;

namespace OSK
{
    public enum ShutdownType : byte
    {
        None = 0,
        Restart,
        Quit,
    }

    [DefaultExecutionOrder(-1000)]
    public partial class Main : MonoBehaviour
    { 
        public static readonly GameFrameworkLinkedList<GameFrameworkComponent> SGameFrameworkComponents = new();

        public static T GetModule<T>() where T : GameFrameworkComponent
        {
            return (T)GetModule(typeof(T));
        }

        private static GameFrameworkComponent GetModule(Type type)
        {
            var current = SGameFrameworkComponents.First;
            while (current != null)
            {
                if (current.Value.GetType() == type)
                {
                    return current.Value;
                }
                current = current.Next;
            }

            return null;
        } 

        public static GameFrameworkComponent GetModule(string typeName)
        {
            var current = SGameFrameworkComponents.First;
            while (current != null)
            {
                Type type = current.Value.GetType();
                if (type.FullName == typeName || type.Name == typeName)
                {
                    return current.Value;
                }
                current = current.Next;
            }

            return null;
        }
    
        internal static void Register(GameFrameworkComponent gameFrameworkComponent)
        {
            if (gameFrameworkComponent == null)
            {
                OSK.MyLogger.Log("Game Framework component is invalid.");
                return;
            }

            Type type = gameFrameworkComponent.GetType();

            var current = SGameFrameworkComponents.First;
            while (current != null)
            {
                if (current.Value.GetType() == type)
                {
                    OSK.MyLogger.Log($"Game Framework component type {type.FullName} is already exist.");
                    return;
                }

                current = current.Next;
            }

            SGameFrameworkComponents.AddLast(gameFrameworkComponent);
        }
        
        internal static void UnRegister(GameFrameworkComponent gameFrameworkComponent)
        {
            if (gameFrameworkComponent == null)
            {
                OSK.MyLogger.Log("Game Framework component is invalid.");
                return;
            }

            Type type = gameFrameworkComponent.GetType();

            var current = SGameFrameworkComponents.First;
            while (current != null)
            {
                if (current.Value.GetType() == type)
                {
                    SGameFrameworkComponents.Remove(current);
                    return;
                }

                current = current.Next;
            }
        }
        
        public static void Shutdown(ShutdownType shutdownType)
        {
            OSK.MyLogger.Log($"Shutdown Game Framework ({shutdownType})...");
            SGameFrameworkComponents.Clear();

            if (shutdownType == ShutdownType.None)
            {
                return;
            }

            if (shutdownType == ShutdownType.Restart)
            {
                var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                UnityEngine.SceneManagement.SceneManager.LoadScene(currentScene.buildIndex);
                return;
            }

            if (shutdownType == ShutdownType.Quit)
            {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
                return;
            }
        }
    }
}