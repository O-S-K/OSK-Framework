using System;
using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    public class BlackboardManager : GameFrameworkComponent 
    {
        public Dictionary<string, Blackboard> blackboards = new Dictionary<string, Blackboard>();
        public Dictionary<string, BlackboardData> blackboardData = new Dictionary<string, BlackboardData>();

        public override void OnInit()
        {
        }

        public Blackboard Create(string name, BlackboardData data, GameObject pingObject)
        {
            if (blackboards.TryGetValue(name, out var existingBlackboard))
            {
                return existingBlackboard;
            }

            var blackboard = new Blackboard();
            blackboard.SetData(data, pingObject);

            blackboardData[name] = data;
            blackboards[name] = blackboard;
            return blackboard;
        }

        public void Remove(string name)
        {
            if (blackboards.TryGetValue(name, out var blackboard))
            {
                blackboards.Remove(name);
                blackboardData.Remove(name);
            }
        }

        public bool Has(string name)
        {
            return blackboards.ContainsKey(name);
        }

        public Blackboard Get(string name)
        {
            return blackboards.TryGetValue(name, out var blackboard) ? blackboard : null;
        }

        public void ClearAll()
        {
            blackboards.Clear();
            blackboardData.Clear();
        }

        public BlackboardData GetData(string name)
        {
            return blackboardData.GetValueOrDefault(name);
        }

        public void SetData(string name, BlackboardData data, GameObject pingObject)
        {
            if (blackboards.TryGetValue(name, out var blackboard))
            {
                blackboard.SetData(data, pingObject);
                blackboardData[name] = data;
            }
        }

        public IReadOnlyDictionary<string, Blackboard> GetAll()
        {
            return blackboards;
        }

        public void SaveAll()
        {
            foreach (var kvp in blackboards)
            {
                var blackboard = kvp.Value;
                if (blackboard != null)
                {
                    foreach (var blackboardValue in blackboard.GetAllValues())
                    {
                        Main.Data.Save(SaveType.Json, blackboardValue.Key, blackboardValue.Value);
                    }
                }
            }
        }

        public void LoadAll()
        {
            foreach (var kvp in blackboards)
            {
                var blackboard = kvp.Value;
                if (blackboard != null)
                {
                    Main.Data.Load<Dictionary<string, Blackboard>>(SaveType.Json, kvp.Key);
                }
            }
        }
    }
}