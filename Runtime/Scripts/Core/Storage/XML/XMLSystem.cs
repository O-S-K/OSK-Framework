using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;

namespace OSK
{
    public class XMLSystem : IFile
    {
        public void Save<T>(string fileName, T data, bool isEncrypt = false)
        {
            string path = IOUtility.GetPath(fileName + ".xml");

            try
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(T));
                    serializer.Serialize(memoryStream, data);
                    byte[] xmlBytes = memoryStream.ToArray();

                    if (isEncrypt)
                    {
                        xmlBytes = FileSecurity.Encrypt(xmlBytes, IOUtility.encryptKey);
                    }

                    File.WriteAllBytes(path, xmlBytes);
                }

                RefreshEditor();
                Logg.Log("Storage",$"[Save File Success]: {fileName}.xml \n {path} ");
            }
            catch (Exception ex)
            {
                Logg.LogError("Storage",$"[Save File Exception]: {fileName}.xml {ex.Message}");
            }
        }

        public T Load<T>(string fileName, bool isDecrypt = false)
        {
            string path = IOUtility.GetPath(fileName + ".xml");
            if (!File.Exists(path))
            {
                Logg.LogError("Storage",$"[Load File Error]: {fileName}.xml NOT found at {path}");
                return default;
            }

            try
            {
                byte[] fileBytes = File.ReadAllBytes(path);
                if (isDecrypt)
                {
                    fileBytes = FileSecurity.Decrypt(fileBytes, IOUtility.encryptKey);
                }

                using (MemoryStream memoryStream = new MemoryStream(fileBytes))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(T));
                    Logg.Log("Storage",$"[Load File Success]: {fileName}.xml \n {path}");
                    return (T)serializer.Deserialize(memoryStream);
                }
            }
            catch (Exception ex)
            {
                Logg.LogError("Storage",$"[Load File Exception]: {fileName}.xml {ex.Message}");
                return default;
            }
        }

        public void Delete(string fileName)
        {
            IOUtility.DeleteFile(fileName + ".xml");
        }

        public T Query<T>(string fileName, bool condition)
        {
            return condition ? Load<T>(fileName) : default;
        }

        private void RefreshEditor()
        {
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
    }
}
