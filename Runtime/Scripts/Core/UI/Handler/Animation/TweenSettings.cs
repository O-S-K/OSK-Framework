using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OSK
{
    [System.Flags]
    public enum TransitionType
    {
        None      = 0,
        Fade      = 1 << 0,
        Scale     = 1 << 1,
        Animation = 1 << 2,
        Slide     = 1 << 3, // flag chung để bật chế độ Slide
    }

    public enum SlideType
    {
        SlideRight,
        SlideLeft,
        SlideUp,
        SlideDown
    }

    [Serializable]
    public class TweenSettings
    {
        [TableColumnWidth(120, Resizable = false)]
        [EnumToggleButtons]
        public TransitionType transition;

        [ShowIf(nameof(HasSlide))]
        [LabelText("Slide Direction")]
        public SlideType slideType;
        
        [ShowIf(nameof(HasSlide))]
        [Range(0f, 2f)]
        public float slideDistanceFactor = 1f;

        [Range(0f, 10f)]
        [LabelText("Duration")]
        public float duration = 0.25f;

        [BoxGroup("Ease Settings")]
        [HideIf(nameof(IsNone))]
        [HideIf(nameof(IsAnimation))]
        [LabelText("Use Ease")]
        public bool useEase = true;

        [BoxGroup("Ease Settings")]
        [HideIf(nameof(IsAnimation))]
        [LabelText("Use Ease")]
        [EnableIf(nameof(useEase))]
        public Ease ease = Ease.OutQuad;

        [BoxGroup("Ease Settings")]
        [HideIf(nameof(IsAnimation))]
        [DisableIf(nameof(useEase))]
        public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [ShowIf(nameof(IsScale))]
        [LabelText("Init Scale")]
        public Vector3 initScale = Vector3.one;

        [ShowIf(nameof(IsAnimation))]
        [LabelText("Animation")]
        public Animation animationComponent;

        // Helpers
        private bool HasSlide => transition.HasFlag(TransitionType.Slide);
        private bool IsNone => transition == TransitionType.None;
        private bool IsScale => transition.HasFlag(TransitionType.Scale);
        private bool IsAnimation => transition.HasFlag(TransitionType.Animation); 
        
        public TweenSettings(
            TransitionType transition = TransitionType.Fade,
            float duration = 0.25f,
            bool useEase = true,
            Ease ease = Ease.OutQuad,
            Vector3? initScale = null,
            SlideType slideType = SlideType.SlideRight,
            float slideDistanceFactor = 1f,
            Animation animationComponent = null)
        {
            this.transition = transition;
            this.duration = duration;
            this.useEase = useEase;
            this.ease = ease;
            this.initScale = initScale ?? Vector3.one;
            this.slideType = slideType;
            this.slideDistanceFactor = slideDistanceFactor;
            this.animationComponent = animationComponent;
        }
    }
}