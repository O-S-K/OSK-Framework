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
        public UIParticle UIParticle => _rootUI.Particle;
        public Canvas Canvas => _rootUI.Canvas;
        public Camera UICamera => _rootUI.UICamera;

        public RootUI RootUI
        {
            get
            {
                if (_rootUI == null)
                {
                    Logg.LogError("RootUI is null. Please check the initialization of the UIManager.");
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
 
        #region Views

        public T Spawn<T>(string path, object[] data = null, bool cache = true, bool hidePrevView = false)
            where T : View
        {
            return RootUI.Spawn<T>(path, data, cache, hidePrevView);
        }

        public T SpawnCache<T>(T view, object[] data = null, bool hidePrevView = false) where T : View
        {
            return RootUI.Spawn(view, data, hidePrevView);
        }

        public T Open<T>(object[] data = null, bool hidePrevView = false) where T : View
        {
            return RootUI.Open<T>(data, hidePrevView);
        }
        
        public void OpenAddStack<T>(object[] data = null, bool hidePrevView = false, Action<T> onOpened = null) where T : View
        {
             RootUI.OpenAddStack<T>(data, hidePrevView, onOpened);
        }
        
        public void OpenAddStack(View view, object[] data = null, bool hidePrevView = false) 
        {
            RootUI.OpenAddStack(view, data, hidePrevView);
        }

        public View OpenPrevious(bool hidePrevView = false)
        {
           return RootUI.OpenPrevious( isHidePrevPopup: hidePrevView);
        }

        public T TryOpen<T>(object[] data = null, bool isHidePrevPopup = false) where T : View
        {
            return RootUI.TryOpen<T>(data, isHidePrevPopup);
        }

        public void Open(View view, object[] data = null, bool hidePrevView = false)
        {
            RootUI.Open(view, data, hidePrevView);
        }

        public AlertView OpenAlert<T>(AlertSetup setup) where T : AlertView
        {
            return RootUI.OpenAlert<T>(setup);
        }

        public void Hide(View view)
        {
            RootUI.Hide(view);
        }

        public void HideAll()
        {
            RootUI.HideAll();
        }

        public void HideAllIgnoreView<T>() where T : View
        {
            RootUI.HideIgnore<T>();
        }

        public void HideAllIgnoreView<T>(T[] viewsToKeep) where T : View
        {
            RootUI.HideIgnore(viewsToKeep);
        }

        public void Delete<T>(T popup) where T : View
        {
            RootUI.Delete<T>(popup);
        }


        public T Get<T>(bool isInitOnScene = true) where T : View
        {
            return RootUI.Get<T>(isInitOnScene);
        } 

        public bool IsShowing(View view)
        {
            return RootUI.Get<View>().IsShowing;
        }

        public List<View> GetAll(bool isInitOnScene)
        {
            return RootUI.GetAll(isInitOnScene);
        }

        #endregion
    }
}