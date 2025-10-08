using UnityEngine;

namespace OSK
{
    public class DataManager : GameFrameworkComponent
    {
        [Header("Global Settings")]
        public bool isEncrypt = false;

        private readonly JsonSystem _json = new JsonSystem();
        private readonly FileSystem _file = new FileSystem();
        private readonly XMLSystem _xml = new XMLSystem();
        
        /// Example: Save<JsonSystem, PlayerData>("playerData.json", playerData);
        /// Example: PlayerData playerData = Load<JsonSystem, PlayerData>("playerData.json");
        /// Example: Query<JsonSystem, PlayerData>("playerData.json", File.Exists("playerData.json"));
        /// Example: Delete<JsonSystem>("playerData.json");

        public override void OnInit() { }

        /// <summary>
        /// Save data to file (T = file system type, U = data type)
        /// </summary>
        public void Save<T, U>(string fileName, U data)
        {
            IFile fileSystem = GetFileSystem<T>();
            fileSystem?.Save(fileName, data, isEncrypt);
        }

        /// <summary>
        /// Load data from file
        /// </summary>
        public U Load<T, U>(string fileName)
        {
            IFile fileSystem = GetFileSystem<T>();
            return fileSystem != null ? fileSystem.Load<U>(fileName, isEncrypt) : default(U);
        }

        /// <summary>
        /// Query data (conditional load)
        /// </summary>
        public U Query<T, U>(string fileName, bool condition)
        {
            return condition ? Load<T, U>(fileName) : default;
        }

        /// <summary>
        /// Delete file
        /// </summary>
        public void Delete<T>(string fileName)
        {
            IFile fileSystem = GetFileSystem<T>();
            fileSystem?.Delete(fileName);
        }

        /// <summary>
        /// Write plain text file (.txt)
        /// </summary>
        public void WriteAllText(string fileName, string[] lines)
        {
            _file.WriteAllLines(fileName, lines);
        }

        private IFile GetFileSystem<T>() =>
            typeof(T) switch
            {
                var t when t == typeof(JsonSystem) => _json,
                var t when t == typeof(FileSystem) => _file,
                var t when t == typeof(XMLSystem) => _xml,
                _ => null
            };
    }
}