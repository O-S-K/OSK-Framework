using System;

namespace OSK
{
    [Flags]
    public enum ModuleType
    {
        None = 0,
        MonoManager = 1 << 0,
        ObserverManager = 1 << 1,
        EventBusManager = 1 << 2,
        PoolManager = 1 << 3,
        CommandManager = 1 << 4,
        DirectorManager = 1 << 5,
        ResourceManager = 1 << 6,
        DataManager = 1 << 7,
        WebRequestManager = 1 << 8,
        GameConfigsManager = 1 << 9,
        UIManager = 1 << 10,
        SoundManager = 1 << 11,
        LocalizationManager = 1 << 12,
        EntityManager = 1 << 13,
        BlackboardManager = 1 << 14,
        ProcedureManager = 1 << 15,
        GameInit = 1 << 16
    }
}