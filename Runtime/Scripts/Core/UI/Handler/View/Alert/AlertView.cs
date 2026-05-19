using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OSK
{
    public class AlertView : View
    {
        public GameObject title;
        public GameObject message;
        public Button okButton;

        public Button cancelButton;
        public float timeHide = 0;
        public AlertSetup alertSetup;
        private Tween _autoHideTween;

        protected override void OnInit()
        {
            viewType = EViewType.Alert;
        }

        protected override void SetData(object data)
        {
            base.SetData(data);
            alertSetup = data as AlertSetup;
            if (alertSetup == null)
            {
                MyLogger.LogError("AlertView: AlertSetup is null. if override this method, remove base.SetData(setup).");
                return;
            }

            SetTile(alertSetup.title);
            SetMessage(alertSetup.message);
            SetOkButton(alertSetup.onOk);
            SetCancelButton(alertSetup.onCancel);
            SetTimeHide(alertSetup.timeHide);
        }


        protected virtual void SetTile(string _title) => SetTextComponent(title, _title, "Title");
        protected virtual void SetMessage(string _message) => SetTextComponent(message, _message, "Message");

        protected virtual void SetTextComponent(GameObject target, string text, string errorContext)
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
                MyLogger.LogError($"[AlertView] {errorContext}: No Text or TMP_Text component found.");
            }
        }

        protected virtual void SetOkButton(Action onOk)
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

        protected virtual void SetCancelButton(Action onCancel)
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
            _autoHideTween?.Kill();
            _autoHideTween = DOVirtual.DelayedCall(time, OnClose);
        }

        protected virtual void OnDisable()
        {
            _autoHideTween?.Kill();
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
            _autoHideTween?.Kill();
            _autoHideTween = null;
            MyLogger.Log("AlertView: OnClose called. Time hide left " + timeHide);

            if (alertSetup?.usePool ?? false) Main.Pool.Despawn(this);
            else Destroy(gameObject);
        }
    }
}
