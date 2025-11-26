#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine; 
using System.IO;

namespace OSK
{
public class DataEditor : Editor
{ 
    
    [MenuItem("OSK-Framework/Tools/Save/Open Persistent Data" )]
    private static void OpenPersistentDataPath()
    { 
        string path = IOUtility.GetDirectoryPath(IOUtility.StorageDirectory.PersistentData);
        if (string.IsNullOrEmpty(path))
        {
            OSKLogger.LogWarning("Path is null or empty");
            return;
        }
        Application.OpenURL(path);
    }
    
    [MenuItem("OSK-Framework/Tools/Save/Open StreamingAssets Data" )]
    private static void OpenStreamingAssetsDataPath()
    { 
        string path = IOUtility.GetDirectoryPath(IOUtility.StorageDirectory.StreamingAssets);
        if (string.IsNullOrEmpty(path))
        {
            OSKLogger.LogWarning("Path is null or empty");
            return;
        }
        Application.OpenURL(path);
    }
    
    [MenuItem("OSK-Framework/Tools/Save/Open DataPath Data" )]
    private static void OpenDataPathDataPath()
    { 
        string path = IOUtility.GetDirectoryPath(IOUtility.StorageDirectory.DataPath);
        if (string.IsNullOrEmpty(path))
        {
            OSKLogger.LogWarning("Path is null or empty");
            return;
        }
        Application.OpenURL(path);
    }
    
    [MenuItem("OSK-Framework/Tools/Save/Open TemporaryCache Data" )]
    private static void OpenTemporaryCacheDataPath()
    { 
        string path = IOUtility.GetDirectoryPath(IOUtility.StorageDirectory.TemporaryCache);
        if (string.IsNullOrEmpty(path))
        {
            OSKLogger.LogWarning("Path is null or empty");
            return;
        }
        Application.OpenURL(path);
    }
    
    [MenuItem("OSK-Framework/Tools/Save/Open Custom Data" )]
    private static void OpenCustomDataPath()
    { 
        string path = IOUtility.GetDirectoryPath(IOUtility.StorageDirectory.Custom);
        if (string.IsNullOrEmpty(path))
        {
            OSKLogger.LogWarning("Path is null or empty");
            return;
        }
        Application.OpenURL(path);
    }
    

    [MenuItem("OSK-Framework/Tools/Save/Clear Persistent Data Path")]
    private static void ClearPersistentDataPath()
    {
        if (EditorUtility.DisplayDialog("Clear Persistent Data Path", 
                "Are you sure you wish to clear the persistent data path?" +
                "\n This action cannot be reversed.", "Clear", "Cancel"))
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(Application.persistentDataPath);

            foreach (FileInfo file in di.GetFiles())
                file.Delete();
            foreach (DirectoryInfo dir in di.GetDirectories())
                dir.Delete(true);
        }
    }

    [MenuItem("OSK-Framework/Tools/Save/Clear PlayerPrefs", false, 200)]
    private static void ClearPlayerPrefs()
    {
        if (EditorUtility.DisplayDialog("Clear PlayerPrefs", 
                "Are you sure you wish to clear PlayerPrefs?" +
                "\nThis action cannot be reversed.", "Clear", "Cancel"))
            PlayerPrefs.DeleteAll();
    }
}

}
#endif