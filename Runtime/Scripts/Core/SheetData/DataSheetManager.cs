using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OSK
{
    public class DataSheetManager : GameFrameworkComponent
    {
        [SerializeField, ReadOnly]
        private List<BaseSheet> sheets = new List<BaseSheet>();

        private Dictionary<Type, object> _sheetMap = new Dictionary<Type, object>();
        private Dictionary<string, BaseSheet> _nameToSheetMap = new Dictionary<string, BaseSheet>();
        private Dictionary<Type, BaseSheet> _dataTypeToSheet = new Dictionary<Type, BaseSheet>();

        public override void OnInit()
        {
            _sheetMap.Clear();
            _nameToSheetMap.Clear();

            var loadedSheets = Resources.LoadAll<BaseSheet>("DataSheets");

            foreach (var sheet in loadedSheets)
            {
                if (sheet == null) continue;
                _sheetMap[sheet.GetType()] = sheet;
                _nameToSheetMap[sheet.name] = sheet;
                _dataTypeToSheet[sheet.GetDataType()] = sheet;
                sheet.Initialize();
            }

            sheets = loadedSheets.ToList();
        }


        public T GetSheet<T>() where T : class => _sheetMap.TryGetValue(typeof(T), out object s) ? s as T : null;

        public T GetByNameSO<T>(string nameSO) where T : BaseSheet
        {
            if (_nameToSheetMap.TryGetValue(nameSO, out var sheet))
            {
                return sheet as T;
            }

            return null;
        }

        public TData GetData<TData>() where TData : BaseData
        {
            if (_dataTypeToSheet.TryGetValue(typeof(TData), out var sheet))
            {
                var container = sheet as BaseSheetContainer<TData>;
                if (container != null && container.dataList.Count > 0)
                {
                    return container.dataList[0];
                }
            }

            Debug.LogError($"[DataSheetManager] Không tìm thấy Sheet nào chứa kiểu dữ liệu: {typeof(TData).Name}");
            return null;
        }

        // Get data by ID from specific sheet type
        public TData GetDataByID<TSheet, TData>(int id = -1) where TSheet : BaseSheetContainer<TData> where TData : BaseData
        {
            var sheet = GetSheet<TSheet>();
            if (sheet == null)
            {
                MyLogger.LogError($"[DataSheetManager] Not found sheet of type {typeof(TSheet).Name}");
                return null;
            }

            return sheet.GetById(id);
        }

        public List<TData> GetAllData<TSheet, TData>() where TSheet : BaseSheetContainer<TData> where TData : BaseData
        {
            var sheet = GetSheet<TSheet>();
            if (sheet == null)
            {
                MyLogger.LogError($"[DataSheetManager] Not found sheet of type {typeof(TSheet).Name}");
                return null;
            }

            return sheet.GetAllData();
        }


        public void AddSheet(BaseSheet sheet)
        {
            if (sheet == null) return;

            _sheetMap[sheet.GetType()] = sheet;
            _nameToSheetMap[sheet.name] = sheet;
            _dataTypeToSheet[sheet.GetDataType()] = sheet;

            sheet.Initialize();
            if (!sheets.Contains(sheet)) sheets.Add(sheet);
        }

        public void RemoveSheet<T>() where T : BaseSheet
        {
            var type = typeof(T);
    
            if (_sheetMap.TryGetValue(type, out object sheetObj))
            {
                var sheet = sheetObj as BaseSheet;
                if (sheet != null)
                {
                    sheets.Remove(sheet);

                    _sheetMap.Remove(type);
                    _nameToSheetMap.Remove(sheet.name);
                    _dataTypeToSheet.Remove(sheet.GetDataType());
            
                    MyLogger.Log($"[DataSheetManager] Removed sheet: {sheet.name}");
                }
            }
        }

        public void ClearSheets()
        {
            sheets.Clear();
            _sheetMap.Clear();
            _nameToSheetMap.Clear();
            _dataTypeToSheet.Clear();
            Debug.Log("[DataSheetManager] All sheets cleared.");
        }

        [Button]
        private void RefreshSheetsInEditor()
        {
#if UNITY_EDITOR
            sheets = UnityEditor.AssetDatabase.FindAssets("t:BaseSheet")
                .Select(guid => UnityEditor.AssetDatabase.LoadAssetAtPath<BaseSheet>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid)))
                .ToList();
#endif
        }
        
        [Button(ButtonSizes.Medium), GUIColor(0, 1, 0)]
        private void ValidateAllSheets()
        {
            foreach (var sheet in sheets)
            {
                sheet.Initialize(); 
                MyLogger.Log($"Sheet {sheet.name} is OK!");
            }
        }

        [Button]
        private void OpenEditorWindow()
        {
#if UNITY_EDITOR
            DataSheetEditorWindow.OpenWindow();
#endif
        }
    }
}