using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace OSK
{
    [DefaultExecutionOrder(-101)]
    public class RootUI : MonoBehaviour
    {
        #region Queued View

        private class QueuedView
        {
            public View view;
            public object data;
            public bool hidePrevView;
            public Action<View> onOpened;
        }

        #endregion

        #region Lists & Cache

        [BoxGroup("🔍 Views")]
        [ShowInInspector, ReadOnly]
        public List<View> ListViewInit { get; set; } = new();

        [BoxGroup("🔍 Views")]
        [ShowInInspector, ReadOnly]
        public List<View> ListCacheView { get; set; } = new();

        public Stack<View> ListViewHistory { get; set; } = new();

        [ShowInInspector, ReadOnly] private List<QueuedView> _queuedViews = new();

        private bool _isProcessingQueue = false;

        private readonly Dictionary<Type, View> _cacheByType = new();
        private readonly Dictionary<Type, View> _initByType = new();

        #endregion

        #region References

        [Title("📌 References")] 
        [Required, SerializeField] private Camera _uiCamera;

        [Title("🏗️ Container Transforms")] 
        [Required, SerializeField] private Transform _screenContainer;
        [Required, SerializeField] private Transform _popupContainer;
        [Required, SerializeField] private Transform _notifContainer;
        [Required, SerializeField] private Transform _overlayContainer;
        [Required, SerializeField] private Transform _lockContainer;
        
        private Canvas _screenCanvas;
        private Canvas _popupCanvas;
        private Canvas _notifCanvas;
        private Canvas _overlayCanvas;
        private Canvas _lockCanvas;

        private CanvasScaler _screenScaler;
        private CanvasScaler _popupScaler;
        private CanvasScaler _notifScaler;
        private CanvasScaler _overlayScaler;
        private CanvasScaler _lockScaler;
        #endregion

        #region Settings

        [Title("⚙️ Settings")] [SerializeField]
        private bool isPortrait = true;

        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private bool isUpdateRatioScaler = true;

        #endregion

        #region Properties

        public Canvas ScreenCanvas => _screenCanvas;
        public Canvas PopupCanvas => _popupCanvas;
        public Canvas OverlayCanvas => _overlayCanvas;
        public Canvas NotifCanvas => _notifCanvas;
        public Canvas LockCanvas => _lockCanvas;

        public Camera UICamera => _uiCamera;
        public Transform ScreenContainer => _screenContainer;
        public Transform PopupContainer => _popupContainer;
        public Transform OverlayContainer => _overlayContainer;
        public Transform LockContainer => _lockContainer;
        public bool IsPortrait => isPortrait;
        public bool IsDirtySort { get; set; } = true;

        public IEnumerable<Canvas> AllCanvases
        {
            get
            {
                yield return _screenCanvas;
                yield return _popupCanvas;
                yield return _notifCanvas;
                yield return _overlayCanvas;
                yield return _lockCanvas;
            }
        }

        public IEnumerable<CanvasScaler> AllScalers
        {
            get
            {
                yield return _screenScaler;
                yield return _popupScaler;
                yield return _notifScaler;
                yield return _overlayScaler;
                yield return _lockScaler;
            }
        }

        #endregion

        public void Initialize()
        {
            gameObject.name = " ========== [RootUI] ==========";
            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            EnsureContainers();

            var data = Main.Instance.configInit.data;
            if (data.listViewS0 != null)
            {
                Preload();
            }

            if (isUpdateRatioScaler)
            {
                // check if the screen is in portrait mode
                Main.UI.SetupCanvasScaleForRatio();
            }
        }
#if OSK_DEBUG

        private void Update()
        {
            // Shortcut to open Debug View: F2 on PC
            if (Input.GetKeyDown(KeyCode.F2))
            {
                ToggleDebugView();
            } 
        }

        private void ToggleDebugView()
        {
            if (Main.UI.TryGet<DebugConsoleViewDemo>(out var debugView) && debugView.IsShowing)
            {
                debugView.Hide();
            }
            else
            {
                Main.UI.Open<DebugConsoleViewDemo>();
            }
        }
#endif

        public void SetupCanvas()
        {
            SetupSingleCanvas(_screenCanvas, _screenScaler);
            SetupSingleCanvas(_popupCanvas, _popupScaler);
            SetupSingleCanvas(_notifCanvas, _notifScaler);
            SetupSingleCanvas(_overlayCanvas, _overlayScaler);
            SetupSingleCanvas(_lockCanvas, _lockScaler);

#if UNITY_EDITOR
            if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(this))
            {
                UnityEditor.EditorUtility.SetDirty(gameObject);
                MyLogger.Log($"[SetupCanvas] Multi-Canvas Scaled for IsPortrait: {isPortrait}");
            }
#endif
        }

        private void SetupSingleCanvas(Canvas canvas, CanvasScaler scaler)
        {
            if (canvas == null || scaler == null) return;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _uiCamera;
            canvas.referencePixelsPerUnit = 100;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            if (isPortrait)
            {
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0;
            }
            else
            {
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 1;
            }
        }


        #region Init

        private void Preload()
        {
            var listUIPopupSo = Main.Instance.configInit.data.listViewS0.ListView;
            if (listUIPopupSo == null)
            {
                MyLogger.LogError("[View] is null");
                return;
            }

            ListViewInit.Clear();
            _initByType.Clear();

            foreach (var entry in listUIPopupSo)
            {
                if (entry.view == null)
                {
                    MyLogger.LogError("[View] Found a null view in ListViewSO. Skipping.");
                    continue;
                }

                // Lưu vào cache
                ListViewInit.Add(entry.view);
                _initByType[entry.view.GetType()] = entry.view;

                // Khởi tạo luôn nếu được đánh dấu PreloadSpawn
                if (entry.view.isPreloadSpawn)
                {
                    SpawnViewCache(entry.view);
                }
            }
        }

        #endregion

        #region Spawn

        public T Spawn<T>(T view, object data, bool hidePrevView) where T : View
        {
            return IsExist<T>() ? Open<T>(data, hidePrevView) : SpawnViewCache(view);
        }

        public T Spawn<T>(string path, object data, bool cache, bool hidePrevView) where T : View
        {
            if (IsExist<T>())
            {
                return Open<T>(data, hidePrevView);
            }

            var view = SpawnFromResource<T>(path);
            if (!cache) return view;
  
            if (!ListCacheView.Contains(view))
            {
                ListCacheView.Add(view);
                _cacheByType[typeof(T)] = view;
            }

            return view;
        }

        public T SpawnViewCache<T>(T view) where T : View
        { 
            var getView = Instantiate(view, GetContainer(view.viewType), false);
            getView.gameObject.SetActive(false);
            getView.Initialize(this);

            MyLogger.Log($"[View] Spawn view: {getView.name}");
            if (!ListCacheView.Contains(getView))
            {
                ListCacheView.Add(getView);
                _cacheByType[getView.GetType()] = getView;
                IsDirtySort = true;
            }

            return getView;
        }

        public T SpawnAlert<T>(T view, bool usePool) where T : View
        {
            var container = GetContainer(view.viewType);
            var getView = usePool
                ? Main.Pool.Spawn<T>(KEY_POOL.KEY_UI_ALERT, view, container)
                : Instantiate(view, container, false);
            getView.gameObject.SetActive(true);
            getView.Initialize(this);

            MyLogger.Log($"[View] Spawn Alert view: {getView.name}");
            return getView;
        }

        #endregion

        #region Open

        public View Open(View view, object data = null, bool hidePrevView = false, bool checkShowing = true)
        {
            if (view == null)
            {
                MyLogger.LogError("[View] Cannot open null view.");
                return null;
            }

            var viewType = view.GetType();
            if (!_cacheByType.TryGetValue(viewType, out var getView))
            {
                getView = ListCacheView.Find(x => x != null && x.GetType() == viewType);
                if (getView != null)
                {
                    _cacheByType[viewType] = getView;
                }
                else if (!_initByType.TryGetValue(viewType, out var viewPrefab))
                {
                    MyLogger.LogError($"[View] Cannot open {viewType.Name}: not found in cache or ListViewSO.");
                    return null;
                }
                else
                {
                    getView = SpawnViewCache(viewPrefab);
                }
            }

            if (getView.IsShowing && checkShowing)
            {
                MyLogger.Log($"[View] Already showing: {getView.name}");
                return getView;
            }

            if (hidePrevView && ListViewHistory.Count > 0)
            {
                var prevView = ListViewHistory.Peek();
                prevView.Hide();
            }

            getView.Open(data);
            ListViewHistory.Push(getView);
            MyLogger.Log($"[View] Opened view: {getView.name}");
            return getView;
        }

        public T Open<T>(object data = null, bool hidePrevView = false, bool checkShowing = true) where T : View
        {
            var viewType = typeof(T);
            if (!TryGet<T>(out var getView))
            {
                if (!_initByType.TryGetValue(viewType, out var prefab))
                {
                    MyLogger.LogError($"[View] Cannot open {viewType.Name}: not found in cache or ListViewSO.");
                    return null;
                }

                getView = SpawnViewCache(prefab) as T;
                if (getView == null)
                {
                    MyLogger.LogError($"[View] Cannot open {viewType.Name}: registered view type mismatch.");
                    return null;
                }
            }

            if (getView.IsShowing && checkShowing)
            {
                MyLogger.Log($"[View] Already showing: {getView.name}");
                return getView;
            }

            if (hidePrevView && ListViewHistory.Count > 0)
            {
                var prevView = ListViewHistory.Peek();
                prevView.Hide();
            }

            getView.Open(data);
            ListViewHistory.Push(getView);
            MyLogger.Log($"[View] Opened view: {getView.name}");
            return getView;
        }

        public T TryOpen<T>(object data = null, bool hidePrevView = false) where T : View
        {
            return Open<T>(data, hidePrevView, false);
        }

        public void EnqueueView(View view, object data = null, bool hidePrevView = false)
        {
            _queuedViews.Add(new QueuedView { view = view, data = data, hidePrevView = hidePrevView });
            _queuedViews.Sort((a, b) => b.view.Priority.CompareTo(a.view.Priority));
            if (!_isProcessingQueue)
                StartCoroutine(ProcessQueue());
        }

        #region Async Loading (Resources Flow)
        public void OpenAsync<T>(string path, object data = null, bool hidePrev = false, Action<T> onComplete = null)
            where T : View
        {
            StartCoroutine(OpenAsyncRoutine<T>(path, data, hidePrev, onComplete));
        }

        private IEnumerator OpenAsyncRoutine<T>(string path, object data = null, bool hidePrev = false,
            Action<T> onComplete = null) where T : View
        {
            // Check cache
            if (TryGet<T>(out var cached))
            {
                Open(cached, data, hidePrev);
                onComplete?.Invoke(cached);
                yield break;
            }

            // Lock input while loading
            LockInput(true);

            ResourceRequest request = Resources.LoadAsync<GameObject>(path);
            yield return request;

            LockInput(false);

            if (request.asset == null)
            {
                MyLogger.LogError($"[View] Async Load failed: {path}");
                yield break;
            }

            var prefab = (request.asset as GameObject).GetComponent<T>();
            var view = Spawn(prefab, data, hidePrev);
            onComplete?.Invoke(view);
        }

        #endregion

        public void EnqueueView<T>(object data = null, bool hidePrev = false, Action<T> onOpened = null)
            where T : View
        {
            // O(1) cached lookup
            _cacheByType.TryGetValue(typeof(T), out var cached);
            var getView = cached as T;

            if (getView == null)
            {
                _initByType.TryGetValue(typeof(T), out var initPrefab);
                var prefab = initPrefab as T;
                if (prefab == null)
                {
                    MyLogger.LogError(
                        $"[EnqueueView<{typeof(T).Name}>] Not found view prefab for type: {typeof(T).Name}");
                    return;
                }

                getView = SpawnViewCache(prefab);
            }

            var queued = new QueuedView
            {
                view = getView,
                data = data,
                hidePrevView = hidePrev,
                onOpened = v => onOpened?.Invoke(v as T)
            };

            _queuedViews.Add(queued);
            _queuedViews.Sort((a, b) => b.view.Priority.CompareTo(a.view.Priority));

            if (!_isProcessingQueue)
                StartCoroutine(ProcessQueue());
        }


        private IEnumerator ProcessQueue()
        {
            _isProcessingQueue = true;

            while (_queuedViews.Count > 0)
            {
                // Already sorted — find first non-showing view (no LINQ)
                QueuedView next = null;
                for (int i = 0; i < _queuedViews.Count; i++)
                {
                    var q = _queuedViews[i];
                    if (q.view != null && !q.view.IsShowing)
                    {
                        next = q;
                        break;
                    }
                }
                if (next == null)
                {
                    yield return null;
                    continue;
                }

                var openedView = Open(next.view, next.data, next.hidePrevView);
                next.onOpened?.Invoke(openedView);

                // Wait until the view is closed
                yield return new WaitUntil(() => next.view == null || !next.view.IsShowing);
                _queuedViews.Remove(next);
            }

            _isProcessingQueue = false;
        }

        /// <summary>
        /// Open previous view in history
        /// </summary>
        public View OpenPrevious(object data = null, bool isHidePrevPopup = false)
        {
            if (ListViewHistory.Count <= 1)
            {
                MyLogger.LogWarning("[View] No previous view to open");
                return null;
            }

            // Pop current view
            var currentView = ListViewHistory.Pop();

            if (isHidePrevPopup && currentView != null && !currentView.Equals(null))
            {
                try
                {
                    currentView.Hide();
                }
                catch (Exception ex)
                {
                    MyLogger.LogError($"[View] Error hiding current view: {ex.Message}");
                }
            }

            // Peek previous view
            var previousView = ListViewHistory.Peek();
            if (previousView == null || previousView.Equals(null))
            {
                MyLogger.LogWarning("[View] Previous view is null or destroyed");
                return null;
            }

            previousView.Open(data);
            MyLogger.Log($"[View] Opened previous view: {previousView.name}");
            return previousView;
        }

        /// <summary>
        /// Spawn and open alert view, destroy it when closed
        /// </summary>
        public AlertView OpenAlert<T>(AlertSetup setup) where T : AlertView
        {
            _initByType.TryGetValue(typeof(T), out var initPrefab);
            var viewPrefab = initPrefab as T;
            if (viewPrefab == null)
            {
                MyLogger.LogError($"[View] Can't find view prefab for type: {typeof(T).Name}");
                return null;
            }

            var view = SpawnAlert(viewPrefab, setup.usePool);
            view.Open(setup);
            MyLogger.Log($"[View] Opened view: {view.name}");
            return view;
        }

        public void LockInput(bool isLock)
        {
            if (_lockContainer != null)
                _lockContainer.gameObject.SetActive(isLock);
        }

        #endregion

        #region Get

        public View Get(View view, bool isInitOnScene)
        {
            var getView = GetAll(isInitOnScene).Find(x => x == view);
            if (getView == null)
            {
                MyLogger.LogError($"[View] Can't find view: {view.name}");
                return null;
            }

            if (!getView.isInitOnScene)
            {
                MyLogger.LogError($"[View] {view.name} is not init on scene");
            }

            return getView;
        }

        public T Get<T>(bool isInitOnScene = true) where T : View
        {
            if (isInitOnScene && _cacheByType.TryGetValue(typeof(T), out var cached))
                return cached as T;

            var view = GetAll(isInitOnScene)?.Find(x => x is T) as T;
            if (view == null)
            {
                MyLogger.LogError($"[View] Can't find view : {typeof(T).Name} InitOnScene {isInitOnScene}");
                return null;
            }

            if (!view.isInitOnScene)
            {
                MyLogger.LogError($"[View] {typeof(T).Name} is not init on scene");
            }

            return view;
        }

        public bool TryGet<T>(out T view) where T : View
        {
            if (_cacheByType.TryGetValue(typeof(T), out var cached))
            {
                view = cached as T;
                return view != null;
            }

            view = ListCacheView.Find(x => x != null && x is T) as T;
            if (view != null)
            {
                _cacheByType[typeof(T)] = view;
                return true;
            }

            view = null;
            return false;
        }

        public View Get(View view)
        {
            var getView = GetAll(true).Find(x => x == view);
            if (getView != null)
            {
                MyLogger.Log($"[View] Found view: {getView.name} is showing {getView.IsShowing}");
                return getView;
            }

            MyLogger.LogError($"[View] Can't find view: {view.name}");
            return null;
        }

        public List<View> GetAll(bool isInitOnScene)
        {
            if (isInitOnScene) 
                return ListCacheView;

            var views = ListViewInit.FindAll(x => x.isInitOnScene);
            if (views.Count > 0)
            {
                MyLogger.Log($"[View] Found {views.Count} views");
                return views;
            }

            MyLogger.LogError($"[View] Can't find any view");
            return null;
        }

        #endregion

        #region Hide

        public void Hide(View view)
        {
            if (view == null || !ListCacheView.Contains(view))
            {
                MyLogger.LogError($"[View] Can't hide: invalid view");
                return;
            }

            if (!view.IsShowing)
            {
                MyLogger.Log($"[View] Can't hide: {view.name} is not showing");
                return;
            }

            try
            {
                view.Hide();
            }
            catch (Exception ex)
            {
                MyLogger.LogError($"[View] Hide failed: {view.name} - {ex.Message}");
            }
        }

        public void HideIgnore<T>() where T : View
        {
            foreach (var view in ListCacheView.ToList())
            {
                if (view == null)
                {
                    MyLogger.Log($"[View] {nameof(view)} is null in HideIgnore");
                    continue;
                }

                if (view is T) continue;
                if (!view.IsShowing) continue;

                try
                {
                    view.Hide();
                }
                catch (Exception ex)
                {
                    MyLogger.LogError($"[View] Error hiding view {view.name}: {ex.Message}");
                }
            }
        }

        public void HideIgnore<T>(T[] viewsToKeep) where T : View
        {
            HashSet<T> keepSet = viewsToKeep != null ? new HashSet<T>(viewsToKeep) : null;

            for (int i = ListCacheView.Count - 1; i >= 0; i--)
            {
                var view = ListCacheView[i];

                if (!view)
                {
                    MyLogger.Log("[View] Null found in HideIgnore -> removing");
                    ListCacheView.RemoveAt(i);
                    continue;
                }

                if (!view.IsShowing) continue;

                if (keepSet != null && view is T tView && keepSet.Contains(tView))
                    continue;

                try
                {
                    view.Hide();
                }
                catch (Exception ex)
                {
                    MyLogger.LogError($"[View] Error hiding {view.name}: {ex}");
                }
            }
        }


        public void HideAll()
        {
            for (int i = ListCacheView.Count - 1; i >= 0; i--)
            {
                var view = ListCacheView[i];

                if (!view)
                {
                    MyLogger.LogError("[View] Null found in HideAll -> removing");
                    ListCacheView.RemoveAt(i);
                    continue;
                }

                if (!view.IsShowing) continue;
                try
                {
                    view.Hide();
                }
                catch (Exception ex)
                {
                    MyLogger.LogError($"[View] Error hiding {view.name}: {ex}");
                }
            }
        }

        public void CleanNull()
        {
            for (int i = ListCacheView.Count - 1; i >= 0; i--)
                if (!ListCacheView[i])
                    ListCacheView.RemoveAt(i);
        }

        #endregion

        #region Remove

        public void Remove(View view)
        {
            if (view == null || ListViewHistory.Count == 0)
                return;

            if (ListViewHistory.Peek() == view)
            {
                ListViewHistory.Pop();
                Hide(view);
            }
            else
            {
                MyLogger.LogWarning($"[View] Can't remove {view.name}: not on top of history");
            }
        }

        public void Remove(bool hidePrevView = false)
        {
            if (ListViewHistory.Count <= 0)
                return;

            var curView = ListViewHistory.Pop();
            curView.Hide();

            if (hidePrevView)
                OpenPrevious();
        }

        public void RemoveAll()
        {
            while (ListViewHistory.Count > 0)
            {
                var curView = ListViewHistory.Pop();
                if (curView == null)
                {
                    MyLogger.LogWarning($"[View] {nameof(curView)} null view");
                    continue;
                }

                if (!curView.IsShowing)
                    continue;

                try
                {
                    curView.Hide();
                }
                catch (Exception ex)
                {
                    MyLogger.LogError($"[View] Error hiding popped view: {ex.Message}");
                }
            }
        }

        #endregion

        #region Delete

        public void Delete<T>(T view, Action action = null) where T : View
        {
            if (!ListCacheView.Contains(view))
                return;

            MyLogger.Log($"[View] Delete view: {view.name}");
            ListCacheView.Remove(view);
            _cacheByType.Remove(view.GetType());
            IsDirtySort = true;
            action?.Invoke();
            if (Main.Pool.IsFromPool(view))
            {
                Main.Pool.Despawn(view);
            }
            else
            {
                Destroy(view.gameObject);
            }
        }

        #endregion

        #region Private

        private T SpawnFromResource<T>(string path) where T : View
        {
            var prefab = Resources.Load<T>(path);
            if (prefab == null)
            {
                MyLogger.LogError($"[View] Can't find view with path: {path}");
                return null;
            }

            var view = Instantiate(prefab, GetContainer(prefab.viewType), false);
            return SpawnViewCache(view);
        }

        private void EnsureContainers()
        {
            _screenContainer = EnsureCanvasContainer("ScreenCanvas", _screenContainer, out _screenCanvas, out _screenScaler, 0);
            _popupContainer = EnsureCanvasContainer("PopupCanvas", _popupContainer, out _popupCanvas, out _popupScaler, 10);
            _notifContainer = EnsureCanvasContainer("NotifCanvas", _notifContainer, out _notifCanvas, out _notifScaler, 15);
            _overlayContainer = EnsureCanvasContainer("OverlayCanvas", _overlayContainer, out _overlayCanvas,  out _overlayScaler, 20);
            _lockContainer = EnsureCanvasContainer("LockCanvas", _lockContainer, out _lockCanvas, out _lockScaler, 100);

            if (_lockContainer != null && _lockContainer.GetComponent<Image>() == null)
            {
                var img = _lockContainer.gameObject.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 0);
                img.raycastTarget = true;
                _lockContainer.gameObject.SetActive(false);
            }
        }

        private Transform EnsureCanvasContainer(string name, Transform existingContainer, out Canvas canvas,
            out CanvasScaler scaler, int sortOrder)
        {
            Transform t;
            if (existingContainer != null)
            {
                t = existingContainer;
            }
            else
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                go.transform.SetParent(transform, false);
                t = go.transform;
            }

            canvas = t.gameObject.GetOrAdd<Canvas>();
            scaler = t.gameObject.GetOrAdd<CanvasScaler>();
            t.gameObject.GetOrAdd<GraphicRaycaster>();

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _uiCamera;
            canvas.sortingOrder = sortOrder;
            canvas.gameObject.layer = LayerMask.NameToLayer("UI");
            return t;
        }

        public Transform GetContainer(EViewType type)
        {
            return type switch
            {
                EViewType.Screen => _screenContainer,
                EViewType.Popup => _popupContainer,
                EViewType.Alert => _notifContainer,
                EViewType.Overlay => _overlayContainer,
                _ => transform
            };
        }

        private bool IsExist<T>() where T : View
        {
            return _cacheByType.ContainsKey(typeof(T));
        }

        #endregion

        #region Debug

        public void LogAllViews()
        {
            MyLogger.Log($"[View] Total views: {ListCacheView.Count}");
            foreach (var view in ListCacheView)
            {
                MyLogger.Log($"[View] View: {view.name} - IsShowing: {view.IsShowing}");
            }

            MyLogger.Log($"[View] Total views: {ListViewInit.Count}");
            foreach (var view in ListViewInit)
            {
                MyLogger.Log($"[View] View: {view.name} - IsShowing: {view.IsShowing}");
            }

            MyLogger.Log($"[View] Total views: {ListViewHistory.Count}");
            foreach (var view in ListViewHistory)
            {
                MyLogger.Log($"[View] View: {view.name} - IsShowing: {view.IsShowing}");
            }
        }

        #endregion
    }
}
