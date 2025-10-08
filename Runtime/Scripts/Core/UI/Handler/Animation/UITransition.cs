using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace OSK
{
    [RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
    public class UITransition : MonoBehaviour
    {
        [LabelText("If not set, will use Rect of this GO")] 
        [SerializeField] private RectTransform contentUI;

        public bool runIgnoreTimeScale = true;

        [TitleGroup("Open", "Open Settings", HorizontalLine = true)]
        [GUIColor(0.9f, .9f, 0.8f)]
        [InlineProperty, HideLabel]
        public TweenSettings _openingTweenSettings;

        [TitleGroup("Close", "Close Settings", HorizontalLine = true)]
        [GUIColor(0.8f, .9f, 0.8f)]
        [InlineProperty, HideLabel]
        public TweenSettings _closingTweenSettings;

        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;

        private RectTransform TargetRectTransform => contentUI != null ? contentUI : _rectTransform;

        public void Initialize()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();

            if (contentUI == null)
                OSKLogger.Log("UI", $"contentUI not set, using RectTransform instead => {gameObject.name}");
        }

        public UniTask OpenTrans() => PlayTransition(_openingTweenSettings, true);
        public UniTask CloseTrans() => PlayTransition(_closingTweenSettings, false);

        // --- Callback API ---
        public void OpenTrans(Action onComplete) => OpenTrans().ContinueWith(onComplete).Forget();
        public void CloseTrans(Action onComplete) => CloseTrans().ContinueWith(onComplete).Forget();
 
        public void AnyClose(Action onComplete)
        {
            DOTween.Kill(TargetRectTransform);
            DOTween.Kill(_canvasGroup);

            _canvasGroup.alpha = 0;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            
            TargetRectTransform.localScale = Vector3.one;
            TargetRectTransform.anchoredPosition = Vector2.zero;

            onComplete?.Invoke();
        }
        
        // Example: 
        /* open with Fade + Scale
        uiTransition.SetOpenSettings(
            TransitionType.Fade | TransitionType.Scale,
            0.3f,
            Ease.OutBack,
            new Vector3(0.8f, 0.8f, 1f)
        );

        // close with Slide Down
        uiTransition.SetCloseSettings(
            TransitionType.Slide,
            0.25f,
            Ease.InQuad,
            SlideType.SlideDown
        );*/
        
        public void SetOpenSettings(TweenSettings settings) => _openingTweenSettings = settings;
        public void SetCloseSettings(TweenSettings settings) => _closingTweenSettings = settings;

        private async UniTask PlayTransition(TweenSettings settings, bool isOpen)
        {
            if (settings.transition == TransitionType.None)
                return;

            // Reset state before open
            if (isOpen) ResetTransitionState();
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            
            Sequence seq = DOTween.Sequence();

            // Fade
            if (settings.transition.HasFlag(TransitionType.Fade))
            {
                if (isOpen) _canvasGroup.alpha = 0;
                float target = isOpen ? 1 : 0;
                seq.Join(_canvasGroup.DOFade(target, settings.duration));
            }

            // Scale
            if (settings.transition.HasFlag(TransitionType.Scale))
            {
                if (isOpen) TargetRectTransform.localScale = settings.initScale;
                Vector3 target = isOpen ? Vector3.one : Vector3.zero;
                seq.Join(TargetRectTransform.DOScale(target, settings.duration));
            }

            // Slide
            if (settings.transition.HasFlag(TransitionType.Slide))
            {
                switch (settings.slideType)
                {
                    case SlideType.SlideRight:
                        if (isOpen)
                            TargetRectTransform.anchoredPosition = new Vector2(-TargetRectTransform.rect.width * settings.slideDistanceFactor, 0);
                        seq.Join(TargetRectTransform.DOAnchorPosX(isOpen ? 0 : TargetRectTransform.rect.width * settings.slideDistanceFactor, settings.duration));
                        break;

                    case SlideType.SlideLeft:
                        if (isOpen)
                            TargetRectTransform.anchoredPosition = new Vector2(TargetRectTransform.rect.width * settings.slideDistanceFactor, 0);
                        seq.Join(TargetRectTransform.DOAnchorPosX(isOpen ? 0 : -TargetRectTransform.rect.width * settings.slideDistanceFactor, settings.duration));
                        break;

                    case SlideType.SlideUp:
                        if (isOpen)
                            TargetRectTransform.anchoredPosition = new Vector2(0, -TargetRectTransform.rect.height * settings.slideDistanceFactor);
                        seq.Join(TargetRectTransform.DOAnchorPosY(isOpen ? 0 : TargetRectTransform.rect.height * settings.slideDistanceFactor, settings.duration));
                        break;

                    case SlideType.SlideDown:
                        if (isOpen)
                            TargetRectTransform.anchoredPosition = new Vector2(0, TargetRectTransform.rect.height * settings.slideDistanceFactor);
                        seq.Join(TargetRectTransform.DOAnchorPosY(isOpen ? 0 : -TargetRectTransform.rect.height * settings.slideDistanceFactor, settings.duration));
                        break;
                }
            }

            // Animation
            if (settings.transition.HasFlag(TransitionType.Animation) && settings.animationComponent != null)
            {
                var clip = settings.animationComponent.clip;
                if (clip != null)
                {
                    settings.animationComponent.Play();
                    await UniTask.Delay(TimeSpan.FromSeconds(clip.length), ignoreTimeScale: runIgnoreTimeScale);
                }
                return;
            }

            if (seq.IsActive())
            {
                ApplyTween(seq, settings);
                seq.SetUpdate(runIgnoreTimeScale);

                await seq.AsyncWaitForCompletion();

                if (isOpen)
                {
                    _canvasGroup.interactable = true;
                    _canvasGroup.blocksRaycasts = true;
                }
                else
                {
                    ResetTransitionState();
                }
            }
        }

        private void ApplyTween(Tween tween, TweenSettings settings)
        {
            if (settings.useEase)
                tween.SetEase(settings.ease);
            else if (settings.curve != null)
                tween.SetEase(settings.curve);
            else
                tween.SetEase(Ease.Linear);
        }

        private void ResetTransitionState()
        {
            DOTween.Kill(TargetRectTransform);
            DOTween.Kill(_canvasGroup);

            _canvasGroup.alpha = 1;
            TargetRectTransform.localScale = Vector3.one;
            TargetRectTransform.anchoredPosition = Vector2.zero;
        }
    }
}
