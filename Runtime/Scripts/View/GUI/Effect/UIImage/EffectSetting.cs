using System.Collections.Generic;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine;

namespace OSK
{
    public enum TypeMove
    {
        Straight,
        Beziers,
        CatmullRom,
        Path,
        DoJump,
        Around,
        Sin,
    }

    public enum TypeAnimation
    {
        Ease,
        Curve,
    }


    [System.Serializable]
    public class EffectSetting
    {
        [Header("Setup")]
        public string name;
        //public int id { get; set; }
        //public GameObject icon;
        [Min(1)]
        public int numberOfEffects = 10;
        public bool isDrop = true;

        [Space(10)]

        #region Drop

        [Header("Drop")]
        [ShowIf(nameof(isDrop), true)]
        public float sphereRadius = 1;

        [ShowIf(nameof(isDrop), true)]
        public MinMaxFloat delayDrop;

        [ShowIf(nameof(isDrop), true)]
        public MinMaxFloat timeDrop;
        
        
        // scale
        public bool isScaleDrop = false;
        public float scaleDropStart = 1;
        public float scaleDropEnd = 1;

        [ShowIf(nameof(isDrop), true)] 
        public TypeAnimation typeAnimationDrop = TypeAnimation.Ease;

        private bool isShowEase => typeAnimationDrop == TypeAnimation.Ease && isDrop;
        [ShowIf(nameof(isShowEase), true)]
        public Ease easeDrop = Ease.Linear;

        private bool isShowCurve => typeAnimationDrop == TypeAnimation.Curve && isDrop;
        [ShowIf(nameof(isShowCurve), true)]
        public AnimationCurve curveDrop;

        #endregion

        [Space(10)]

        #region Move

        [Header("Move")]
        public TypeMove typeMove = TypeMove.Straight;
        
        public List<Vector3> paths;
        public TypeAnimation typeAnimationMove = TypeAnimation.Ease;
 
        public Ease easeMove = Ease.Linear;
        public AnimationCurve curveMove;
        
        // scale
        public bool isScaleMove = false;
        public float scaleMoveStart = 1;
        public float scaleMoveTarget = 1;
        
        // Jump
        public MinMaxFloat jumpPower;
        [Min(1)]
        public int jumpNumber;
 
        public MinMaxFloat midPointOffsetX;
        public MinMaxFloat midPointOffsetZ;
        
        // Sin
        [Min(4)]
        public int pointsCount = 10;

        public MinMaxFloat height;
        public MinMaxFloat timeMove;
        public MinMaxFloat delayMove;
        public System.Action OnCompleted;

        #endregion

        public Vector3 pointSpawn { get; set; }
        public Vector3 pointTarget { get; set; }
        
        public EffectSetting Clone()
        {
            return (EffectSetting) MemberwiseClone();
        }
    }
}