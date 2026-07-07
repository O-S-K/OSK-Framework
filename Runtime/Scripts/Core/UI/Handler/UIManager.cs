using System;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace OSK
{
    public partial class UIManager : GameFrameworkComponent
    {
        [ReadOnly, SerializeField] private RootUI _rootUI;
        public Canvas Canvas => _rootUI.ScreenCanvas;
        public Camera UICamera => _rootUI.UICamera;

        public RootUI RootUI
        {
            get
            {
                if (_rootUI == null)
                {
                    MyLogger.LogError("RootUI is null. Please check the initialization of the UIManager.");
                    return null;
                }
                return _rootUI;
            }
        }

        public override void OnInit()
        {
            _rootUI = FindObjectOfType<RootUI>();
            if (_rootUI != null)
                _rootUI.Initialize();
        }

        #region Spawn — Tạo view mới từ Resources hoặc prefab cache

        /// <summary>
        /// Spawn view từ Resources path. Nếu đã tồn tại thì Open thay vì spawn lại.
        /// </summary>
        public T Spawn<T>(string path, object data = null, bool cache = true, bool hidePrev = false)
            where T : View
        {
            return RootUI.Spawn<T>(path, data, cache, hidePrev);
        }

        /// <summary>
        /// Spawn view từ prefab reference. Nếu đã tồn tại thì Open.
        /// </summary>
        public T Spawn<T>(T prefab, object data = null, bool hidePrev = false) where T : View
        {
            return RootUI.Spawn(prefab, data, hidePrev);
        }

        #endregion

        #region Open — Mở view đã spawn (hoặc auto-spawn nếu chưa có)

        /// <summary>
        /// Mở view theo type. Auto-spawn nếu chưa có trong cache.
        /// </summary>
        public T Open<T>(object data = null, bool hidePrev = false) where T : View
        {
            return RootUI.Open<T>(data, hidePrev);
        }

        /// <summary>
        /// Mở view bất đồng bộ từ Resources path.
        /// </summary>
        public void OpenAsync<T>(string path, object data = null, bool hidePrev = false, Action<T> onComplete = null) where T : View
        {
            RootUI.OpenAsync<T>(path, data, hidePrev, onComplete);
        }

        /// <summary>
        /// Mở view theo instance reference.
        /// </summary>
        public void Open(View view, object data = null, bool hidePrev = false)
        {
            RootUI.Open(view, data, hidePrev);
        }

        /// <summary>
        /// Mở view, cho phép mở lại nếu đang showing (không skip).
        /// </summary>
        public T TryOpen<T>(object data = null, bool hidePrev = false) where T : View
        {
            return RootUI.TryOpen<T>(data, hidePrev);
        }

        /// <summary>
        /// Quay lại view trước trong history stack.
        /// </summary>
        public View OpenPrevious(bool hideCurrent = false)
        {
            return RootUI.OpenPrevious(isHidePrevPopup: hideCurrent);
        }

        /// <summary>
        /// Mở alert dialog.
        /// </summary>
        public AlertView OpenAlert<T>(AlertSetup setup) where T : AlertView
        {
            return RootUI.OpenAlert<T>(setup);
        }

        #region Tutorial Bridge

        public void ShowTutorial(params RectTransform[] targets)
        {
            ShowTutorial(new HoleViewData(targets));
        }
        
        public void ShowTutorial(params Transform[] targets)
        {
            ShowTutorial(new HoleViewData(targets));
        }

        public void ShowTutorial(HoleViewData data)
        {
            if (!RootUI.TryGet<HoleView>(out var view) || !view.IsShowing)
            {
                RootUI.Open<HoleView>(new object[] { data });
            }
            else
            {
                view.ApplyData(data);
            }
        }

        public void HideTutorial()
        {
            if (RootUI.TryGet<HoleView>(out var view) && view.IsShowing)
            {
                view.Hide();
            }
        }

        #endregion

        #endregion

        #region Enqueue — Xếp hàng views mở tuần tự theo priority

        /// <summary>
        /// Xếp hàng view để mở tuần tự. View tiếp theo mở khi view hiện tại đóng.
        /// </summary>
        public void EnqueueView<T>(object data = null, bool hidePrev = false, Action<T> onOpened = null) where T : View
        {
            RootUI.EnqueueView<T>(data, hidePrev, onOpened);
        }

        /// <summary>
        /// Xếp hàng view instance để mở tuần tự.
        /// </summary>
        public void EnqueueView(View view, object data = null, bool hidePrev = false)
        {
            RootUI.EnqueueView(view, data, hidePrev);
        }

        #endregion

        #region Hide — Ẩn views

        /// <summary>
        /// Ẩn 1 view cụ thể.
        /// </summary>
        public void Hide(View view)
        {
            RootUI.Hide(view);
        }

        /// <summary>
        /// Ẩn tất cả views đang hiển thị.
        /// </summary>
        public void HideAll()
        {
            RootUI.HideAll();
        }

        /// <summary>
        /// Ẩn tất cả views NGOẠI TRỪ type T.
        /// </summary>
        public void HideAllExcept<T>() where T : View
        {
            RootUI.HideIgnore<T>();
        }

        /// <summary>
        /// Ẩn tất cả views NGOẠI TRỪ các view trong danh sách.
        /// </summary>
        public void HideAllExcept<T>(T[] viewsToKeep) where T : View
        {
            RootUI.HideIgnore(viewsToKeep);
        }

        #endregion

        #region Delete — Xóa hẳn view (destroy GameObject)

        /// <summary>
        /// Xóa view khỏi cache và destroy GameObject.
        /// </summary>
        public void Delete<T>(T view) where T : View
        {
            RootUI.Delete<T>(view);
        }

        public void LockInput(bool isLock)
        {
            RootUI.LockInput(isLock);
        }

        #endregion

        #region Query — Truy vấn views

        /// <summary>
        /// Lấy view theo type từ cache.
        /// </summary>
        public T Get<T>() where T : View
        {
            return RootUI.Get<T>(true);
        }

        public bool TryGet<T>(out T view) where T : View
        {
            return RootUI.TryGet<T>(out view);
        }

        /// <summary>
        /// Kiểm tra view có đang hiển thị không.
        /// </summary>
        public bool IsShowing(View view)
        {
            return view != null && view.IsShowing;
        }

        /// <summary>
        /// Kiểm tra view type T có đang hiển thị không.
        /// </summary>
        public bool IsShowing<T>() where T : View
        {
            return RootUI.TryGet<T>(out var view) && view.IsShowing;
        }

        /// <summary>
        /// Lấy tất cả views đã spawn.
        /// </summary>
        public List<View> GetAll()
        {
            return RootUI.GetAll(true);
        }

        #endregion

        #region Builder

        /// <summary>
        /// example usage of builder pattern for opening views with fluent interface:
        /// ex: UIManager.Build<MainScreenView>().Open();
        /// ex: UIManager.Build<InventoryView>().SetData(inventoryData).HidePrevious().OnComplete(view => view.PlayOpenAnimation()).Open();
        /// ex: UIManager.Build<SettingsView>().SetPath("UI/SettingsView").Async().OnComplete(view => view.Init(settingsData)).Open();
        /// ex: UIManager.Build<DialogView, DialogModel>().SetModel(dialogModel).HidePrevious().Open();
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public UIBuilder<T> Build<T>() where T : View
        {
            return new UIBuilder<T>(this);
        }

        public class UIBuilder<T> where T : View
        {
            private readonly UIManager _manager;
            private object _data;
            private bool _hidePrev;
            private string _path;
            private Action<T> _onComplete;
            private bool _isAsync;
            private bool _isEnqueue;
            private bool _isTryOpen;

            public UIBuilder(UIManager manager)
            {
                _manager = manager;
            }

            public UIBuilder<T> SetData(object data)
            {
                _data = data;
                return this;
            }

            public UIBuilder<T> HidePrevious(bool hide = true)
            {
                _hidePrev = hide;
                return this;
            }

            public UIBuilder<T> SetPath(string path)
            {
                _path = path;
                return this;
            }

            public UIBuilder<T> OnComplete(Action<T> onComplete)
            {
                _onComplete = onComplete;
                return this;
            }

            public UIBuilder<T> Async()
            {
                _isAsync = true;
                return this;
            }

            public UIBuilder<T> Enqueue()
            {
                _isEnqueue = true;
                return this;
            }

            public UIBuilder<T> TryOpen()
            {
                _isTryOpen = true;
                return this;
            }

            public T Open()
            {
                if (_isEnqueue)
                {
                    _manager.EnqueueView<T>(_data, _hidePrev, _onComplete);
                    return null;
                }
                
                if (_isAsync)
                {
                    _manager.OpenAsync<T>(_path, _data, _hidePrev, _onComplete);
                    return null;
                }

                T view;
                if (_isTryOpen)
                {
                    view = _manager.TryOpen<T>(_data, _hidePrev);
                }
                else
                {
                    view = _manager.Open<T>(_data, _hidePrev);
                }

                _onComplete?.Invoke(view);
                return view;
            }
        }

        public UIBuilder<TView, TModel> Build<TView, TModel>() where TView : View<TModel>
        {
            return new UIBuilder<TView, TModel>(this);
        }

        public class UIBuilder<TView, TModel> where TView : View<TModel>
        {
            private readonly UIManager _manager;
            private TModel _model;
            private bool _hidePrev;
            private string _path;
            private Action<TView> _onComplete;
            private bool _isAsync;
            private bool _isEnqueue;
            private bool _isTryOpen;

            public UIBuilder(UIManager manager)
            {
                _manager = manager;
            }

            public UIBuilder<TView, TModel> SetModel(TModel model)
            {
                _model = model;
                return this;
            }

            public UIBuilder<TView, TModel> HidePrevious(bool hide = true)
            {
                _hidePrev = hide;
                return this;
            }

            public UIBuilder<TView, TModel> SetPath(string path)
            {
                _path = path;
                return this;
            }

            public UIBuilder<TView, TModel> OnComplete(Action<TView> onComplete)
            {
                _onComplete = onComplete;
                return this;
            }

            public UIBuilder<TView, TModel> Async()
            {
                _isAsync = true;
                return this;
            }

            public UIBuilder<TView, TModel> Enqueue()
            {
                _isEnqueue = true;
                return this;
            }

            public UIBuilder<TView, TModel> TryOpen()
            {
                _isTryOpen = true;
                return this;
            }

            public TView Open()
            {
                if (_isEnqueue)
                {
                    _manager.EnqueueView<TView>(_model, _hidePrev, _onComplete);
                    return null;
                }
                
                if (_isAsync)
                {
                    _manager.OpenAsync<TView>(_path, _model, _hidePrev, _onComplete);
                    return null;
                }

                TView view;
                if (_isTryOpen)
                {
                    view = _manager.TryOpen<TView>(_model, _hidePrev);
                }
                else
                {
                    view = _manager.Open<TView>(_model, _hidePrev);
                }

                _onComplete?.Invoke(view);
                return view;
            }
        }

        #endregion
    }
}
