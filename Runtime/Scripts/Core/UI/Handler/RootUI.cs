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
            public object[] data;
            public bool hidePrevView;
            public Action<View> onOpened;
        }
        #endregion

        #region Lists & Cache

        [BoxGroup("🔍 Views")] [ShowInInspector, ReadOnly]
        public List<View> ListViewInit { get; private set; } = new();

        [BoxGroup("🔍 Views")] [ShowInInspector, ReadOnly]
        public List<View> ListCacheView { get; private set; } = new();

        [HideInInspector]
        public Stack<View> ListViewHistory { get; private set; } = new();

        [ShowInInspector, ReadOnly]
        private List<QueuedView> _queuedViews = new();

        private bool _isProcessingQueue = false;

        #endregion

        #region References

        [Title("📌 References")]
        [Required, SerializeField] private Camera _uiCamera;
        [Required, SerializeField] private Canvas _canvas;
        [Required, SerializeField] private CanvasScaler _canvasScaler;
        [SerializeField] private Transform _viewContainer;

        #endregion

        #region Settings

        [Title("⚙️ Settings")]
        [SerializeField] private bool isPortrait = true;
        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private bool isUpdateRatioScaler = true;
        [SerializeField] private bool enableLog = true;

        #endregion

        #region Properties

        public Canvas Canvas => _canvas;
        public CanvasScaler CanvasScaler => _canvasScaler;
        public Camera UICamera => _uiCamera;
        public Transform ViewContainer => _viewContainer;
        public bool IsPortrait => isPortrait;
        public bool EnableLog => enableLog;

        #endregion

        public void Initialize()
        {
            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            var data = Main.Instance.configInit.data;
            if (data.listViewS0 != null)
            {
                data.listViewS0.CheckNullView();
                Preload();
            }
            if (isUpdateRatioScaler)
            {
                // check if the screen is in portrait mode
                Main.UI.SetupCanvasScaleForRatio();
            }
        }

        public void SetupCanvas()
        {
            _canvas.referencePixelsPerUnit = 100;
            _canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            if (isPortrait)
            {
                _canvasScaler.referenceResolution = new Vector2(1080, 1920);
                _canvasScaler.matchWidthOrHeight = 0;
            }
            else
            {
                _canvasScaler.referenceResolution = new Vector2(1920, 1080);
                _canvasScaler.matchWidthOrHeight = 1;
            }

#if UNITY_EDITOR
            if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(this))
            {
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(_canvas);
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(_canvasScaler);

                UnityEditor.EditorUtility.SetDirty(_canvas);
                UnityEditor.EditorUtility.SetDirty(_canvasScaler);
                UnityEditor.EditorUtility.SetDirty(gameObject);
                Logg.Log($"[SetupCanvas] IsPortrait: {isPortrait} => Saved to prefab instance");
            }
