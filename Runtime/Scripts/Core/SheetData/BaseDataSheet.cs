using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OSK
{
    public interface IDataEntity { int ID { get; } }

    [Serializable]
    public abstract class BaseData : IDataEntity
    {
        [TableColumnWidth(50, Resizable = false)]
        public int id;
        public int ID => id;
    }

    public abstract class BaseSheet : ScriptableObject
    {
        public abstract Type GetDataType();
        public abstract void Initialize();
    }

    public abstract class BaseSheetContainer<T> : BaseSheet where T : BaseData
    {
        [TableList(AlwaysExpanded = true, NumberOfItemsPerPage = 25)]
        [Searchable] // Cho phép tìm kiếm nhanh như Excel
        public List<T> dataList = new List<T>();
        
        protected Dictionary<int, T> _cache = new Dictionary<int, T>();

        public override Type GetDataType() => typeof(T);

        

        public override void Initialize()
        {
            _cache.Clear();
            foreach (var item in dataList)
            {
                if (item != null && !_cache.ContainsKey(item.id))
                    _cache.Add(item.id, item);
            }
        }

        public T GetById(int id = -1)
        {
            if (_cache.Count == 0 && dataList.Count > 0) Initialize();
    
            if (id < 0 && dataList.Count > 0) return dataList[0];
            return _cache.TryGetValue(id, out T value) ? value : null;
        }
         
        public List<T> GetAllData()
        {
            if (_cache.Count == 0 && dataList.Count > 0) Initialize();
            return dataList;
        }
        
    }
}