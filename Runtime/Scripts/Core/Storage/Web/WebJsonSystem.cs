using System;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace OSK
{
    public class WebJsonSystem : IFile
    {
        public void Save<T>(string fileName, T data, bool encrypt = false)
        {
            string json = JsonConvert.SerializeObject(data);
            if (encrypt)
                json = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

            PlayerPrefs.SetString(fileName, json);
            PlayerPrefs.Save();
        }

        public T Load<T>(string fileName, bool decrypt = false)
        {
            if (!PlayerPrefs.HasKey(fileName))
                return default;

            string json = PlayerPrefs.GetString(fileName);

            if (decrypt)
            {
                var bytes = Convert.FromBase64String(json);
                json = Encoding.UTF8.GetString(bytes);
            }

            return JsonConvert.DeserializeObject<T>(json);
        }

        public void Delete(string fileName)
        {
            PlayerPrefs.DeleteKey(fileName);
        }

        public bool Exists(string fileName)
        {
            return PlayerPrefs.HasKey(fileName);
        }

        public void WriteAllLines(string fileName, string[] lines)
        {
            string json = JsonConvert.SerializeObject(lines);
            PlayerPrefs.SetString(fileName, json);
        }
    }

}
