using System;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine;

namespace OSK
{
    public enum TransitionType
    {
        None,
        Fade,
        Scale,
        SlideRight,
        SlideLeft,
        SlideUp,
        SlideDown,
        Animation
    }

    [System.Serializable]
    public class TweenSettings
    {
        public TransitionType transition;

        [HideIf(nameof(transition), TransitionType.None),
         HideIf(nameof(transition), TransitionType.Animation)]
        public float time = 0.25f;
         
        [HideIf(nameof(transition), TransitionType.None),
         HideIf(nameof(useCustomCurve), true),
         HideIf(nameof(transition), TransitionType.Animation)]
        public bool useEase = false;

        [ShowIf(nameof(useEase)),
         HideIf(nameof(transition), TransitionType.Animation)]
        public Ease ease = Ease.OutQuad;

        [ShowIf(nameof(transition), TransitionType.Scale)]
        public Vector3 initScale;
        
        [HideIf(nameof(transition), TransitionType.None),
         HideIf(nameof(useEase), true)]
        public bool useCustomCurve = false;

        [ShowIf(nameof(useCustomCurve))]
        public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [ShowIf(nameof(transition), TransitionType.Animation)]
        public Animation animationClip;
    }


    [RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
    public class UITransition : MonoBehaviour
    {
        [Header("Content UI")] [SerializeField]
        private RectTransform contentUI;

        public bool runIgnoreTimeScale = true;
        [SerializeField] private TweenSettings _openingTweenSettings;
        [SerializeField] private TweenSettings _closingTweenSettings;
        [Space(10)] private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;

        [Button]
        public void AutoRefContent()
        {
            if (transform.childCount > 0)
            {
                if(transform.Find("Container") != null)
                    contentUI = transform.Find("Container").GetComponent<RectTransform>();
                else
                    contentUI = transform.GetChild(0).GetComponent<RectTransform>();
            }
        }

        [Button]
        private void AddImageBlackFade()
        {
            var image = gameObject.GetOrAdd<UnityEngine.UI.Image>();
            image.color = new Color(0, 0, 0, 0.9f);
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.sizeDelta = Vector2.zero;
        }

        // Property to return either contentUI or _rectTransform
        private RectTransform TargetRectTransform => contentUI != null ? contentUI : _rectTransform;

        public void Initialize()
        {
            DOTween.Init();
            if(contentUI == null)
            {
                Logg.Log("contentUI not set, using RectTransform instead => " + gameObject.name, Color.red);
            }
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();
        }

        public void OpenTrans(Action onComplete)
        {
            ResetTransitionState();

            if (_openingTweenSettings.transition == TransitionType.None)
            {
                onComplete?.Invoke();
                return;
            }

            Tween tween = null;
            switch (_openingTweenSettings.transition)
            {
                case TransitionType.Fade:
                    if (_canvasGroup == null)
                    {
                        Logg.Log("_canvasGroup not add to component => " + gameObject.name);
                        _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                        break;
                    } 
                    _canvasGroup.alpha = 0;
                    tween = _canvasGroup.DOFade(1, _openingTweenSettings.time);
                    break;

                case TransitionType.Scale:
                    TargetRectTransform.localScale = _openingTweenSettings.initScale;
                    tween = TargetRectTransform.DOScale(Vector3.one, _openingTweenSettings.time);
                    break;

                case TransitionType.SlideRight:
                    TargetRectTransform.anchoredPosition = new Vector2(-TargetRectTransform.rect.width, 0);
                    tween = TargetRectTransform.DOAnchorPosX(0, _openingTweenSettings.time);
                    break;

                case TransitionType.SlideLeft:
                    TargetRectTransform.anchoredPosition = new Vector2(TargetRectTransform.rect.width, 0);
                    tween = TargetRectTransform.DOAnchorPosX(0, _openingTweenSettings.time);
                    break;

                case TransitionType.SlideUp:
                    TargetRectTransform.anchoredPosition = new Vector2(0, -TargetRectTransform.rect.height);
                    tween = TargetRectTransform.DOAnchorPosY(0, _openingTweenSettings.time);
                    break;
                case TransitionType.SlideDown:
                    TargetRectTransform.anchoredPosition = new Vector2(0, TargetRectTransform.rect.height);
                    tween = TargetRectTransform.DOAnchorPosY(0, _openingTweenSettings.time);
                    break;
                case TransitionType.Animation:
                    _openingTweenSettings.animationClip?.Play();
                    break;
            }

            OnCompletedTween(_openingTweenSettings, tween, onComplete, true);
        }

        public void CloseTrans(Action onComplete)
        {
            ResetTransitionState();

            if (_closingTweenSettings.transition == TransitionType.None)
            {
                onComplete?.Invoke();
                return;
            }

            Tween tween = null;
            switch (_closingTweenSettings.transition)
            {
                case TransitionType.Fade:
                    if (_canvasGroup == null)
                    {
                        Logg.Log("_canvasGroup not add to component => " + gameObject.name);
                        _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                        break;
                    } 
                    tween = _canvasGroup.DOFade(0, _closingTweenSettings.time);
                    break;

                case TransitionType.Scale:
                    tween = TargetRectTransform.DOScale(Vector3.zero, _closingTweenSettings.time);
                    break;

                case TransitionType.SlideRight:
                    tween = TargetRectTransform.DOAnchorPosX(TargetRectTransform.rect.width,
                        _closingTweenSettings.time);
                    break;

                case TransitionType.SlideLeft:
                    tween = TargetRectTransform.DOAnchorPosX(-TargetRectTransform.rect.width,
                        _closingTweenSettings.time);
                    break;

                case TransitionType.SlideUp:
                    tween = TargetRectTransform.DOAnchorPosY(TargetRectTransform.rect.height,
                        _closingTweenSettings.time);
                    break;

                case TransitionType.SlideDown:
                    tween = TargetRectTransform.DOAnchorPosY(-TargetRectTransform.rect.height,
                        _closingTweenSettings.time);
                    break;
                case TransitionType.Animation:
                    _closingTweenSettings.animationClip?.Play();
                    break;
            }
            OnCompletedTween(_closingTweenSettings, tween, onComplete, false);
        }


        private void OnCompletedTween(TweenSettings tweenSettings, Tween tween, Action onComplete, bool isOpen)
        {
            if (tweenSettings.transition == TransitionType.Animation)
            {
                if (tweenSettings.animationClip != null)
                {
                    float clipLength = tweenSettings.animationClip.clip.length;
                    DOVirtual.DelayedCall(clipLength, () => { onComplete?.Invoke(); });
                }
            }
            else
            {
                if (tween != null)
                {
                    ApplyTween(tween, isOpen);
                    tween.SetUpdate(runIgnoreTimeScale); 
                    tween.OnComplete(() =>
                    {
                        ResetTransitionState();
                        onComplete?.Invoke();
                    });
                }
                else
                {
                    ResetTransitionState();
                    onComplete?.Invoke();
                }
            }
        }

        private void ApplyTween(Tween tween, bool isOpen)
        {
            if (isOpen)
            {
                if (_openingTweenSettings.useEase)
                    tween.SetEase(_openingTweenSettings.ease);
                else if (_openingTweenSettings.useCustomCurve)
                    tween.SetEase(_openingTweenSettings.curve);
                else
                    tween.SetEase(Ease.Linear);
            }
            else
            {
                if (_closingTweenSettings.useEase)
                    tween.SetEase(_closingTweenSettings.ease);
                else if (_closingTweenSettings.useCustomCurve)
                    tween.SetEase(_closingTweenSettings.curve);
                else
                    tween.SetEase(Ease.Linear);
            }
        }


        private void ResetTransitionState()
        {
            DOTween.Kill(TargetRectTransform);
            DOTween.Kill(_canvasGroup);

            _canvasGroup.alpha = 1;
            TargetRectTransform.localScale = Vector3.one;
            TargetRectTransform.anchoredPosition = Vector2.zero;
        }

        public void AnyClose(Action onComplete)
        {
            DOTween.KillAll();
            ResetTransitionState();
            onComplete?.Invoke();
        }
    }
}