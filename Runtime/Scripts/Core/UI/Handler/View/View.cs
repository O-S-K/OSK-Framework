using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace OSK
{
    public class View : MonoBehaviour
    {
        [Header("Datas")] [ShowInInspector, ReadOnly]
        private object[] data;

        public object[] Data
        {
            get => data;
            set
            {
                data = value;
                string details = string.Join(", ", data.Select(d =>
                    d == null ? "null" : $"{d.GetType().Name}({d})"));
                Logg.Log($"[DebugData] {GetType().Name} received data: [{details}]");
            }
        }


        [Header("Settings")] [EnumToggleButtons]
        public EViewType viewType = EViewType.Popup;

        /// Depth is used to determine the order of views in the stack
        public int depth;
        private int _depth;

        public int Priority => _priority;
        [SerializeField] private int _priority; // used for sorting views, higher value means higher priority in the stack


        [Space] [ToggleLeft] public bool isAddToViewManager = true;
        [ToggleLeft] public bool isPreloadSpawn = true;
        [ToggleLeft] public bool isRemoveOnHide = false;

        [ReadOnly] [ToggleLeft] public bool isInitOnScene;
        public bool IsShowing => _isShowing;

        [ShowInInspector, ReadOnly] [ToggleLeft]
        private bool _isShowing;

        private UITransition _uiTransition;
        public UITransition UITransition 
        {
            get
            {
                if (_uiTransition == null)
                    _uiTransition = GetComponent<UITransition>();
                return _uiTransition;
            }
        }
        private RootUI _rootUI;

        [Space] [ToggleLeft] public bool isShowEvent = false;
        [ShowIf(nameof(isShowEvent))] public UnityEvent EventAfterInit;
        [ShowIf(nameof(isShowEvent))] public UnityEvent EventBeforeOpened;
        [ShowIf(nameof(isShowEvent))] public UnityEvent EventAfterOpened;
        [ShowIf(nameof(isShowEvent))] public UnityEvent EventBeforeClosed;
        [ShowIf(nameof(isShowEvent))] public UnityEvent EventAfterClosed;

        [ReadOnly, SerializeField]
        protected IReferenceHolder referenceHolder;

        public IReferenceHolder ReferenceHolder
        {
            get
            {
                if (referenceHolder == null)
                    referenceHolder = GetComponent<IReferenceHolder>();
                return referenceHolder;
            }
        }

        [Button]
        public void AddUITransition()
        {
            _uiTransition = gameObject.GetOrAdd<UITransition>();
        }

        [Button]
        public void AddBindRef()
        {
            if (GetComponent<IReferenceHolder>() != null)
            {
                Debug.LogWarning("ReferenceHolder already exists.");
                return;
            }

            gameObject.AddComponent<MonoReferenceHolder>();
            Debug.Log("ReferenceHolder added.");
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
            EventAfterInit?.Invoke();
        }

        public void IterateRefs(System.Action<string, object> func) => ReferenceHolder.Foreach(func);

        public T GetRef<T>(string name, bool isTry = false) where T : Object
        {
            return ReferenceHolder.GetRef<T>(name, isTry);
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
                if (insertIndex == childPages.Count) transform.SetAsLastSibling();
                else transform.SetSiblingIndex(insertIndex);
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
            EventBeforeOpened?.Invoke();
            gameObject.SetActive(true);

            if (_depth != depth)
                SetDepth(depth);

            if (_uiTransition != null)
                _uiTransition.OpenTrans(() => EventAfterOpened?.Invoke());
            else EventAfterOpened?.Invoke();
        }

        // example: SetData(new object[]{1,2,3,4,5});
        protected virtual void SetData(object[] data = null)
        {
            if (data == null || data.Length == 0)
            {
                Logg.Log($"[SetData] No data passed to {GetType().Name}");
                return;
            }

            this.data = data;

#if UNITY_EDITOR
            // log data in editor
            var sb = new System.Text.StringBuilder();
            string htmlColorHead = ColorUtils.LimeGreen.ToHex();
            string htmlColorChill = ColorUtils.Chartreuse.ToHex();

            sb.AppendLine(
                $"<color={htmlColorHead}>[SetData] [{GetType().Name}] received {data.Length} parameters, click to expand:</color>");
            for (int i = 0; i < data.Length; i++)
            {
                object param = data[i];
                if (param == null)
                {
                    sb.AppendLine($"<color={htmlColorChill}>  - [{i}] (null): null</color>");
                    continue;
                }

                System.Type type = param.GetType();
                string typeName = type.Name;

                if (param is IEnumerable enumerable && type != typeof(string))
                {
                    sb.AppendLine($"<color={htmlColorChill}>  - ({typeName}):</color>");
                    int index = 0;
                    foreach (var item in enumerable)
                    {
                        string itemStr = item?.ToString() ?? "null";
                        sb.AppendLine($"<color={htmlColorChill}>       • [{index++}] {itemStr}</color>");
                    }
                }
                else
                {
                    string valueStr = param.ToString();
                    sb.AppendLine($"<color={htmlColorChill}>  - [{i}] ({typeName}): {valueStr}</color>");
                }
            }

            Debug.Log(sb.ToString());
#endif
        }

        public virtual void Hide()
        {
            if (!_isShowing) return;

            _isShowing = false;
            EventBeforeClosed?.Invoke();
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
            if (_isShowing)
            {
                Logg.LogWarning("[View] View is already showing");
                return true;
            }

            return false;
        }

        protected void FinalizeHide()
        {
            gameObject.SetActive(false);
            EventAfterClosed?.Invoke();

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