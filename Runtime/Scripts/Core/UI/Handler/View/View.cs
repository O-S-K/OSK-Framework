using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;

namespace OSK
{
    public abstract class View : MonoBehaviour
    {
        public event Action<object[]> OnDataChanged;

        [SerializeField]
        private object[] _data;

        public object[] Data
        {
            get => _data;
            private set
            {
                if (!Equals(_data, value))
                {
                    _data = value;
                    OnDataChanged?.Invoke(_data);
                }

#if UNITY_EDITOR
                if (_data != null && _data.Length > 0)
                {
                    string details = string.Join(", ", _data.Select(d =>
                        d == null ? "null" : $"{d.GetType().Name}({d})"));
                    MyLogger.Log($"[DebugData] {GetType().Name} received data: [{details}]");
                }
                else
                {
                    MyLogger.Log($"[DebugData] {GetType().Name} received empty data");
                }
#endif
            }
        }

        [Header("Settings")]
        [EnumToggleButtons]
        public EViewType viewType = EViewType.Popup;

        /// Depth is used to determine the order of views in the stack
        public int depthEdit;

        [ShowInInspector]
        private int _depth
        {
            get
            {
                int depthOffset = viewType switch
                {
                    EViewType.None => 0,
                    EViewType.Popup => 1000,
                    EViewType.Alert => 5000,
                    EViewType.Overlay => 10000,
                    EViewType.Screen => -1000,
                    _ => 0
                };
                return depthEdit + depthOffset;
            }
        }

        public int Depth => _depth;


        /// used for sorting views, higher value means higher priority in the stack
        [SerializeField]
        private int _priority;

        public int Priority => _priority;

        [Space]
        [ToggleLeft]
        public bool isAddToViewManager = true;

        [ToggleLeft]
        public bool isPreloadSpawn = true;

        [ToggleLeft]
        public bool isRemoveOnHide = false;

        /// <summary>
        /// isInitOnScene = true if spawn in scene 
        /// </summary>
        [ReadOnly]
        [ToggleLeft]
        public bool isInitOnScene; 


        [ShowInInspector, ReadOnly]
        [ToggleLeft]
        private bool _isShowing;

        [SerializeReference]
        public Action OnOpened;

        [SerializeReference]
        public Action OnClosed;

        public bool IsShowing => _isShowing;

        /// <summary>
        /// True khi view đang trong quá trình close transition (chưa SetActive false).
        /// Subclass dùng để skip Update logic trong thời gian này.
        /// </summary>
        public bool IsClosing { get; private set; }

        [ReadOnly, SerializeField]
        private UITransition _uiTransition;

        public UITransition UITransition => _uiTransition ??= GetComponent<UITransition>();

        private RootUI _rootUI;

        [Button]
        public void AddUITransition() => _uiTransition = gameObject.GetOrAdd<UITransition>();

