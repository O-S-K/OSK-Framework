using System;

namespace OSK
{
    [Flags]
    public enum ModuleType
    {
        None = 0,
        MonoManager = 1 << 0,
        ObserverManager = 1 << 2,
        EventBusManager = 1 << 3,
        PoolManager = 1 << 4,
        CommandManager = 1 << 5,
        DirectorManager = 1 << 6,
        ResourceManager = 1 << 7,
        StorageManager = 1 << 8,
        DataManager = 1 << 9,
        WebRequestManager = 1 << 10,
        GameConfigsManager = 1 << 11,
        UIManager = 1 << 12,
        SoundManager = 1 << 13,
        LocalizationManager = 1 << 14,
        EntityManager = 1 << 15,
        BlackboardManager = 1 << 16,
        ProcedureManager = 1 << 17,
        GameInit = 1 << 18
    }
}