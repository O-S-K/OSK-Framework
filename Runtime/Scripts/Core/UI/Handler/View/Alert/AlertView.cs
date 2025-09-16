using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OSK
{
    public class AlertView : View
    {
        public GameObject title; /// Title of the alert (require text in component).
        public GameObject message;  /// Message of the alert (require text in component).
        
        public Button okButton;
        public Button cancelButton;
        public float timeHide = 0;
 
        protected override void SetData(object[] data)
        {
            base.SetData(data);
            var setup = data[0] as AlertSetup;
            if(setup == null)
            {
                Logg.LogError("AlertView: setup is null. if override this method, remove base.SetData(setup).");
                return;
            }
            SetTile(setup.title);
            SetMessage(setup.message);
            SetOkButton(setup.onOk);
            SetCancelButton(setup.onCancel);
            SetTimeHide(setup.timeHide);
        }

         
        protected virtual void SetTile(string _title) =>  SetTextComponent(title, _title, "Title");
        protected virtual  void SetMessage(string _message) => SetTextComponent(message, _message, "Message");
        protected virtual  void SetTextComponent(GameObject target, string text, string errorContext)
        {
            if (string.IsNullOrEmpty(text) || target == null)
                return;

            if (target.TryGetComponent<TMP_Text>(out var tmp))
            {
                tmp.text = text;
            }
            else if (target.TryGetComponent<Text>(out var legacyText))
            {
                legacyText.text = text;
            }
            else
            {
                Logg.LogError("UI",$"[AlertView] {errorContext}: No Text or TMP_Text component found.");
            }
        }

        protected virtual  void SetOkButton(Action onOk)
        {
            if (onOk == null)
            {
                okButton?.gameObject.SetActive(false);
                return;
            }

            okButton.BindButton(() =>
            {
                onOk?.Invoke();
                OnClose();
            });
        }

        protected virtual  void SetCancelButton(Action onCancel)
        {
            if (onCancel == null)
            {
                cancelButton?.gameObject.SetActive(false);
                return;
            }

            cancelButton.BindButton(() =>
            {
                onCancel?.Invoke();
                OnClose();
            });
        }

        public virtual void SetTimeHide(float time)
        {
            if (time <= 0)
                return;
            timeHide = time;
            DOVirtual.DelayedCall(time, OnClose);
        }

        protected virtual void OnDisable()
        {
            okButton?.onClick.RemoveAllListeners();
            cancelButton?.onClick.RemoveAllListeners();
        }
        
        protected virtual void OnDestroy()
        {
            okButton?.onClick.RemoveAllListeners();
            cancelButton?.onClick.RemoveAllListeners();
        }

        public virtual void OnClose()
        {
            Logg.Log("UI","AlertView: OnClose called. Time hide left " + timeHide);
            Destroy(gameObject);
        }
    }
}