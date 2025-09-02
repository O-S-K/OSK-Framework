#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine; 
using System.IO;

namespace OSK
{
public class SaveEditor : Editor
{
    [MenuItem("OSK-Framework/Tools/Save/Open Persistent Data Path" )]
    private static void OpenPersistentDataPath()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
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