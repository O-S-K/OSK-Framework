using System;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;

namespace OSK
{
    public class View : MonoBehaviour
    {
        public event Action<object[]> OnDataChanged;

        [SerializeField] private object[] _data;

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
                    MyLogger.Log( $"[DebugData] {GetType().Name} received data: [{details}]");
                }
                else
                {
                    MyLogger.Log( $"[DebugData] {GetType().Name} received empty data");
                }
#endif
            }
        }

        [Header("Settings")] [EnumToggleButtons]
        public EViewType viewType = EViewType.Popup;

        /// Depth is used to determine the order of views in the stack
        public int depthEdit;

        [ShowInInspector]
        private int _depth
        {
            get
            {
                int _depthOffset = viewType switch
                {
                    EViewType.None => 0,
                    EViewType.Popup => 1000,
                    EViewType.Overlay => 10000,
                    EViewType.Screen => -1000,
                    _ => 0
                };
                return depthEdit + _depthOffset;
            }
        }

        public int Depth => _depth;


        /// used for sorting views, higher value means higher priority in the stack
        [SerializeField] private int _priority;

        public int Priority => _priority;

        [Space] [ToggleLeft] public bool isAddToViewManager = true;
        [ToggleLeft] public bool isPreloadSpawn = true;
        [ToggleLeft] public bool isRemoveOnHide = false;
        [ReadOnly] [ToggleLeft] public bool isInitOnScene;


        [ShowInInspector, ReadOnly] [ToggleLeft]
        private bool _isShowing;

        [SerializeReference] public Action OnOpened;
        [SerializeReference] public Action OnClosed;

        public bool IsShowing => _isShowing;

        [ReadOnly, SerializeField] private UITransition _uiTransition;
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
                MyLogger.LogError( "[AddViewToDataSO] Cannot find ViewDataSO in Resources folder.");
                return;
            }

            if (dataSO.Views.Any(v => v.view == this))
            {
                MyLogger.LogWarning( $"[AddViewToDataSO] {name} already exists in ViewDataSO.");
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
            MyLogger.Log( $"[AddViewToDataSO] Added {name} to ViewDataSO.");
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(dataSO);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        public virtual void Initialize(RootUI rootUI)
        {
            if (isInitOnScene) return;

            isInitOnScene = true;
            _rootUI = rootUI;

            _uiTransition = GetComponent<UITransition>();
            _uiTransition?.Initialize();


            if (_rootUI == null)
            {
                MyLogger.LogError( "[View] RootUI is still null after initialization.");
            }

            SetDepth();
        }

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
            gameObject.SetActive(true);

            SetDepth();
            Opened();
        }

        protected async void Opened()
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
                MyLogger.Log( $"[SetData] No data passed to {GetType().Name}");
                return;
            }

            this.Data = data;
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
            this.viewType = viewType;
            this.depthEdit = depth;
            SetDepth();
        }

        private void SetDepth()
        {
            /*var canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = viewType switch
                {
                    EViewType.None => (0 + canvas.sortingOrder),
                    EViewType.Popup => (1000 + canvas.sortingOrder),
                    EViewType.Overlay => (10000 + canvas.sortingOrder),
                    EViewType.Screen => (-1000 + canvas.sortingOrder),
                    _ => canvas.sortingOrder
                };
            }
            else*/
            {
                var childPages = _rootUI.GetSortedChildPages(_rootUI.ViewContainer);
                if (childPages.Count == 0)
                    return;

                var insertIndex = _rootUI.FindInsertIndex(childPages, _depth);
                if (insertIndex <= 0)
                {
                    transform.SetAsFirstSibling();
                }
                else if (insertIndex >= childPages.Count)
                {
                    transform.SetAsLastSibling();
                }
                else
                {
                    transform.SetSiblingIndex(insertIndex);
                }
            }
        }

        public virtual void Hide()
        {
            if (!_isShowing) return;

            _isShowing = false;
            MyLogger.Log( $"[View] Hide {gameObject.name} is showing {_isShowing}");

            if (_uiTransition != null)
                _uiTransition.CloseTrans(FinalizeHide);
            else FinalizeHide();
        }

        public void CloseImmediately()
        {
            _isShowing = false;

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
            MyLogger.LogWarning( "[View] View is already showing");
            return true;
        }

        protected void FinalizeHide()
        {
            OnClosed?.Invoke();
            OnClosed = null;
            gameObject.SetActive(false);

            if (isRemoveOnHide)
                _rootUI.Delete(this);
        }

        protected void FinalizeImmediateClose()
        {
            gameObject.SetActive(false);
        }

        public virtual void Delete()
        {
            _rootUI.Delete(this);
        }
    }
}