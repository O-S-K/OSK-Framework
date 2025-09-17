using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;

namespace OSK
{
    public class TransitionUI : View
    {
        [Header("Optional UI Bindings (Inspector)")]
        public TMP_Text loadingText;

        public string preFixLoading = "Loading ";
        public Image fillImage;

        [Header("Transition Settings")]
        public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public float timeTransition = 2f;
        public bool pauseTimeScale = true;

        private CancellationTokenSource _cts;

        public event Action<float> OnProgress;
        public event Action OnCompleted;

        public override void Open(object[] data = null)
        {
            base.Open(data);
            StartTransition();
        }

        private void StartTransition()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            RunTransitionAsync(_cts.Token).Forget();
        }

        private async UniTaskVoid RunTransitionAsync(CancellationToken token)
        {
            if (pauseTimeScale) Time.timeScale = 0;

            float elapsed = 0f;
            UpdateUI(0f);

            while (elapsed < timeTransition && !token.IsCancellationRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / timeTransition);
                float progress = easeCurve.Evaluate(t);

                UpdateUI(progress);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            if (!token.IsCancellationRequested)
            {
                UpdateUI(1f);
                OnCompleted?.Invoke();
            }

            if (pauseTimeScale) Time.timeScale = 1;
        }

        private void UpdateUI(float progress)
        {
            OnProgress?.Invoke(progress);
            if (fillImage) fillImage.fillAmount = progress;
            if (loadingText) loadingText.text = $"{preFixLoading + Mathf.RoundToInt(progress * 100)}%";
        }

        public override void Hide()
        {
            _cts?.Cancel();
            base.Hide();
        }
    }
}
