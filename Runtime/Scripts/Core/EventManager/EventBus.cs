using System;

namespace OSK
{
    public abstract class GameEvent
    {
        public DateTime TimeStamp { get; } = DateTime.UtcNow;
    } 
    
    public class AssetLoadedEvent<T> : GameEvent
    {
        public string AssetKey { get; }
        public T Asset { get; }

        public AssetLoadedEvent(string key, T asset)
        {
            AssetKey = key;
            Asset = asset;
        }
    }
}