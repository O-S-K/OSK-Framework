using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OSK
{
    public class OSKEditor : MonoBehaviour
    {
        [MenuItem("OSK-Framework/Create/ Framework", false,5)]
        public static void CreateWorldOnScene()
        {
            if (FindObjectOfType<Main>() == null)
            {
                PrefabUtility.InstantiatePrefab(Resources.LoadAll<Main>("").First());
            }
        }

        [MenuItem("OSK-Framework/Create/ UIRoot", false,5)]
        public static void CreateHUDOnScene()
        {
            if (FindObjectOfType<RootUI>() == null)
            {
                PrefabUtility.InstantiatePrefab(Resources.LoadAll<RootUI>("").First());
            }
        }
 
        [MenuItem("OSK-Framework/UI/Manager SO")]

        public static void LoadListView()
        {
            FindViewDataSOAssets();
        }

        [MenuItem("OSK-Framework/Sound/Manager SO")]
        public static void LoadListSound()
        {
            FindSoundSOAssets();
        }

        private static void FindViewDataSOAssets()
        {
            string[] guids = AssetDatabase.FindAssets("t:ListViewSO");
            if (guids.Length == 0)
            {
                MyLogger.LogError("No ListViewSO found in the project.");
                return;
            }

            var viewData = new List<ListViewSO>();
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ListViewSO v = AssetDatabase.LoadAssetAtPath<ListViewSO>(path);
                viewData.Add(v);
            }

            if (viewData.Count == 0)
            {
                MyLogger.LogError("No ListViewSO found in the project.");
            }
            else
            {
                foreach (var v in viewData)
                {
                    MyLogger.Log("ListViewSO found: " + v.name);
                    Selection.activeObject = v;
                    EditorGUIUtility.PingObject(v);
                }
            }
        }

        private static void FindSoundSOAssets()
        {
            string[] guids = AssetDatabase.FindAssets("t:ListSoundSO");
            if (guids.Length == 0)
            {
                MyLogger.LogError("No SoundSO found in the project.");
                return;
            }

            var soundData = new List<ListSoundSO>();
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ListSoundSO v = AssetDatabase.LoadAssetAtPath<ListSoundSO>(path);
                soundData.Add(v);
            }

            if (soundData.Count == 0)
            {
                MyLogger.LogError("No SoundSO found in the project.");
            }
            else
            {
                foreach (var v in soundData)
                {
                    MyLogger.Log("SoundSO found: " + v.name);
                    Selection.activeObject = v;
                    EditorGUIUtility.PingObject(v);
                }
            }
        } 
    }
}