        [Button]
        public void AddViewToDataSO()
        {
            var allSO = Resources.LoadAll<ListViewSO>("");
            var dataSO = allSO.FirstOrDefault();

            if (dataSO == null)
            {
                MyLogger.LogError("[AddViewToDataSO] Cannot find ViewDataSO in Resources folder.");
                return;
            }

            if (dataSO.Views.Any(v => v.view == this))
            {
                MyLogger.LogWarning($"[AddViewToDataSO] {name} already exists in ViewDataSO.");
                return;
            }

            var newData = new DataViewUI
            {
#if UNITY_EDITOR
                path = UnityEditor.AssetDatabase.GetAssetPath(this),
#endif
                viewType = viewType,
                isPreloadSpawn = isPreloadSpawn,
                isRemoveOnHide = isRemoveOnHide,
                depth = depthEdit,
                view = this
            };
            dataSO.Views.Add(newData);
            MyLogger.Log($"[AddViewToDataSO] Added {name} to ViewDataSO.");
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(dataSO);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        public void Initialize(RootUI rootUI)
        {
            if (isInitOnScene) return;

            isInitOnScene = true;
            _rootUI = rootUI;

            _uiTransition = GetComponent<UITransition>();
            _uiTransition?.Initialize();

            if (_rootUI == null)
            {
                MyLogger.LogError("[View] RootUI is still null after initialization.");
            }
            if (_rootUI != null) _rootUI.IsDirtySort = true;
        }

        protected virtual void Awake() => OnInit();
        protected abstract void OnInit(); // setting, get set component data ..... 

        public virtual void Open(object[] data = null)
        {
            if (!IsViewContainerInitialized()) return;
            if (IsAlreadyShowing())
            {
                SetData(data);
                return;
            }

            SetData(data);
            _isShowing = true;
            IsClosing = false;
            gameObject.SetActive(true);

            if (_rootUI.IsDirtySort)
            {
                SortHierarchyByDepth();
                _rootUI.IsDirtySort = false;
            }

            Opened();
        }

        private void Opened()
        {
            if (_uiTransition)
            {
                _uiTransition.OpenTrans(() =>
                {
                    OnOpened?.Invoke();
                    OnOpened = null;
                });
            }
            else
            {
                OnOpened?.Invoke();
                OnOpened = null;
            }
        }

        // example: SetData(new object[]{1,2,3,4,5});
        protected virtual void SetData(object[] data = null)
        {
            if (data == null || data.Length == 0)
            {
                //MyLogger.Log($"[SetData] No data passed to {GetType().Name}");
                return;
            }

            this.Data = data;
            MyLogger.Log($"[SetData] {GetType().Name} received data: [{string.Join(", ", data.Select(d => d == null ? "null" : $"{d.GetType().Name}({d})"))}]");
        }

        public void SetTweenUIOpen(TweenSettings settings)
        {
            _uiTransition.SetOpenSettings(settings);
        }

        public void SetTweenUIClose(TweenSettings settings)
        {
            _uiTransition.SetCloseSettings(settings);
        }


        public void SetDepth(EViewType viewType, int depth)
        {
            if (this.viewType != viewType)
            {
                this.viewType = viewType;
                var container = _rootUI.GetContainer(viewType);
                if (transform.parent != container)
                    transform.SetParent(container, false);
            }

            this.depthEdit = depth;
            _rootUI.IsDirtySort = true;
            SortHierarchyByDepth();
            _rootUI.IsDirtySort = false;
        }

        private void SortHierarchyByDepth()
        {
            var parent = transform.parent;
            if (parent == null) return;

            var siblings = new List<View>();
            for (int i = 0; i < parent.childCount; i++)
            {
                var v = parent.GetChild(i).GetComponent<View>();
                if (v != null) siblings.Add(v);
            }

            if (siblings.Count <= 1) return;

            siblings.Sort((a, b) => a.Depth.CompareTo(b.Depth));

            for (int i = 0; i < siblings.Count; i++)
            {
                if (siblings[i].transform.GetSiblingIndex() != i)
                    siblings[i].transform.SetSiblingIndex(i);
            }
        }

        public virtual void Hide()
        {
            if (!_isShowing) return;

            _isShowing = false;
            IsClosing = true;

            if (_uiTransition != null)
                _uiTransition.CloseTrans(FinalizeHide);
            else FinalizeHide();
        }

        public void CloseImmediately()
        {
            _isShowing = false;
            IsClosing = true;

            if (_uiTransition != null) _uiTransition.AnyClose(FinalizeImmediateClose);
            else FinalizeImmediateClose();
        }

        protected bool IsViewContainerInitialized()
        {
            if (_rootUI == null)
            {
                MyLogger.LogError(
                    "[View] View Manager is null. Ensure that the View has been initialized before calling Open.");
                return false;
            }

            return true;
        }

        protected bool IsAlreadyShowing()
        {
            if (!_isShowing) return false;
            MyLogger.LogWarning("[View] View is already showing");
            return true;
        }

        protected void FinalizeHide()
        {
            IsClosing = false;
            OnClosed?.Invoke();
            OnClosed = null;
            gameObject.SetActive(false);

            if (isRemoveOnHide)
                _rootUI.Delete(this);
        }

        protected void FinalizeImmediateClose()
        {
            IsClosing = false;
            gameObject.SetActive(false);
        }

        public virtual void Delete()
        {
            _rootUI.Delete(this);
        }
    }
}