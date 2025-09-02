namespace OSK
{
    // Data class for scene management
    public class DataScene
    {
        // Scene name to load
        public string sceneName;
        // Load mode (Single or Additive)
        public ELoadMode loadMode;
        // Whether to automatically remove the scene when unloading
        public bool autoRemove = true;
    }
}