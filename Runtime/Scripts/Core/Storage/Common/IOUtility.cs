using System.IO;
using UnityEngine;
using System.Collections.Generic;

/*
 *🧩 Storage Directory:
Enum	            Read	Write	        Data	        Build	        Note
PersistentData	    ✅	    ✅	            ✅	            ❌	            Khuyên dùng cho save game / config
StreamingAssets	    ✅	    ⚠️ (only PC)	✅	            ✅	            Dữ liệu build sẵn, read-only
DataPath	        ✅	    ⚠️	            ❌	            ✅	            Tool hoặc Editor-only
TemporaryCache	    ✅	    ✅	            ❌	            ❌	            File tạm, cache, có thể bị xóa
Custom	            ✅	    ✅	            option	        ❌	            Chỉ định đường dẫn thủ công
 */

namespace OSK
{
    public static class IOUtility
    {
        public enum StorageDirectory
        {
            PersistentData, 
            StreamingAssets,
            DataPath,       
            TemporaryCache, 
            Custom          
        }
        [Tooltip("Select the directory where files will be saved.")]
        public static StorageDirectory directorySave = StorageDirectory.PersistentData;
        public static string customPath = "";
        
        public static string encryptKey = "b14ca5898a4e4133bbce2ea2315a1916";
        
        public static string CreateFilePath(string fileName)
        {
            string dir = GetDirectory();
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            return Path.Combine(dir, fileName);
        }
        
        public static string CreateDirectory (string folderName)
        {
            string dir = GetDirectory();
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string fullPath = Path.Combine(dir, folderName);
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);

            return fullPath;
        }

        public static string GetDirectory()
        {
           return GetDirectoryPath(directorySave, customPath);
        }
        
        public static string GetDirectoryPath(StorageDirectory dir, string customDir = "")
        {
            switch (dir)
            {
                case StorageDirectory.PersistentData: return Application.persistentDataPath;
                case StorageDirectory.StreamingAssets: return Application.streamingAssetsPath;
                case StorageDirectory.DataPath: return Application.dataPath;
                case StorageDirectory.TemporaryCache: return Application.temporaryCachePath;
                case StorageDirectory.Custom: return string.IsNullOrEmpty(customDir) ? Application.persistentDataPath : customDir;
                default: return Application.persistentDataPath;
            }
        }
         

        public static string GetPath(string fileName)
        {
            string dir = GetDirectory();
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            return Path.Combine(dir, fileName);
        }

        public static string FilePath(string fileName)
        {
            return GetPath(fileName);
        }

        public static void DeleteFile(string fileName)
        {
            string path = GetPath(fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
                MyLogger.Log($"🗑 Deleted file: {path}");
            }
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

            MyLogger.LogWarning("Asset not found in resources");
            return fullPath;
        }
#endif
    }
}