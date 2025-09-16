using System;
using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    public class DataManager : GameFrameworkComponent
    {  
        [SerializeReference, SerializeField]
        private Dictionary<Type, List<IData>> k_DataStore = new Dictionary<Type, List<IData>>();

        public override void OnInit() {}

        // add data to the data store
        public void Add<T>(T data) where T : class, IData
        {
            Type type = typeof(T);
            if (!k_DataStore.ContainsKey(type))
            {
                Logg.Log("Data",$"Creating data store for type {type}.");
                k_DataStore[type] = new List<IData>();
            }

            List<IData> list = k_DataStore[type];

            if (!list.Contains(data))
            {
                Logg.Log("Data",$"Adding data of type {type}.");
                list.Add(data);
            }
            else
            {
                Logg.LogWarning("Data",$"Data of type {type} already exists.");
            }
        }

        // add data to the data store
        public void Add<T>(List<T> dataList) where T : class, IData
        {
            Type type = typeof(T);
            if (!k_DataStore.ContainsKey(type))
            {
                k_DataStore[type] = new List<IData>();
            }

            Logg.Log("Data",$"Adding list of type {type}.");
            k_DataStore[type].AddRange(dataList);
        }

        // get all data of type T
        public List<T> GetAll<T>() where T : class, IData
        {
            Type type = typeof(T);
            if (k_DataStore.TryGetValue(type, out var value))
            {
                Logg.Log("Data",$"Data of type {type} found.");
                return value.ConvertAll(x => x as T);
            }

            Logg.LogWarning("Data",$"No data found of type {type}.");
            return new List<T>();
        }

        // get first data of type T
        public T Get<T>() where T : class, IData
        {
            Type type = typeof(T);
            if (k_DataStore.TryGetValue(type, out var value) && value.Count > 0)
            {
                Logg.Log("Data",$"Data of type {type} found.");
                return value[0] as T;
            }

            Logg.LogWarning("Data",$"No data found of type {type}.");
            return null;
        }

        // find data of type T
        public T Query<T>(Predicate<T> query) where T : class, IData
        {
            Type type = typeof(T);
            if (k_DataStore.TryGetValue(type, out var value))
            {
                Logg.Log("Data",$"Querying data of type {type}.");
                return value.ConvertAll(x => x as T).Find(query);
            }

            Logg.LogWarning("Data",$"No data found of type {type}.");
            return null;
        }

        // take all data of type T
        public List<T> QueriesAll<T>(Predicate<T> query) where T : class, IData
        {
            Type type = typeof(T);
            if (k_DataStore.TryGetValue(type, out var value))
            {
                Logg.Log("Data",$"Querying all data of type {type}.");
                return value.ConvertAll(x => x as T).FindAll(query);
            }

            Logg.LogWarning("Data",$"No data found of type {type}.");
            return new List<T>();
        }
        
        // save data of type T
        // use Main.Storage.Save<T, U>(T data, string key) to save data
        public void Save<T>(T data, string nameData, Action onSuccess = null) where T : class, IData
        {
            Type type = typeof(T);
            if (k_DataStore.TryGetValue(type, out var value))
            {
                Logg.Log("Data",$"Saving data of type {type}.");
                int index = value.IndexOf(data);
                if (index >= 0)
                {
                    value[index] = data;
                    Main.Storage.Save<JsonSystem, T>(nameData, data);
                    onSuccess?.Invoke();
                }
                else
                {
                    Logg.LogWarning("Data",$"Data of type {type} not found in store.");
                }
            }
            else
            {
                Logg.LogWarning("Data",$"No data store found for type {type}.");
            }
        }
        
        // Get saved data of type T
        // use Main.Storage.Get<T, U>(string key) to get data
        public T GetSaved<T>(string nameData) where T : class, IData
        {
            Type type = typeof(T);
            if (k_DataStore.TryGetValue(type, out var value))
            {
                Logg.Log("Data",$"Getting saved data of type {type}.");
                T data = Main.Storage.Load<JsonSystem, T>(nameData);
                if (data != null)
                {
                    if (!value.Contains(data))
                    {
                        value.Add(data);
                    }
                    return data;
                }
                else
                {
                    Logg.LogWarning("Data",$"No saved data found for type {type} with name {nameData}.");
                }
            }
            else
            {
                Logg.LogWarning("Data",$"No data store found for type {type}.");
            }
            return null;
        }
        
        public void Remove<T>(T data) where T : class, IData
        {
            Type type = typeof(T);
            if (k_DataStore.TryGetValue(type, out var value))
            {
                Logg.Log("Data",$"Removing data of type {type}.");
                value.Remove(data);
            }
        }

        // remove data of type T
        public void Remove<T>(Predicate<T> query) where T : class, IData
        {
            Type type = typeof(T);
            if (k_DataStore.TryGetValue(type, out var value))
            {
                Logg.Log("Data",$"Removing data of type {type}.");
                value.RemoveAll(x => query((T)x));
            }
        }
    }
}
