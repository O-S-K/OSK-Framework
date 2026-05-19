using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace OSK
{
    public class HoleView : View, ICanvasRaycastFilter
    {
        [Header("References")]
        [SerializeField] private Image _maskImage;
        [SerializeField] private RectTransform _pointer; 
        
        [Header("Settings")]
        [SerializeField] private Vector2 _padding = new Vector2(20, 20);
        [SerializeField] private float _cornerRadius = 25f;
        [SerializeField] private float _showPointerThreshold = 2f;
        [SerializeField] private float _clickCooldown = 0.3f; 

        private Material _maskMat;
        private List<RectTransform> _uiTargets = new List<RectTransform>();
        private List<Transform> _worldTargets = new List<Transform>();
        private RectTransform _myRectTransform;
        private Canvas _myCanvas;
        
        private float _lastStepTime = 0f;
        private int _totalCount = 0;
        private bool _freezeSize = true;

        private EHoleViewEase _currentEase = EHoleViewEase.SmoothDamp;
        private float _duration = 0.3f;
        private int _pointerIndex = 0;
        private Vector2 _currentPointerOffset = new Vector2(50, -50);
        private bool _shouldShowPointer = true;
        private bool _isFirstOpen = true;

        private Vector2[] _startCenters = new Vector2[8];
        private Vector2[] _startSizes = new Vector2[8];
        private Vector2[] _currentCenters = new Vector2[8];
        private Vector2[] _currentSizes = new Vector2[8];
        private Vector2[] _fixedTargetSizes = new Vector2[8]; // Mảng lưu size đã chốt
        private Vector2[] _velCenters = new Vector2[8];
        private Vector2[] _velSizes = new Vector2[8];
        
        private Vector4[] _shaderCenters = new Vector4[8];
        private Vector4[] _shaderSizes = new Vector4[8];
        private float[] _shaderRadii = new float[8];
        private float _animTimer = 0f;
        
        public Vector4 GetHoleCenter(int index) => _shaderCenters[index];
        public Vector4 GetHoleSize(int index) => _shaderSizes[index];
        public float GetHoleRadius(int index) => _shaderRadii[index];
        public int GetHoleCount() => _totalCount;

        protected override void OnInit()
        {
            viewType = EViewType.Overlay;
            _myRectTransform = GetComponent<RectTransform>();
            _myCanvas = GetComponentInParent<Canvas>();
            if (_maskImage != null)
            {
                _maskMat = Instantiate(_maskImage.material);
                _maskImage.material = _maskMat;
            }
            _isFirstOpen = true;
        }

        protected override void SetData(object data = null)
        {
            if (data == null || false) return;
            if (data is HoleViewData holeData) ApplyData(holeData);
        }

        private void Update()
        {
            _totalCount = Mathf.Min(_uiTargets.Count + _worldTargets.Count, 8);
            if (_totalCount == 0 || _maskMat == null) return;

            UpdateHolePositions();
            //HandleClickDetection();
        }

        private void UpdateHolePositions()
        {
            _animTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_animTimer / _duration);
            bool targetReached = t >= 1f;

            Camera uiCam = (_myCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _myCanvas.worldCamera;
            int uiCount = _uiTargets.Count;

            for (int i = 0; i < _totalCount; i++)
            {
                Vector2 targetCenter, targetSize;
                
                // Luôn cập nhật vị trí Center vì mục tiêu có thể di chuyển (Move)
                if (i < uiCount)
                {
                    var rt = _uiTargets[i];
                    Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(uiCam, rt.position);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(_myRectTransform, screenPos, uiCam, out targetCenter);
                    targetSize = _freezeSize ? _fixedTargetSizes[i] : new Vector2(rt.rect.width + _padding.x, rt.rect.height + _padding.y);
                }
                else
                {
                    CalculateWorldToLocalBounds(_worldTargets[i - uiCount], uiCam, out targetCenter, out targetSize);
                    if (_freezeSize) targetSize = _fixedTargetSizes[i];
                }

                if (_currentEase == EHoleViewEase.SmoothDamp)
                {
                    _currentCenters[i] = Vector2.SmoothDamp(_currentCenters[i], targetCenter, ref _velCenters[i], _duration * 0.5f);
                    _currentSizes[i] = Vector2.SmoothDamp(_currentSizes[i], targetSize, ref _velSizes[i], _duration * 0.5f);
                    if (i == _pointerIndex) targetReached = Vector2.Distance(_currentCenters[i], targetCenter) < _showPointerThreshold;
                }
                else if (_currentEase == EHoleViewEase.Instant)
                {
                    _currentCenters[i] = targetCenter;
                    _currentSizes[i] = targetSize;
                    targetReached = true;
                }
                else
                {
                    float easedT = EvaluateEase(_currentEase, t);
                    _currentCenters[i] = Vector2.LerpUnclamped(_startCenters[i], targetCenter, easedT);
                    _currentSizes[i] = Vector2.LerpUnclamped(_startSizes[i], targetSize, easedT);
                }

                _shaderCenters[i] = new Vector4(_currentCenters[i].x, _currentCenters[i].y, 0, 0);
                _shaderSizes[i] = new Vector4(_currentSizes[i].x, _currentSizes[i].y, 0, 0);
                _shaderRadii[i] = _cornerRadius;
            }

            _maskMat.SetVectorArray("_HoleCenters", _shaderCenters);
            _maskMat.SetVectorArray("_HoleSizes", _shaderSizes);
            _maskMat.SetFloatArray("_HoleRadii", _shaderRadii);
            _maskMat.SetInt("_HoleCount", _totalCount);

            if (_pointer != null)
            {
                if (_shouldShowPointer && targetReached && _pointerIndex < _totalCount)
                {
                    if (!_pointer.gameObject.activeSelf) _pointer.gameObject.SetActive(true);
                    _pointer.anchoredPosition = _currentCenters[_pointerIndex] + _currentPointerOffset;
                }
                else if (_pointer.gameObject.activeSelf) _pointer.gameObject.SetActive(false);
            }
        }

        private void HandleClickDetection()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (Time.time - _lastStepTime < _clickCooldown) return;

                Vector2 mousePos = Input.mousePosition;
                Camera uiCam = (_myCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _myCanvas.worldCamera;
                Vector2 localPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_myRectTransform, mousePos, uiCam, out localPos);

                for (int i = 0; i < _totalCount; i++)
                {
                    float halfW = _currentSizes[i].x * 0.5f;
                    float halfH = _currentSizes[i].y * 0.5f;
                    if (localPos.x >= _currentCenters[i].x - halfW && localPos.x <= _currentCenters[i].x + halfW &&
                        localPos.y >= _currentCenters[i].y - halfH && localPos.y <= _currentCenters[i].y + halfH)
                    {
                        _lastStepTime = Time.time;
                        //_onTargetClick?.Invoke();
                        break;
                    }
                }
            }
        }

        private void CalculateWorldToLocalBounds(Transform target, Camera uiCam, out Vector2 center, out Vector2 size)
        {
            Camera worldCam = Camera.main;
            if (worldCam == null || target == null) { center = Vector2.zero; size = Vector2.zero; return; }

            var renderer = target.GetComponentInChildren<Renderer>();
            if (renderer == null)
            {
                Vector2 screenPos = worldCam.WorldToScreenPoint(target.position);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_myRectTransform, screenPos, uiCam, out center);
                size = Vector2.one * 100f;
                return;
            }

            Bounds b = renderer.bounds;
            Vector3[] corners = new Vector3[8];
            corners[0] = b.min; corners[1] = b.max;
            corners[2] = new Vector3(b.min.x, b.min.y, b.max.z);
            corners[3] = new Vector3(b.min.x, b.max.y, b.min.z);
            corners[4] = new Vector3(b.max.x, b.min.y, b.min.z);
            corners[5] = new Vector3(b.min.x, b.max.y, b.max.z);
            corners[6] = new Vector3(b.max.x, b.min.y, b.max.z);
            corners[7] = new Vector3(b.max.x, b.max.y, b.min.z);

            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            bool anyInView = false;
            foreach (var corner in corners)
            {
                Vector3 screenPoint = worldCam.WorldToScreenPoint(corner);
                if (screenPoint.z > 0)
                {
                    anyInView = true;
                    Vector2 lp;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(_myRectTransform, screenPoint, uiCam, out lp);
                    minX = Mathf.Min(minX, lp.x); minY = Mathf.Min(minY, lp.y);
                    maxX = Mathf.Max(maxX, lp.x); maxY = Mathf.Max(maxY, lp.y);
                }
            }

            if (!anyInView) { center = new Vector2(5000, 5000); size = Vector2.zero; return; }
            center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
            size = new Vector2(maxX - minX + _padding.x, maxY - minY + _padding.y);
        }

        public void ApplyData(HoleViewData data)
        {
            _lastStepTime = Time.time;
            _freezeSize = data.freezeSize;

            Camera uiCam = (_myCanvas != null && _myCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? _myCanvas.worldCamera : null;

            for(int i=0; i<8; i++) { 
                _startCenters[i] = _currentCenters[i]; _startSizes[i] = _currentSizes[i]; 
                _velCenters[i] = Vector2.zero; _velSizes[i] = Vector2.zero; 
            }

            _uiTargets.Clear(); if (data.uiTargets != null) _uiTargets.AddRange(data.uiTargets);
            _worldTargets.Clear(); if (data.worldTargets != null) _worldTargets.AddRange(data.worldTargets);
 
            int uiCount = _uiTargets.Count;
            for (int i = 0; i < Mathf.Min(_uiTargets.Count + _worldTargets.Count, 8); i++)
            {
                if (i < uiCount)
                    _fixedTargetSizes[i] = new Vector2(_uiTargets[i].rect.width + _padding.x, _uiTargets[i].rect.height + _padding.y);
                else
                {
                    Vector2 dummyCenter;
                    CalculateWorldToLocalBounds(_worldTargets[i - uiCount], uiCam, out dummyCenter, out _fixedTargetSizes[i]);
                }
            }

            _pointerIndex = data.pointerTargetIndex;
            _currentPointerOffset = data.pointerOffset;
            _shouldShowPointer = data.showPointer;
            _currentEase = data.easeType;
            _duration = data.duration;
            //_onTargetClick = data.onTargetClick;
            _animTimer = 0f;

            if (_isFirstOpen) { _currentEase = EHoleViewEase.Instant; _isFirstOpen = false; }
            if (_pointer != null) _pointer.gameObject.SetActive(false);
        }

        private float EvaluateEase(EHoleViewEase ease, float t)
        {
            switch (ease)
            {
                case EHoleViewEase.Linear: return t;
                case EHoleViewEase.OutSine: return Mathf.Sin(t * Mathf.PI * 0.5f);
                case EHoleViewEase.OutBack:
                    float s = 1.70158f; t -= 1.0f;
                    return t * t * ((s + 1) * t + s) + 1.0f;
                default: return t;
            }
        }

        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            int total = Mathf.Min(_uiTargets.Count + _worldTargets.Count, 8);
            if (total == 0) return true;
            Camera uiCam = (_myCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _myCanvas.worldCamera;
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_myRectTransform, sp, uiCam, out localPos);
            for (int i = 0; i < total; i++)
            {
                float halfW = _currentSizes[i].x * 0.5f;
                float halfH = _currentSizes[i].y * 0.5f;
                if (localPos.x >= _currentCenters[i].x - halfW && localPos.x <= _currentCenters[i].x + halfW &&
                    localPos.y >= _currentCenters[i].y - halfH && localPos.y <= _currentCenters[i].y + halfH) return false; 
            }
            return true;
        }
    }
}
