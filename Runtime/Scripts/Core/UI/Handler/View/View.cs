using System;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;

namespace OSK
{
    public class View : MonoBehaviour
    {
         private object[] _data;
         public object[] Data
         {
             get => _data;
             set
             {
                 _data = value;
#if UNITY_EDITOR
                 string details = string.Join(", ", _data.Select(d =>
                     d == null ? "null" : $"{d.GetType().Name}({d})"));
                 Logg.Log($"[DebugData] {GetType().Name} received data: [{details}]");
#endif
             }
         }
        
        [Header("Settings")] [EnumToggleButtons]
        public EViewType viewType = EViewType.Popup;

        /// Depth is used to determine the order of views in the stack
        public int depth;

        private int _depth;

        public int Priority => _priority;

        [SerializeField]
        private int _priority; // used for sorting views, higher value means higher priority in the stack
 
        [Space] [ToggleLeft] public bool isAddToViewManager = true;
        [ToggleLeft] public bool isPreloadSpawn = true;
        [ToggleLeft] public bool isRemoveOnHide = false; 
        [ReadOnly] [ToggleLeft] public bool isInitOnScene;
        
        
        [ShowInInspector, ReadOnly] [ToggleLeft]
        private bool _isShowing;
        
        [SerializeReference] public Action OnOpened;
        [SerializeReference] public Action OnClosed;

        public bool IsShowing => _isShowing;


        [ReadOnly, SerializeField] 
        private UITransition _uiTransition;
        [ReadOnly, SerializeField] 
        public UITransition UITransition => _uiTransition ??= GetComponent<UITransition>(); 
        
        private RootUI _rootUI;

        [Button]
        public void AddUITransition()
        {
            _uiTransition = gameObject.GetOrAdd<UITransition>();
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
                Logg.LogError("[View] RootUI is still null after initialization.");
            }

            _depth = depth;
            SetDepth(depth);
        }

        public void SetDepth(EViewType viewType, int depth)
        {
            this.viewType = viewType;
            SetDepth(depth);
        }

        private void SetDepth(int depth)
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

                var insertIndex = _rootUI.FindInsertIndex(childPages, depth);
                transform.SetSiblingIndex(insertIndex == childPages.Count ? transform.GetSiblingIndex() : insertIndex);
            }
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

            if (_depth != depth)
                SetDepth(depth);

            Opened();
        }

        protected void Opened()
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
                Logg.Log($"[SetData] No data passed to {GetType().Name}");
                return;
            }

            this._data = data;
        } 

        public virtual void Hide()
        {
            if (!_isShowing) return;

            _isShowing = false;
            Logg.Log($"[View] Hide {gameObject.name} is showing {_isShowing}");

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
                Logg.LogError(
                    "[View] View Manager is null. Ensure that the View has been initialized before calling Open.");
                return false;
            }

            return true;
        }

        protected bool IsAlreadyShowing()
        {
            if (!_isShowing) return false;
            Logg.LogWarning("[View] View is already showing");
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