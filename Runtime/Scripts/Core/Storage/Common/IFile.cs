using Cysharp.Threading.Tasks;

namespace OSK
{
    public interface IFile
    {
        public void Save<T>(string fileName, T data, bool isEncrypt = false);
        public T Load<T>(string fileName, bool isDecrypt = false);
        public void Delete(string fileName);
        bool Exists(string fileName);
        void WriteAllLines(string fileName, string[] lines);
    }
    
}