#endif
        }


        #region Init

        private void Preload()
        {
            var listUIPopupSo = Main.Instance.configInit.data.listViewS0.Views;
            if (listUIPopupSo == null)
            {
                Logg.LogError("[View] is null", isLog: enableLog);
                return;
            }
 
            ListViewInit.Clear();
            ListViewInit = listUIPopupSo.Select(view => view.view).ToList();

            foreach (var view in ListViewInit)
            {
                if (view.isPreloadSpawn)
                {
                    SpawnViewCache(view);
                }
            }
        }

        #endregion

        #region Spawn

        public T Spawn<T>(T view, object[] data, bool hidePrevView) where T : View
        {
            return IsExist<T>() ? Open<T>(data, hidePrevView) : SpawnViewCache(view);
        }

        public T Spawn<T>(string path, object[] data, bool cache, bool hidePrevView) where T : View
        {
            if (IsExist<T>())
            {
                return Open<T>(data, hidePrevView);
            }

            var view = SpawnFromResource<T>(path);
            if (!cache) return view;

            if (ListCacheView.Contains(view))
                ListCacheView.Add(view);

            return view;
        }

        public T SpawnViewCache<T>(T view) where T : View
        {
            var _view = Instantiate(view, _viewContainer);
            _view.gameObject.SetActive(false);
            _view.Initialize(this);

            _view.transform.localPosition = Vector3.zero;
            _view.transform.localScale = Vector3.one;

            Logg.Log($"[View] Spawn view: {_view.name}", isLog: enableLog);
            if (!ListCacheView.Contains(_view))
                ListCacheView.Add(_view);
            return _view;
        }

        public T SpawnAlert<T>(T view) where T : View
        {
            var _view = Instantiate(view, _viewContainer);
            _view.gameObject.SetActive(false);
            _view.Initialize(this);

            _view.transform.localPosition = Vector3.zero;
            _view.transform.localScale = Vector3.one;

            Logg.Log($"[View] Spawn Alert view: {_view.name}", isLog: enableLog);
            return _view;
        }

        #endregion

        #region Open

        public View Open(View view, object[] data = null, bool hidePrevView = false, bool checkShowing = true)
        {
            var _view = ListCacheView.FirstOrDefault(v => v.GetType() == view.GetType());
            if (hidePrevView && ListViewHistory.Count > 0)
            {
                var prevView = ListViewHistory.Peek();
                prevView.Hide();
            }

            if (_view == null)
            {
                var viewPrefab = ListViewInit.FirstOrDefault(v => v.GetType() == view.GetType());
                if (viewPrefab == null)
                {
                    Logg.LogError($"[View] Can't find view prefab for type: {view.GetType().Name}", isLog: enableLog);
                    return null;
                }

                _view = SpawnViewCache(viewPrefab);
            }

            if (_view.IsShowing && checkShowing)
            {
                Logg.Log($"[View] Opened view IsShowing: {_view.name}", isLog: enableLog);
                return _view;
            }

            _view.Open(data);
            ListViewHistory.Push(_view);
            Logg.Log($"[View] Opened view: {_view.name}", isLog: enableLog);
            return _view;
        }

        public T Open<T>(object[] data = null, bool hidePrevView = false, bool checkShowing = true) where T : View
        {
            var _view = ListCacheView.FirstOrDefault(v => v.GetType() == typeof(T)) as T;
            if (hidePrevView && ListViewHistory.Count > 0)
            {
                var prevView = ListViewHistory.Peek();
                prevView.Hide();
            }

            if (_view == null)
            {
                var viewPrefab = ListViewInit.FirstOrDefault(v => v.GetType() == typeof(T)) as T;
                if (viewPrefab == null)
                {
                    Logg.LogError($"[View] Can't find view prefab for type: {typeof(T).Name}", isLog: enableLog);
                    return null;
                }

                _view = SpawnViewCache(viewPrefab);
            }

            if (_view.IsShowing && checkShowing)
            {
                Logg.Log($"[View] Opened view: {_view.name}", isLog: enableLog);
                return _view;
            }

            _view.Open(data);
            ListViewHistory.Push(_view);
            Logg.Log($"[View] Opened view: {_view.name}", isLog: enableLog);
            return _view;
        }

        public T TryOpen<T>(object[] data = null, bool hidePrevView = false) where T : View
        {
            return Open<T>(data, hidePrevView, false);
        }
        
        public void OpenAddStack(View view, object[] data = null, bool hidePrevView = false)
        {
            _queuedViews.Add(new QueuedView
            {
                view = view,
                data = data,
                hidePrevView = hidePrevView
            });

            // Sort the queue by priority
            _queuedViews = _queuedViews.OrderByDescending(q => q.view.Depth).ToList();

            if (!_isProcessingQueue)
            {
                StartCoroutine(ProcessQueue());
            }
        }
        
        public void OpenAddStack<T>(object[] data = null, bool hidePrev = false, Action<T> onOpened = null) where T : View
        {
            var _view = ListCacheView.FirstOrDefault(v => v is T) as T;

            if (_view == null)
            {
                var prefab = ListViewInit.FirstOrDefault(v => v is T) as T;
                if (prefab == null)
                {
                    Logg.LogError($"[OpenAddStack<{typeof(T).Name}>] Not found view prefab for type: {typeof(T).Name}", isLog: enableLog);
                    return;
                }

                _view = SpawnViewCache(prefab);
            }

            var queued = new QueuedView
            {
                view = _view,
                data = data,
                hidePrevView = hidePrev,
                onOpened = v => onOpened?.Invoke(v as T)
            };

            _queuedViews.Add(queued);
 
            // Allways sort the queue by priority
            if (!_isProcessingQueue)
                StartCoroutine(ProcessQueue());
        }

        
        private IEnumerator ProcessQueue()
        {
            _isProcessingQueue = true;

            while (_queuedViews.Count > 0)
            {
                // Get the next view in the queue that is not showing
                var next = _queuedViews
                    .Where(q => q.view != null && !q.view.IsShowing)
                    .OrderByDescending(q => q.view.Priority) //  Sort by priority
                    .FirstOrDefault();

                if (next == null)
                {
                    //  All views are already showing or null, wait for next frame
                    yield return null;
                    continue;
                }
                 
                var openedView = Open(next.view, next.data, next.hidePrevView);
                next.onOpened?.Invoke(openedView);

                // Wait  until the view is closed
                yield return new WaitUntil(() => next.view == null || !next.view.IsShowing);
                // Remove view from queue
                _queuedViews.Remove(next);
            }

            _isProcessingQueue = false;
        }
        
        /// <summary>
        /// Open previous view in history
        /// </summary>
        public View OpenPrevious(object[] data = null, bool isHidePrevPopup = false)
        {
            if (ListViewHistory.Count <= 1)
            {
                Logg.LogWarning("[View] No previous view to open", isLog: enableLog);
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
                    Logg.LogError($"[View] Error hiding current view: {ex.Message}", isLog: enableLog);
                }
            }

            // Peek previous view
            var previousView = ListViewHistory.Peek();
            if (previousView == null || previousView.Equals(null))
            {
                Logg.LogWarning("[View] Previous view is null or destroyed", isLog: enableLog);
                return null;
            }

            previousView.Open(data);
            Logg.Log($"[View] Opened previous view: {previousView.name}", isLog: enableLog);
            return previousView;
        }

        /// <summary>
        /// Spawn and open alert view, destroy it when closed
        /// </summary>
        public AlertView OpenAlert<T>(AlertSetup setup) where T : AlertView
        {
            var viewPrefab = ListViewInit.FirstOrDefault(v => v.GetType() == typeof(T)) as T;
            if (viewPrefab == null)
            {
                Logg.LogError($"[View] Can't find view prefab for type: {typeof(T).Name}", isLog: enableLog);
                return null;
            }

            var view = SpawnAlert(viewPrefab);
            view.Open(new object[] { setup });
            Logg.Log($"[View] Opened view: {view.name}", isLog: enableLog);
            return view;
        }

        #endregion

        #region Get

        public View Get(View view, bool isInitOnScene)
        {
            var _view = GetAll(isInitOnScene).Find(x => x == view);
            if (_view == null)
            {
                Logg.LogError($"[View] Can't find view: {view.name}", isLog: enableLog);
                return null;
            }

            if (!_view.isInitOnScene)
            {
                Logg.LogError($"[View] {view.name} is not init on scene", isLog: enableLog);
            }

            return _view;
        }

        public T Get<T>(bool isInitOnScene = true) where T : View
        {
            var _view = GetAll(isInitOnScene).Find(x => x is T) as T;
            if (_view == null)
            {
                Logg.LogError($"[View] Can't find view: {typeof(T).Name}", isLog: enableLog);
                return null;
            }

            if (!_view.isInitOnScene)
            {
                Logg.LogError($"[View] {typeof(T).Name} is not init on scene", isLog: enableLog);
            }

            return _view;
        }

        public View Get(View view)
        {
            var _view = GetAll(true).Find(x => x == view);
            if (_view != null)
            {
                Logg.Log($"[View] Found view: {_view.name} is showing {_view.IsShowing}", isLog: enableLog);
                return _view;
            }

            Logg.LogError($"[View] Can't find view: {view.name}", isLog: enableLog);
            return null;
        }

        public List<View> GetAll(bool isInitOnScene)
        {
            if (isInitOnScene) // check if the view is already initialized
                return ListCacheView;

            var views = ListViewInit.FindAll(x => x.isInitOnScene);
            if (views.Count > 0)
            {
                Logg.Log($"[View] Found {views.Count} views", isLog: enableLog);
                return views;
            }

            Logg.LogError($"[View] Can't find any view", isLog: enableLog);
            return null;
        }

        #endregion

        #region Hide

        public void Hide(View view)
        {
            if (view == null || !ListCacheView.Contains(view))
            {
                Logg.LogError($"[View] Can't hide: invalid view", isLog: enableLog);
                return;
            }

            if (!view.IsShowing)
            {
                Logg.Log($"[View] Can't hide: {view.name} is not showing", isLog: enableLog);
                return;
            }

            try
            {
                view.Hide();
            }
            catch (Exception ex)
            {
                Logg.LogError($"[View] Hide failed: {view.name} - {ex.Message}", isLog: enableLog);
            }
        }

        public void HideIgnore<T>() where T : View
        {
            foreach (var view in ListCacheView.ToList())
            {
                if (view == null)
                {
                    Logg.Log($"[View] {nameof(view)} is null in HideIgnore", isLog: enableLog);
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
                    Logg.LogError($"[View] Error hiding view {view.name}: {ex.Message}", isLog: enableLog);
                }
            }
        }

        public void HideIgnore<T>(T[] viewsToKeep) where T : View
        {
            foreach (var view in ListCacheView.ToList())
            {
                if (view == null)
                {
                    Logg.Log($"[View] {nameof(view)}  is null in HideIgnore", isLog: enableLog);
                    continue;
                }

                if (view is not T tView || viewsToKeep.Contains(tView)) continue;
                if (!view.IsShowing) continue;

                try
                {
                    view.Hide();
                }
                catch (Exception ex)
                {
                    Logg.LogError($"[View] Error hiding view {view.name}: {ex.Message}", isLog: enableLog);
                }
            }
        }

        public void HideAll()
        {
            var views = ListCacheView.Where(view => view.IsShowing).ToList();
            foreach (var view in views)
            {
                if (view == null)
                {
                    Logg.LogError($"[View] {nameof(view)} is null in HideAll", isLog: enableLog);
                    ListCacheView.Remove(view);
                    continue;
                }

                try
                {
                    view.Hide();
                }
                catch (Exception ex)
                {
                    Logg.LogError($"[View] Error hiding view: {ex.Message}", isLog: enableLog);
                }
            }
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
                Logg.LogWarning($"[View] Can't remove {view.name}: not on top of history", isLog: enableLog);
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
                    Logg.LogWarning($"[View] {nameof(curView)} null view", isLog: enableLog);
                    continue;
                }

                try
                {
                    curView.Hide();
                }
                catch (Exception ex)
                {
                    Logg.LogError($"[View] Error hiding popped view: {ex.Message}", isLog: enableLog);
                }
            }
        }

        #endregion

        #region Delete

        public void Delete<T>(T view, Action action = null) where T : View
        {
            if (!ListCacheView.Contains(view))
                return;

            Logg.Log($"[View] Delete view: {view.name}", isLog: enableLog);
            ListCacheView.Remove(view);
            action?.Invoke();
            Destroy(view.gameObject);
        }

        #endregion

        #region Sort oder

        public List<View> GetSortedChildPages(Transform container)
        {
            List<View> childPages = new List<View>();
            for (int i = 0; i < container.childCount; i++)
            {
                var childPage = container.GetChild(i).GetComponent<View>();
                if (childPage != null)
                    childPages.Add(childPage);
            }

            return childPages;
        }

        public int FindInsertIndex(List<View> childPages, int depth)
        {
            int left = 0, right = childPages.Count - 1;
            int insertIndex = childPages.Count;

            while (left <= right)
            {
                int mid = (left + right) / 2;
                if (depth < childPages[mid].Depth)
                {
                    insertIndex = mid;
                    right = mid - 1;
                }
                else
                {
                    left = mid + 1;
                }
            }

            return insertIndex;
        }

        #endregion

        #region Private

        private T SpawnFromResource<T>(string path) where T : View
        {
            var view = Instantiate(Resources.Load<T>(path), _viewContainer);
            if (view != null)
                return SpawnViewCache(view);
            Logg.LogError($"[View] Can't find popup with path: {path}");
            return null;
        }

        private bool IsExist<T>() where T : View
        {
            return ListCacheView.Exists(x => x is T);
        }

        #endregion

        #region Debug

        public void LogAllViews()
        {
            Logg.Log($"[View] Total views: {ListCacheView.Count}");
            foreach (var view in ListCacheView)
            {
                Logg.Log($"[View] View: {view.name} - IsShowing: {view.IsShowing}");
            }

            Logg.Log($"[View] Total views: {ListViewInit.Count}");
            foreach (var view in ListViewInit)
            {
                Logg.Log($"[View] View: {view.name} - IsShowing: {view.IsShowing}");
            }

            Logg.Log($"[View] Total views: {ListViewHistory.Count}");
            foreach (var view in ListViewHistory)
            {
                Logg.Log($"[View] View: {view.name} - IsShowing: {view.IsShowing}");
            }
        }

        #endregion
    }
}