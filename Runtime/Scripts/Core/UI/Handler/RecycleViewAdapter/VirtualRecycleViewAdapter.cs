using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace OSK
{
    public enum JumpPosition
    {
        Top = 0,
        Center = 1,
        Bottom = 2
    }

    public enum ScrollDirection
    {
        Horizontal,
        Vertical
    }

    public enum SnapTargetType
    {
        Nearest,
        Previous,
        Next
    }


    [RequireComponent(typeof(ScrollRect))]
    [RequireComponent(typeof(ScrollDragState))]
    [AddComponentMenu("OSK-Framework/UI/ScrollView Adapter")]
    public class VirtualRecycleViewAdapter<TModel, TView> : MonoBehaviour where TView : Component, IRecyclerItem<TModel>
    {
        [Title(" ------ Virtual Recycle View Adapter Settings ------ ", HorizontalLine = true)]
        [InfoBox("Not use Content Size Fitter and Layout Groups on Content RectTransform. " +
                 "Item size and positions are managed by the adapter.", InfoMessageType.Warning)]
        [Space(20)]
        [LabelText("Item Prefab")]
        [HorizontalGroup("Left", Width = 300)]
        [InlineEditor(InlineEditorModes.GUIOnly)]
        [HideLabel]
        [AssetsOnly]
        public TView ItemPrefab;

        public enum SizeMode
        {
            Manual,      /// Tự nhập tay cả hai
            UsePrefabX,  /// Lấy X từ Prefab, Y nhập tay
            UsePrefabY,  /// Lấy Y từ Prefab, X nhập tay
            UsePrefabXY  /// Lấy cả hai từ Prefab
        }


        [Title("Size Settings")]
        public SizeMode SizeOption = SizeMode.Manual;

        [HorizontalGroup("SizeRow")]
        [ShowIf(nameof(ShowWidthField))]
        [LabelText("Width")]
        public float ItemWidth = 200f;

        [HorizontalGroup("SizeRow")]
        [ShowIf(nameof(ShowHeightField))]
        [LabelText("Height")]
        public float ItemHeight = 100f;

        private bool ShowWidthField => SizeOption == SizeMode.Manual || SizeOption == SizeMode.UsePrefabY;
        private bool ShowHeightField => SizeOption == SizeMode.Manual || SizeOption == SizeMode.UsePrefabX;

        // ---------- Buffer / Prewarm on one line ----------
        [HorizontalGroup("RowA", MarginLeft = 6, PaddingRight = 8)]
        [LabelWidth(90)] 
        [LabelText("Buffer"), MinValue(0)]
        //buffer là số lượng item thêm vào trước và sau vùng nhìn thấy để tránh nhấp nháy khi cuộn nhanh
        public int Buffer = 2;

        [HorizontalGroup("RowA")] [LabelText("Prewarm"), MinValue(0)]
        public int Prewarm = 5;


        // ---------- Direction (single compact row) ----------
        [HorizontalGroup("RowB", MarginLeft = 6)] [LabelWidth(90)] [LabelText("Scroll Direction")]
        public ScrollDirection Direction = ScrollDirection.Vertical;


        // ---------- Spacing X / Y on one line ----------
        [HorizontalGroup("RowC", MarginLeft = 6)] [LabelWidth(90)] [LabelText("Spacing X")]
        public float SpacingX = 0f;

        [HorizontalGroup("RowC")] [LabelText("Spacing Y")]
        public float SpacingY = 0f;


        [Title(" ------ Jump Settings ------ ", HorizontalLine = true)]
        [HorizontalGroup("RowD", MarginLeft = 6)]
        [LabelWidth(90)]
        [LabelText("Disable Input")]
        public bool DisableInputDuringJump = true;

        [HorizontalGroup("RowD")] [LabelText("Use Unscaled")]
        public bool JumpUseUnscaledTime = true;


        [Title(" ------ Infinite Settings ------ ", HorizontalLine = true)]
        public bool IsInfinite = false; // Bật cái này lên để loop

        [Title(" ------ Snap Settings ------ ", HorizontalLine = true)] [LabelWidth(120)]
        public bool EnableSnap = false;

        [ShowIf(nameof(EnableSnap))]
        [LabelWidth(120)]
        [Tooltip("Tốc độ Snap (đơn vị item/giây). Giá trị cao hơn = Snap nhanh hơn.")]
        public float SnapSpeed = 0.25f;

        [ShowIf(nameof(EnableSnap))] [LabelWidth(120)] [Tooltip("Đích đến khi cuộn dừng lại (Nearest, Previous/Next).")]
        public SnapTargetType SnapTarget = SnapTargetType.Nearest; // NEW: Snap Target

        [ShowIf(nameof(EnableSnap))]
        [LabelWidth(120)]
        [Tooltip("Tốc độ tối thiểu để cuộn được xem là Flick/Swipe, vượt qua SnapTarget=Nearest.")]
        public float ThresholdSpeedToSnap = 1000f;

        // data sources (mutually exclusive modes)
        private ObservableCollection<TModel> _obsSource = null;
        private IList<TModel> _listSource = null; // for List<TModel> or TModel[]
        private IList<TModel> _dictValues = null; // for IDictionary mode (values in chosen order)
        private IDictionary _rawDict = null; // original dictionary (non-generic reference)
        private IList _dictKeyOrder = null; // ordered list of keys for dictionary mode (optional)

        // internals
        private PoolRecycleView<TView> _poolRecycleView;
        private readonly Dictionary<int, TView> _active = new Dictionary<int, TView>();
        private int _totalCount = 0;
        private ScrollDragState _scrollDragState;
        private Tweener _scrollTweener;
        private ScrollRect _scrollRect;
        private bool _isDragging = false;

        public ScrollRect ScrollRect
        {
            get
            {
                if (_scrollRect == null) _scrollRect = GetComponent<ScrollRect>();
                return _scrollRect;
            }
        }

        public RectTransform Viewport => ScrollRect.viewport;
        public RectTransform Content => ScrollRect.content;


        protected virtual void Awake()
        {
            _scrollDragState = GetComponent<ScrollDragState>();
            UpdateSizesFromPrefab();
            if (ItemPrefab != null) _poolRecycleView = new PoolRecycleView<TView>(ItemPrefab, Content);
            enabled = true;
        }
        
        private void UpdateSizesFromPrefab()
        {
            if (ItemPrefab == null || SizeOption == SizeMode.Manual) return;

            RectTransform prefabRect = ItemPrefab.GetComponent<RectTransform>();
            if (prefabRect == null) return;

            switch (SizeOption)
            {
                case SizeMode.UsePrefabX:
                    ItemWidth = prefabRect.rect.width;
                    break;
            
                case SizeMode.UsePrefabY:
                    ItemHeight = prefabRect.rect.height;
                    break;
            
                case SizeMode.UsePrefabXY:
                    ItemWidth = prefabRect.rect.width;
                    ItemHeight = prefabRect.rect.height;
                    break;
            }
        }


        protected virtual void Start()
        {
            PrewarmPool();
            ScrollRect.onValueChanged.AddListener(OnScroll);
            ScrollRect.movementType =
                IsInfinite ? ScrollRect.MovementType.Unrestricted : ScrollRect.MovementType.Clamped;
            UpdateContentSize();
            Refresh();
        }

        protected virtual void LateUpdate()
        {
            ClampContentPosition();

            if (EnableSnap && !_scrollDragState.IsDragging && _scrollTweener == null &&
                ScrollRect.velocity.magnitude < 100f)
            {
                TrySnapToCenter();
            }
        }

        private void TrySnapToCenter()
        {
            // Nếu đang có tween chạy thì thôi, tránh xung đột
            if (_scrollTweener != null && _scrollTweener.IsActive()) return;
            if (GetCount() == 0 || Viewport == null) return;

            float viewportSize = Direction == ScrollDirection.Vertical ? Viewport.rect.height : Viewport.rect.width;
            float itemFull = ItemFullSize();

            // Lấy vị trí cuộn hiện tại (đã chuẩn hóa dấu)
            float currentScrollPos = Direction == ScrollDirection.Vertical
                ? Content.anchoredPosition.y
                : -Content.anchoredPosition.x;

            float releaseSpeed = ScrollRect.velocity.magnitude;

            float scrollCenterOffset = viewportSize / 2f - itemFull / 2f;
            float indexAtCenterFloat = (currentScrollPos + scrollCenterOffset) / itemFull;
            int nearestIndex = Mathf.RoundToInt(indexAtCenterFloat);
            int targetIndex = nearestIndex;

            if (!IsInfinite)
            {
                int maxIndex = GetCount() - 1;
                nearestIndex = Mathf.Clamp(nearestIndex, 0, maxIndex);
                targetIndex = nearestIndex;
            }

            if (releaseSpeed > ThresholdSpeedToSnap)
            {
                bool isHorizontal = Direction == ScrollDirection.Horizontal;
                bool isPositiveVelocity = isHorizontal ? ScrollRect.velocity.x > 0 : ScrollRect.velocity.y > 0;

                if (SnapTarget == SnapTargetType.Previous ||
                    (SnapTarget == SnapTargetType.Nearest && isPositiveVelocity))
                {
                    targetIndex = nearestIndex - 1;
                }
                else if (SnapTarget == SnapTargetType.Next ||
                         (SnapTarget == SnapTargetType.Nearest && !isPositiveVelocity))
                {
                    targetIndex = nearestIndex + 1;
                }
            }

            if (!IsInfinite)
            {
                targetIndex = Mathf.Clamp(targetIndex, 0, GetCount() - 1);
            }

            float targetScroll = (targetIndex * itemFull) - scrollCenterOffset;

            if (!IsInfinite)
            {
                float contentLen = Direction == ScrollDirection.Vertical ? Content.rect.height : Content.rect.width;
                float maxScroll = Mathf.Max(0f, contentLen - viewportSize);
                targetScroll = Mathf.Clamp(targetScroll, 0f, maxScroll);
            }

            float snapDistance = Mathf.Abs(currentScrollPos - targetScroll);

            if (snapDistance > 1f)
            {
                float duration = Mathf.Clamp(snapDistance / (itemFull * SnapSpeed), 0.1f, 0.5f);
                ScrollRect.velocity = Vector2.zero;
                if (_scrollTweener != null) _scrollTweener.Kill();

                if (Direction == ScrollDirection.Vertical)
                {
                    _scrollTweener = DOTween.To(
                            () => Content.anchoredPosition.y,
                            y =>
                            {
                                Content.anchoredPosition = new Vector2(Content.anchoredPosition.x, y);
                                ClampContentPosition();
                                Refresh();
                            },
                            targetScroll,
                            duration)
                        .SetEase(Ease.OutSine)
                        .OnComplete(() => { _scrollTweener = null; });
                }
                else // Horizontal
                {
                    _scrollTweener = DOTween.To(
                            () => -Content.anchoredPosition.x,
                            x =>
                            {
                                Content.anchoredPosition =
                                    new Vector2(-x, Content.anchoredPosition.y);
                                ClampContentPosition();
                                Refresh();
                            },
                            targetScroll,
                            duration)
                        .SetEase(Ease.OutSine)
                        .OnComplete(() => { _scrollTweener = null; });
                }
            }
        }

        // ---------- Public: support ObservableCollection (existing) ----------
        public void SetSource(ObservableCollection<TModel> source)
        {
            // clear other modes
            ClearListMode();
            ClearDictMode();

            if (_obsSource != null) UnbindSource();
            _obsSource = source;
            if (_obsSource != null) BindSource();
            _totalCount = GetCount();
            UpdateContentSize();
            Refresh();
        }

        private void BindSource()
        {
            if (_obsSource == null) return;
            _obsSource.OnAdd += OnAdd;
            _obsSource.OnRemove += OnRemove;
            _obsSource.OnReplace += OnReplace;
            _obsSource.OnReset += OnReset;
        }

        private void UnbindSource()
        {
            if (_obsSource == null) return;
            _obsSource.OnAdd -= OnAdd;
            _obsSource.OnRemove -= OnRemove;
            _obsSource.OnReplace -= OnReplace;
            _obsSource.OnReset -= OnReset;
            _obsSource = null;
        }

        // ---------- New: support IList<TModel> / arrays ----------
        /// <summary>Use a plain IList or array as data source (non-observable). Adapter will not auto-listen to changes.</summary>
        public void SetData(IList<TModel> list)
        {
            // clear observable/dict modes
            if (_obsSource != null)
            {
                UnbindSource();
                _obsSource = null;
            }

            ClearDictMode();

            _listSource = list;
            _totalCount = GetCount();
            UpdateContentSize();
            Refresh();
        }

        public void ClearListMode()
        {
            _listSource = null;
        }

        // ---------- New: support IDictionary<TKey,TModel> ----------
        /// <summary>
        /// Use a dictionary as a data source. If keyOrder==null, the dictionary's Keys enumeration order will be used (not guaranteed).
        /// You can provide keyOrder to guarantee ordering.
        /// </summary>
        public void SetData<TKey>(IDictionary<TKey, TModel> dict, IList<TKey> keyOrder = null)
        {
            if (dict == null)
            {
                ClearDictMode();
                _totalCount = GetCount();
                UpdateContentSize();
                Refresh();
                return;
            }

            // clear observable/list modes
            if (_obsSource != null)
            {
                UnbindSource();
                _obsSource = null;
            }

            ClearListMode();

            // store original dictionary in non-generic reference for internal use
            _rawDict = dict as IDictionary;

            // build ordered values list
            if (keyOrder != null)
            {
                // use provided order
                var vals = new List<TModel>(keyOrder.Count);
                foreach (var k in keyOrder)
                {
                    if (dict.TryGetValue(k, out var v)) vals.Add(v);
                    else vals.Add(default);
                }

                _dictValues = vals;
                // store keys as non-generic IList for lookup
                _dictKeyOrder = new List<TKey>(keyOrder) as IList;
            }
            else
            {
                // fallback: use dict.Keys order
                _dictKeyOrder = new List<TKey>(dict.Keys) as IList;
                var vals = new List<TModel>(_dictKeyOrder.Count);
                foreach (TKey k in _dictKeyOrder)
                {
                    vals.Add(dict[k]);
                }

                _dictValues = vals;
            }

            _totalCount = GetCount();
            UpdateContentSize();
            Refresh();
        }

        private void ClearDictMode()
        {
            _rawDict = null;
            _dictValues = null;
            _dictKeyOrder = null;
        }

        // Optional helper: Jump to item by dictionary key (generic)
        public void JumpToKey<TKey>(TKey key, float duration = 0.3f, JumpPosition pos = JumpPosition.Top,
            Ease ease = Ease.OutSine)
        {
            if (_dictKeyOrder == null) return;
            int idx = -1;
            for (int i = 0; i < _dictKeyOrder.Count; i++)
            {
                if (Equals(_dictKeyOrder[i], key))
                {
                    idx = i;
                    break;
                }
            }

            if (idx >= 0) JumpTo(idx, pos, duration, ease);
        }

        public bool IsNearEnd(float thresholdPercent = 0.8f)
        {
            if (Content == null || Viewport == null) return false;
            if (GetCount() == 0) return false;

            float viewportSize = Direction == ScrollDirection.Vertical ? Viewport.rect.height : Viewport.rect.width;
            float scrollPos = Direction == ScrollDirection.Vertical
                ? Content.anchoredPosition.y
                : -Content.anchoredPosition.x;

            float contentLen = Direction == ScrollDirection.Vertical ? Content.rect.height : Content.rect.width;
            scrollPos = Mathf.Clamp(scrollPos, 0f, Mathf.Max(0f, contentLen - viewportSize));

            float itemFull = ItemFullSize();
            int firstVisible = Mathf.FloorToInt(scrollPos / itemFull);
            int visibleItemCount = Mathf.CeilToInt(viewportSize / itemFull);

            int lastVisible = firstVisible + visibleItemCount - 1;
            int thresholdIndex = Mathf.FloorToInt(thresholdPercent * (GetCount() - 1));

            return lastVisible >= thresholdIndex;
        }

        // ---------- Notify APIs for non-observable modes ----------
        public void NotifyItemInserted(int index)
        {
            _totalCount = GetCount();
            UpdateContentSize();
            // shift active indices greater/equal index
            var keys = new List<int>(_active.Keys);
            keys.Sort((a, b) => b - a);
            foreach (var k in keys)
            {
                if (k >= index)
                {
                    var v = _active[k];
                    _active.Remove(k);
                    _active[k + 1] = v;
                    SetViewPosition(v, k + 1);
                }
            }

            Refresh();
        }

        public void NotifyItemRemoved(int index)
        {
            _totalCount = GetCount();
            if (_active.TryGetValue(index, out var v))
            {
                Recycle(index);
            }

            var keys = new List<int>(_active.Keys);
            keys.Sort();
            foreach (var k in keys)
            {
                if (k > index)
                {
                    var view = _active[k];
                    _active.Remove(k);
                    _active[k - 1] = view;
                    SetViewPosition(view, k - 1);
                    var item = GetItem(k - 1);
                    (view as IRecyclerItem<TModel>)?.SetData(item, k - 1);
                }
            }

            UpdateContentSize();
            Refresh();
        }

        public void NotifyItemChanged(int index)
        {
            if (_active.TryGetValue(index, out var v))
            {
                v?.SetData(GetItem(index), index);
            }
        }

        public void NotifyDataSetChanged()
        {
            ClearAll();
            _totalCount = GetCount();
            UpdateContentSize();
            Refresh();
        }

        // ---------- ObservableCollection callbacks (existing) ----------
        public void OnAdd(int index, TModel item) => NotifyItemInserted(index);
        public void OnRemove(int index, TModel item) => NotifyItemRemoved(index);
        public void OnReplace(int index, TModel oldItem, TModel newItem) => NotifyItemChanged(index);
        public void OnReset() => NotifyDataSetChanged();

        // ---------- Internal helpers to unify data access ----------
        public int GetCount()
        {
            if (_obsSource != null) return _obsSource.Count;
            if (_listSource != null) return _listSource.Count;
            if (_dictValues != null) return _dictValues.Count;
            return 0;
        }

        private TModel GetItem(int index)
        {
            if (_obsSource != null) return _obsSource[index];
            if (_listSource != null) return _listSource[index];
            if (_dictValues != null) return _dictValues[index];
            throw new IndexOutOfRangeException("No data source set or index out of range.");
        }

        // ---------- pool / UI logic (unchanged but using GetCount/GetItem) ----------
        private void PrewarmPool()
        {
            if (_poolRecycleView == null) return;
            for (int i = 0; i < Prewarm; i++)
            {
                var v = _poolRecycleView.Get();
                _poolRecycleView.Release(v);
            }
        }

        private void UpdateContentSize()
        {
            _totalCount = GetCount();
            if (Content == null || Viewport == null) return;

            if (IsInfinite)
            {
                // BẮT BUỘC: Chế độ Unrestricted để kéo không bị giật lại
                ScrollRect.movementType = ScrollRect.MovementType.Unrestricted;

                // Set size tượng trưng để thuật toán tính toán đúng
                float size = GetCount() * ItemFullSize();
                if (Direction == ScrollDirection.Vertical)
                    Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
                else
                    Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);

                return;
            }

            float itemFull = ItemFullSize();

            if (Direction == ScrollDirection.Vertical)
            {
                float desiredHeight = _totalCount * itemFull;
                float viewH = Viewport.rect.height;

                if (_totalCount <= 0)
                {
                    Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, viewH);
                    Content.anchoredPosition = Vector2.zero;
                    ScrollRect.vertical = false;
                }
                else if (desiredHeight <= viewH)
                {
                    Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, viewH);
                    Content.anchoredPosition = Vector2.zero;
                    ScrollRect.vertical = false;
                }
                else
                {
                    Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, desiredHeight);
                    ScrollRect.vertical = true;
                    ClampContentPosition();
                }

                Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Viewport.rect.width);
            }
            else // Horizontal
            {
                float desiredWidth = _totalCount * itemFull;
                float viewW = Viewport.rect.width;

                if (_totalCount <= 0)
                {
                    Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, viewW);
                    Content.anchoredPosition = Vector2.zero;
                    ScrollRect.horizontal = false;
                }
                else if (desiredWidth <= viewW)
                {
                    Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, viewW);
                    Content.anchoredPosition = Vector2.zero;
                    ScrollRect.horizontal = false;
                }
                else
                {
                    Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, desiredWidth);
                    ScrollRect.horizontal = true;
                    ClampContentPosition();
                }

                Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Viewport.rect.height);
            }

            Canvas.ForceUpdateCanvases();
        }

        private void ClampContentPosition()
        {
            if (IsInfinite) return;
            if (Content == null || Viewport == null) return;

            Vector2 ap = Content.anchoredPosition;

            if (Direction == ScrollDirection.Vertical)
            {
                float contentH = Content.rect.height;
                float viewH = Viewport.rect.height;
                float maxY = Mathf.Max(0f, contentH - viewH);
                ap.y = Mathf.Clamp(ap.y, 0f, maxY);
            }
            else
            {
                float contentW = Content.rect.width;
                float viewW = Viewport.rect.width;
                float maxX = Mathf.Max(0f, contentW - viewW);
                ap.x = Mathf.Clamp(ap.x, -maxX, 0f);
            }

            Content.anchoredPosition = ap;
        }

        private void OnScroll(Vector2 v)
        {
            ClampContentPosition();
            Refresh();
        }

        private void Refresh()
        {
            if (GetCount() == 0 || Content == null || Viewport == null) return;

            _totalCount = GetCount();
            float itemFull = ItemFullSize();

            // Lấy kích thước viewport
            float viewportSize = Direction == ScrollDirection.Vertical ? Viewport.rect.height : Viewport.rect.width;

            // Lấy vị trí cuộn hiện tại
            float scrollPos = Direction == ScrollDirection.Vertical
                ? Content.anchoredPosition.y
                : -Content.anchoredPosition.x;

            // --- LOGIC 1: Chỉ chặn scrollPos nếu KHÔNG PHẢI Infinite ---
            if (!IsInfinite)
            {
                float contentLen = Direction == ScrollDirection.Vertical ? Content.rect.height : Content.rect.width;
                scrollPos = Mathf.Clamp(scrollPos, 0f, Mathf.Max(0f, contentLen - viewportSize));
            }

            // Tính toán index bắt đầu và kết thúc trong vùng nhìn thấy
            int firstVisible = Mathf.FloorToInt(scrollPos / itemFull) - Buffer;
            int lastVisible = Mathf.CeilToInt((scrollPos + viewportSize) / itemFull) + Buffer;

            // --- LOGIC 2: Chỉ giới hạn Index nếu KHÔNG PHẢI Infinite ---
            // Nếu là Infinite, ta cho phép firstVisible âm (vd: -1, -2) 
            // và lastVisible vượt quá tổng số (vd: 100, 101...)
            if (!IsInfinite)
            {
                firstVisible = Mathf.Max(0, firstVisible);
                lastVisible = Mathf.Min(Mathf.Max(0, _totalCount - 1), lastVisible);
            }

            // Thu hồi các item nằm ngoài vùng nhìn thấy
            var toRecycle = new List<int>();
            foreach (var kv in _active)
            {
                int idx = kv.Key;
                if (idx < firstVisible || idx > lastVisible) toRecycle.Add(idx);
            }

            foreach (var idx in toRecycle) Recycle(idx);

            // Tạo mới các item trong vùng nhìn thấy
            for (int i = firstVisible; i <= lastVisible; i++)
            {
                // Nếu là Infinite, i có thể là 100 (trong khi max là 99).
                // Hàm CreateForIndex sẽ lo việc map 100 về 0.
                if (!_active.ContainsKey(i))
                    CreateForIndex(i);
            }
        }

        private void CreateForIndex(int index)
        {
            if (_poolRecycleView == null) return;

            // Nếu KHÔNG phải Infinite thì chặn lỗi index như cũ
            if (!IsInfinite && (index < 0 || index >= GetCount())) return;

            var view = _poolRecycleView.Get();
            view.transform.SetParent(Content, false);

            // --- LOGIC LOOP DATA ---
            int realCount = GetCount();

            // Công thức chia lấy dư để map Index ảo về Index thật
            // Vd: Index 100 -> Data 0. Index -1 -> Data 99.
            int dataIndex = (index % realCount + realCount) % realCount;

            // Đặt vị trí: Dùng 'index' (ảo) để xếp nó nằm tít bên dưới Player 99
            SetViewPosition(view, index);

            _active[index] = view;

            // Đổ dữ liệu: Dùng 'dataIndex' (thật)
            view?.SetData(GetItem(dataIndex), dataIndex);
        }

        private void SetViewPosition(TView view, int index)
        {
            var rt = view.GetComponent<RectTransform>();
            if (rt == null) return;

            if (Direction == ScrollDirection.Vertical)
            {
                // SỬA LẠI: Không dùng (0,1) và (1,1) nữa vì nó là Stretch
                // Dùng (0.5, 1) để neo vào GIỮA TRÊN
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);

                float y = -index * (ItemHeight + SpacingY); // Đã cộng thêm SpacingY

                // X = 0 vì neo ở giữa, Y = vị trí tính toán
                rt.anchoredPosition = new Vector2(0f, y);

                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ItemHeight);
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ItemWidth);
            }
            else // Horizontal
            {
                // SỬA LẠI: Dùng (0, 0.5) để neo vào TRÁI GIỮA
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);

                float x = index * (ItemWidth + SpacingX); // Đã cộng thêm SpacingX

                rt.anchoredPosition = new Vector2(x, 0f);

                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ItemWidth);
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ItemHeight); // <--- Quan trọng: Ép Height
            }
        }

        private void Recycle(int index)
        {
            if (_active.TryGetValue(index, out var v))
            {
                v?.Clear();
                _active.Remove(index);
                v.transform.localScale = Vector3.one;

                // Reset Trạng thái Hiệu ứng (Scale và Alpha)
                var cg = v.GetComponent<CanvasGroup>();
                if (cg) cg.alpha = 1f;

                _poolRecycleView.Release(v);
            }
        }

        private void ClearAll()
        {
            var keys = new List<int>(_active.Keys);
            foreach (var k in keys) Recycle(k);
            _active.Clear();
        }

        protected virtual void OnDestroy()
        {
            if (_obsSource != null) UnbindSource();
            _poolRecycleView?.Clear();
            _active.Clear();
            if (_scrollTweener != null && _scrollTweener.IsActive())
            {
                ScrollRect.onValueChanged.RemoveAllListeners();
                _scrollTweener.Kill();
                _scrollTweener = null;
            }
        }

        private float ItemFullSize()
        {
            return (Direction == ScrollDirection.Vertical) ? (ItemHeight + SpacingY) : (ItemWidth + SpacingX);
        }

        // ----- Jump / DOTween scroll (unchanged API) -----
        private void JumpToIndex(int index, float duration, Ease jumpEase = Ease.OutSine,
            float normalizedViewportPos = 0f)
        {
            if (Content == null || Viewport == null) return;
            if (GetCount() == 0) return;

            index = Mathf.Clamp(index, 0, GetCount() - 1);

            UpdateContentSize();
            Canvas.ForceUpdateCanvases();

            float itemFull = ItemFullSize();
            float itemStart = index * itemFull;
            float viewportSize = Direction == ScrollDirection.Vertical ? Viewport.rect.height : Viewport.rect.width;
            float itemSize = Direction == ScrollDirection.Vertical ? ItemHeight : ItemWidth;
            float offsetInViewport = Mathf.Clamp01(normalizedViewportPos) * Mathf.Max(0f, viewportSize - itemSize);
            float rawTargetScroll = itemStart - offsetInViewport;

            float contentLen = Direction == ScrollDirection.Vertical ? Content.rect.height : Content.rect.width;
            float maxScroll = Mathf.Max(0f, contentLen - viewportSize);
            float clampedScroll = Mathf.Clamp(rawTargetScroll, 0f, maxScroll);

            if (duration <= 0f)
            {
                if (Direction == ScrollDirection.Vertical)
                    Content.anchoredPosition = new Vector2(Content.anchoredPosition.x, clampedScroll);
                else
                    Content.anchoredPosition = new Vector2(-clampedScroll, Content.anchoredPosition.y);
                ClampContentPosition();
                Refresh();
                return;
            }

            if (_scrollTweener != null && _scrollTweener.IsActive())
            {
                _scrollTweener.Kill();
                _scrollTweener = null;
            }

            if (DisableInputDuringJump)
            {
                enabled = false;
                ScrollRect.velocity = Vector2.zero;
            }

            if (Direction == ScrollDirection.Vertical)
            {
                _scrollTweener = DOTween.To(() => Content.anchoredPosition.y,
                        y => Content.anchoredPosition = new Vector2(Content.anchoredPosition.x, y),
                        clampedScroll, duration)
                    .SetEase(jumpEase)
                    .OnUpdate(() =>
                    {
                        ClampContentPosition();
                        Refresh();
                    })
                    .OnComplete(() =>
                    {
                        if (DisableInputDuringJump) enabled = true;
                        _scrollTweener = null;
                    });

                if (JumpUseUnscaledTime) _scrollTweener.SetUpdate(true);
            }
            else
            {
                _scrollTweener = DOTween.To(() => -Content.anchoredPosition.x,
                        s => Content.anchoredPosition = new Vector2(-s, Content.anchoredPosition.y),
                        clampedScroll, duration)
                    .SetEase(jumpEase)
                    .OnUpdate(() =>
                    {
                        ClampContentPosition();
                        Refresh();
                    })
                    .OnComplete(() =>
                    {
                        if (DisableInputDuringJump) enabled = true;
                        _scrollTweener = null;
                    });

                if (JumpUseUnscaledTime) _scrollTweener.SetUpdate(true);
            }
        }

        public void JumpTo(int index, JumpPosition position = JumpPosition.Top, float duration = 0.3f,
            Ease jumpEase = Ease.OutSine)
        {
            float normalized = position == JumpPosition.Top ? 0f : (position == JumpPosition.Center ? 0.5f : 1f);
            JumpToIndex(index, duration, jumpEase, normalized);
        }

        public void JumpTo(int index, float duration, float normalizedViewportPos, Ease jumpEase = Ease.OutSine)
        {
            JumpToIndex(index, duration, jumpEase, normalizedViewportPos);
        }

        public void JumpToStart(float duration = 0.3f, Ease jumpEase = Ease.OutSine,
            JumpPosition pos = JumpPosition.Top) => JumpTo(0, pos, duration, jumpEase);

        public void JumpToMiddle(float duration = 0.3f, Ease jumpEase = Ease.OutSine,
            JumpPosition pos = JumpPosition.Top)
        {
            if (GetCount() == 0) return;
            int mid = Mathf.Clamp(GetCount() / 2, 0, GetCount() - 1);
            JumpTo(mid, pos, duration, jumpEase);
        }

        public void JumpToEnd(float duration = 0.3f, Ease jumpEase = Ease.OutSine, JumpPosition pos = JumpPosition.Top)
        {
            if (GetCount() == 0) return;
            int last = GetCount() - 1;
            JumpTo(last, pos, duration, jumpEase);
        }

        // public metrics
        public int ActiveCount => _active.Count;
        public int PoolInactiveCount => _poolRecycleView?.CountInactive ?? 0;
    }
}