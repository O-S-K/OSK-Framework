using System;
using System.Collections.Generic;

namespace OSK
{
    [Serializable]
    public class SubUpdateContainer
    {
        private readonly UpdateContainer<ISubUpdate> subUpdates = new();
        private readonly UpdateContainer<ISubFixedUpdate> subFixedUpdates = new();
        private readonly UpdateContainer<ISubLateUpdate> subLateUpdates = new();
        
        public IReadOnlyList<ISubUpdate> GetUpdates() => subUpdates.GetUpdates();
        public IReadOnlyList<ISubFixedUpdate> GetFixedUpdates() => subFixedUpdates.GetUpdates();
        public IReadOnlyList<ISubLateUpdate> GetLateUpdates() => subLateUpdates.GetUpdates();
        
        public void Register(object obj)
        {
            if (obj is ISubUpdate update) subUpdates.Add(update);
            else if (obj is ISubFixedUpdate fixedUpdate) subFixedUpdates.Add(fixedUpdate);
            else if (obj is ISubLateUpdate lateUpdate) subLateUpdates.Add(lateUpdate);
            else
            {
                Logg.LogError("SubUpdateContainer Register Error");
            }
        }

        public void Unregister(object obj)
        {
            if (obj is ISubUpdate update) subUpdates.Remove(update);
            else if (obj is ISubFixedUpdate fixedUpdate) subFixedUpdates.Remove(fixedUpdate);
            else if (obj is ISubLateUpdate lateUpdate) subLateUpdates.Remove(lateUpdate);
            else
            {
                Logg.LogError("SubUpdateContainer Unregister Error");
            }
        }

        public void Tick() => subUpdates.Execute(x => x.Tick());
        public void FixedTick() => subFixedUpdates.Execute(x => x.FixedTick());
        public void LateTick() => subLateUpdates.Execute(x => x.LateTick());
        
        public void DebugDisplay(string nameContainer)
        {
            Logg.Log("MonoTick",$"\n<color=cyan>📦 {nameContainer} - Debug Display</color>");

            int updateCount = subUpdates.GetUpdates().Count;
            int fixedUpdateCount = subFixedUpdates.GetUpdates().Count;
            int lateUpdateCount = subLateUpdates.GetUpdates().Count;

            Logg.Log("MonoTick",$"🔄 Total Updates: <color=yellow>{updateCount}</color> | ⚙️ FixedUpdates: <color=yellow>{fixedUpdateCount}</color> | ⏳ LateUpdates: <color=yellow>{lateUpdateCount}</color>");

            DisplayUpdateList("🔄 SubUpdates", subUpdates.GetUpdates());
            DisplayUpdateList("⚙️ SubFixedUpdates", subFixedUpdates.GetUpdates());
            DisplayUpdateList("⏳ SubLateUpdates", subLateUpdates.GetUpdates());
        }

        private void DisplayUpdateList<T>(string title, IReadOnlyList<T> updates)
        {
            if (updates.Count == 0)
            {
                Logg.Log("MonoTick",$"<color=gray>{title}: Empty</color>");
                return;
            }

            Logg.Log("MonoTick",$"<color=magenta>{title} ({updates.Count}):</color>");
            for (int i = 0; i < updates.Count; i++)
            {
                Logg.Log("MonoTick",$"  - <color=green>{updates[i].GetType().Name}</color>");
            }
        } 
    }
}
