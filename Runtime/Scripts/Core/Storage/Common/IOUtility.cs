using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OSK
{
    public static class IOUtility
    {
        public static string encryptKey = "b14ca5898a4e4133bbce2ea2315a1916";

        public static bool IsFileExists(string path)
        {
            return File.Exists(path);
        }

        public static void DeleteFile(string fileName)
        {
            string path = IOUtility.GetPath(fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
                OSKLogger.Log("Storage",$"[Delete File Success]: {fileName}");
            }
            else
            {
                OSKLogger.LogError("Storage",$"[Delete File Error]: {fileName} NOT found");
            }
        }

        public static bool IsDirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public static bool CreateDirectory(string path)
        {
            if (Directory.Exists(path)) return false;
            Directory.CreateDirectory(path);
            return true;

        }

        public static bool DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path);
                return true;
            }

            return false;
        }


        public static string GetPath(string fileName)
        {
            string directory = Application.persistentDataPath;

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // Sanitize file name
            fileName = Path.GetFileName(fileName); // Xóa hết thư mục gắn kèm (nếu có)
            return Path.Combine(directory, fileName);
        }

        public static string FilePath(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("[FilePath] Invalid file name!");
                return null;
            }

            name = Path.GetFileName(name); // Remove invalid path parts
            return Path.Combine(Application.persistentDataPath, name);
        }

        public static List<string> GetAll(string fileName)
        {
            List<string> allFiles = new List<string>();
            var path = IOUtility.GetPath(fileName);

            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path);
                foreach (var file in files)
                {
                    allFiles.Add(Path.GetFileName(file));
                }
            }

            return allFiles;
        }

#if UNITY_EDITOR
        public static string GetPathAfterResources(Object asset)
        {
            string fullPath = UnityEditor.AssetDatabase.GetAssetPath(asset);
            int resourcesIndex = fullPath.IndexOf("Resources/");
            if (resourcesIndex >= 0)
            {
                return fullPath.Substring(resourcesIndex + "Resources/".Length);
            }

            OSKLogger.LogWarning("Asset not found in resources");
            return fullPath;
        }
#endif
    }
}