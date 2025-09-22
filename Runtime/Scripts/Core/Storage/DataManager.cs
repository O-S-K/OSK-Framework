namespace OSK
{
    public class DataManager : GameFrameworkComponent
    {
        public bool isEncrypt = false;
        private JsonSystem _json = new JsonSystem();
        private FileSystem _file = new FileSystem();
        private XMLSystem _xml = new XMLSystem();

        public override void OnInit()
        {
        }

        /// <summary>
        ///  Save data to file, T is the file system type, U is the data type
        ///  Example: Save<JsonSystem, PlayerData>("playerData.json", playerData);
        /// </summary> 
        public void Save<T, U>(string fileName, U data)
        {
            IFile fileSystem = GetFileSystem<T>();
            if (fileSystem != null)
            {
                fileSystem.Save(fileName, data, isEncrypt);
            }
        }

        /// <summary>
        ///  Write all text to file, T is the file system type
        ///  </summary>
        public void WriteAllText(string fileName,  string[] text)
        {
            FileSystem fileSystem = new FileSystem();
            fileSystem.WriteAllLines(fileName, text);
        }

        /// <summary>
        ///  Load data from file, T is the file system type, U is the data type
        ///  Example: Load<JsonSystem, PlayerData>("playerData.json");
        /// </summary>
        public U Load<T, U>(string fileName)
        {
            IFile fileSystem = GetFileSystem<T>();
            if (fileSystem != null)
            {
                return fileSystem.Load<U>(fileName, isEncrypt);
            }
            return default(U);
        }

        /// <summary>
        ///  Query data from file, T is the file system type, U is the data type
        /// </summary>
        public U Query<T, U>(string fileName, bool condition)
        {
            IFile fileSystem = GetFileSystem<T>();
            if (fileSystem != null)
            {
                return fileSystem.Query<U>(fileName, condition);
            }
            return default(U);
        }

        /// <summary>
        ///  Delete file, T is the file system type
        ///  Example: Delete<JsonSystem>("playerData.json");
        /// </summary>
        public void Delete<T>(string fileName)
        {
            IFile fileSystem = GetFileSystem<T>();
            if (fileSystem != null)
            {
                fileSystem.Delete(fileName);
            }
        }
          
        private IFile GetFileSystem<T>()
        {
            if (typeof(T) == typeof(JsonSystem))
                return _json;
            if (typeof(T) == typeof(FileSystem))
                return _file;
            if (typeof(T) == typeof(XMLSystem))
                return _xml;
            return null;
        }
    }
}