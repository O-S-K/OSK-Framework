using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

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
        
        [TitleGroup("Tick System", "Centralized Execution", alignment: TitleAlignments.Centered)]
        [ShowInInspector, ReadOnly, ListDrawerSettings(Expanded = true)]
        private static List<IUpdateable> Updateables => k_Updateables;

        [ShowInInspector, ReadOnly]
        private static List<IFixedUpdateable> FixedUpdateables => k_FixedUpdateables;

        [ShowInInspector, ReadOnly]
        private static List<ILateUpdateable> LateUpdateables => k_LateUpdateables;

        private static readonly List<IUpdateable> k_Updateables = new();
        private static readonly List<IFixedUpdateable> k_FixedUpdateables = new();
        private static readonly List<ILateUpdateable> k_LateUpdateables = new();
        
        // Snapshots, because a tick may unregister itself and must not make the next item get skipped.
        private static readonly List<IUpdateable> k_UpdateSnapshot = new();
        private static readonly List<IFixedUpdateable> k_FixedUpdateSnapshot = new();
        private static readonly List<ILateUpdateable> k_LateUpdateSnapshot = new();

        private static bool k_IsPaused = false;
        public static void SetPause(bool pause) => k_IsPaused = pause;

        private void Update()
        {
            if (k_IsPaused) return;
            k_UpdateSnapshot.Clear();
            k_UpdateSnapshot.AddRange(k_Updateables);
            for (int i = 0; i < k_UpdateSnapshot.Count; i++)
            {
                // One failing module must not stop the rest of the frame, as a normal Unity Update would not.
                try { k_UpdateSnapshot[i].OnUpdate(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        private void FixedUpdate()
        {
            if (k_IsPaused) return;
            k_FixedUpdateSnapshot.Clear();
            k_FixedUpdateSnapshot.AddRange(k_FixedUpdateables);
            for (int i = 0; i < k_FixedUpdateSnapshot.Count; i++)
            {
                try { k_FixedUpdateSnapshot[i].OnFixedUpdate(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        private void LateUpdate()
        {
            if (k_IsPaused) return;
            k_LateUpdateSnapshot.Clear();
            k_LateUpdateSnapshot.AddRange(k_LateUpdateables);
            for (int i = 0; i < k_LateUpdateSnapshot.Count; i++)
            {
                try { k_LateUpdateSnapshot[i].OnLateUpdate(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

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

            // Add to update lists if applicable
            RegisterTick(gameFrameworkComponent);
        }

        public static void RegisterTick(object obj)
        {
            if (obj is IUpdateable u && !k_Updateables.Contains(u)) k_Updateables.Add(u);
            if (obj is IFixedUpdateable f && !k_FixedUpdateables.Contains(f)) k_FixedUpdateables.Add(f);
            if (obj is ILateUpdateable l && !k_LateUpdateables.Contains(l)) k_LateUpdateables.Add(l);
        }

        public static void UnRegisterTick(object obj)
        {
            if (obj is IUpdateable u) k_Updateables.Remove(u);
            if (obj is IFixedUpdateable f) k_FixedUpdateables.Remove(f);
            if (obj is ILateUpdateable l) k_LateUpdateables.Remove(l);
        }
        
        internal static void UnRegister(GameFrameworkComponent gameFrameworkComponent)
        {
            if (gameFrameworkComponent == null)
            {
                OSK.MyLogger.Log("Game Framework component is invalid.");
                return;
            }

            // By instance: a destroyed duplicate must not unregister the live module of the same type.
            if (SGameFrameworkComponents.Remove(gameFrameworkComponent))
            {
                UnRegisterTick(gameFrameworkComponent);
            }
        }
        
        public static void Shutdown(ShutdownType shutdownType)
        {
            OSK.MyLogger.Log($"Shutdown Game Framework ({shutdownType})...");
            SGameFrameworkComponents.Clear();
            k_Updateables.Clear();
            k_FixedUpdateables.Clear();
            k_LateUpdateables.Clear();

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