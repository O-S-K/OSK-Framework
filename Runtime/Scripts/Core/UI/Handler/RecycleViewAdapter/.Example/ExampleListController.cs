using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace OSK.Bindings.Example
{
    /// <summary>
    /// Example controller that demonstrates using the adapter with:
    ///  - ObservableCollection<T> (call Adapter.SetSource)
    ///  - IList<T> (call Adapter.SetData)
    ///  
    /// UI (OnGUI) allows switching mode, adding/removing items, testing JumpTo and notifying adapter in list mode.
    /// </summary>
    public class ExampleListController : MonoBehaviour
    {
        public VirtualRecycleViewAdapter<PlayerDataa, PlayerItemView> Adapter;

        [Tooltip("How many items to lazily add each load")]
        public int LazyLoadBatch = 50;

        [Tooltip("Optional hard cap for total items (0 = no cap)")]
        public int MaxModelItems = 0; // tương ứng với số lượng data mình muốn hiện thị

        [Tooltip("Default duration (seconds) used by JumpTo when duration field is empty/zero")]
        public float JumpDurationDefault = 0.5f;

        public Ease JumpEase = Ease.OutCubic;

        // Observable source (your existing)
        [HideInInspector] public ObservableCollection<PlayerDataa> PlayersObs = new ObservableCollection<PlayerDataa>();

        // Plain list source
        private List<PlayerDataa> _playersList = new List<PlayerDataa>();

        public enum DataMode
        {
            Observable, // khơỉ tạo dâata = Observable
            PlainList // khởi tạo class ...
        }

        public DataMode Mode = DataMode.Observable;

        // lazy loading guard
        bool isLoadingMore = false;

        // fps
        float _fps;
        int _frameCount;
        float _dtAccum;

        // Jump UI
        string _jumpIndexStr = "0";
        string _jumpDurationStr = "0.5";
        int _viewportPosIndex = 0; // 0 = Top, 1 = Center, 2 = Bottom
        readonly string[] _viewportPosOptions = new string[] { "Top", "Center", "Bottom" };

        // simple indexer for UI add/remove in plain list
        string _insertIndexStr = "";
        string _removeIndexStr = "";

        void Start()
        {
            if (Adapter == null)
            {
                Debug.LogError("Adapter not assigned");
                return;
            }

            // Prepare both sources
            // Observable: fill initial items
            PlayersObs = new ObservableCollection<PlayerDataa>();
            // Plain list: separate copy
            _playersList = new List<PlayerDataa>();
            for (int i = 0; i < MaxModelItems; i++)
            {
                var p = GenerateRandomPlayer(i);
                PlayersObs.Add(p);
                _playersList.Add(new PlayerDataa { Name = p.Name, Score = p.Score, Avatar = p.Avatar });
            }

            // default mode -> Observable
            SetMode(Mode);
        }

        void Update()
        {
            // fps metric
            _frameCount++;
            _dtAccum += Time.unscaledDeltaTime;
            if (_dtAccum >= 1f)
            {
                _fps = _frameCount / _dtAccum;
                _frameCount = 0;
                _dtAccum = 0f;
            }

            // lazy load (only in current active source)
            if (!isLoadingMore && Adapter != null && Adapter.IsNearEnd(0.92f))
            {
                int currentCount = (Mode == DataMode.Observable) ? PlayersObs.Count : _playersList.Count;
                if (MaxModelItems <= 0 || currentCount < MaxModelItems)
                {
                    StartCoroutine(LoadMoreRoutine());
                }
            }
        }

        IEnumerator LoadMoreRoutine()
        {
            isLoadingMore = true;
            yield return null;

            int start = (Mode == DataMode.Observable) ? PlayersObs.Count : _playersList.Count;
            int toAdd = LazyLoadBatch;
            if (MaxModelItems > 0) toAdd = Mathf.Min(toAdd, MaxModelItems - start);
            if (toAdd <= 0)
            {
                isLoadingMore = false;
                yield break;
            }

            for (int i = 0; i < toAdd; i++)
            {
                var p = GenerateRandomPlayer(start + i);
                if (Mode == DataMode.Observable) PlayersObs.Add(p);
                else
                {
                    _playersList.Add(p);
                    // notify adapter about insertion in plain list mode
                    Adapter.NotifyItemInserted(_playersList.Count - 1);
                }
            }

            // ensure adapter is aware of changes
            yield return null;
            isLoadingMore = false;
        }

        void OnGUI()
        {
            
            GUILayout.BeginArea(new Rect(10, 10, 420, 1000), GUI.skin.box);

            int total = (Mode == DataMode.Observable) ? PlayersObs.Count : _playersList.Count;
            GUILayout.Label($"Mode: {Mode}   Total items: {total}");
            if (Adapter != null)
            {
                GUILayout.Label($"Active views: {Adapter.ActiveCount}");
                GUILayout.Label($"Pool inactive: {Adapter.PoolInactiveCount}");
            }

            GUILayout.Label($"FPS: {_fps:F0}");

            GUILayout.Space(8);
            GUILayout.Label("Data Mode");
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(Mode == DataMode.Observable, "Observable", "Button"))
            {
                if (Mode != DataMode.Observable) SetMode(DataMode.Observable);
            }

            if (GUILayout.Toggle(Mode == DataMode.PlainList, "PlainList", "Button"))
            {
                if (Mode != DataMode.PlainList) SetMode(DataMode.PlainList);
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("Modify Data");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Item End", GUILayout.Height(28)))
            {
                AddItemEnd();
            }

            if (GUILayout.Button("Remove Last", GUILayout.Height(28)))
            {
                RemoveLast();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Insert idx:", GUILayout.Width(70));
            _insertIndexStr = GUILayout.TextField(_insertIndexStr, GUILayout.Width(60));
            if (GUILayout.Button("Insert", GUILayout.Height(24))) TryInsertAt();
            GUILayout.Label(" Remove idx:", GUILayout.Width(80));
            _removeIndexStr = GUILayout.TextField(_removeIndexStr, GUILayout.Width(60));
            if (GUILayout.Button("Remove At", GUILayout.Height(24))) TryRemoveAt();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("Jump To Demo", GUI.skin.box);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Index:", GUILayout.Width(40));
            _jumpIndexStr = GUILayout.TextField(_jumpIndexStr, GUILayout.Width(80));
            GUILayout.Label("Duration(s):", GUILayout.Width(80));
            _jumpDurationStr = GUILayout.TextField(_jumpDurationStr, GUILayout.Width(60));
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Viewport Pos:", GUILayout.Width(80));
            for (int i = 0; i < _viewportPosOptions.Length; i++)
            {
                bool isSelected = (_viewportPosIndex == i);
                if (GUILayout.Toggle(isSelected, _viewportPosOptions[i], "Button", GUILayout.Width(80)))
                {
                    _viewportPosIndex = i;
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Jump To Index", GUILayout.Height(30)))
            {
                TryJumpToInput();
            }

            if (GUILayout.Button("Jump To Start", GUILayout.Height(30)))
            {
                Adapter.JumpToStart(ParseDurationOrDefault(_jumpDurationStr, JumpDurationDefault), JumpEase,
                    ViewportPosToJumpPosition(_viewportPosIndex));
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Jump To Middle", GUILayout.Height(30)))
            {
                Adapter.JumpToMiddle(ParseDurationOrDefault(_jumpDurationStr, JumpDurationDefault), JumpEase,
                    ViewportPosToJumpPosition(_viewportPosIndex));
            }

            if (GUILayout.Button("Jump To End", GUILayout.Height(30)))
            {
                Adapter.JumpToEnd(ParseDurationOrDefault(_jumpDurationStr, JumpDurationDefault), JumpEase,
                    ViewportPosToJumpPosition(_viewportPosIndex));
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("Other");
            if (GUILayout.Button("Clear & refill small sample", GUILayout.Height(28)))
            {
                ClearAndRefillSample();
            }

            GUILayout.EndArea();
        }

        void SetMode(DataMode mode)
        {
            Mode = mode;
            // detach previous source on adapter and set new one
            if (Mode == DataMode.Observable)
            {
                Adapter.SetSource(PlayersObs);
            }
            else // PlainList
            {
                Adapter.SetData(_playersList);
            }
        }

        void AddItemEnd()
        {
            int idx = (Mode == DataMode.Observable) ? PlayersObs.Count : _playersList.Count;
            var p = GenerateRandomPlayer(idx);
            if (Mode == DataMode.Observable)
            {
                PlayersObs.Add(p);
            }
            else
            {
                _playersList.Add(p);
                Adapter.NotifyItemInserted(_playersList.Count - 1);
            }

            JumpToIndex(idx);
        }

        void RemoveLast()
        {
            int count = (Mode == DataMode.Observable) ? PlayersObs.Count : _playersList.Count;
            if (count == 0) return;
            int last = count - 1;
            if (Mode == DataMode.Observable)
            {
                PlayersObs.RemoveAt(last);
            }
            else
            {
                _playersList.RemoveAt(last);
                Adapter.NotifyItemRemoved(last);
            }

            JumpToIndex(last - 1);
        }

        void TryInsertAt()
        {
            if (!int.TryParse(_insertIndexStr, out int idx))
            {
                Debug.LogWarning("Invalid insert index");
                return;
            }

            int count = (Mode == DataMode.Observable) ? PlayersObs.Count : _playersList.Count;
            idx = Mathf.Clamp(idx, 0, count);

            var p = GenerateRandomPlayer(idx);
            if (Mode == DataMode.Observable)
            {
                PlayersObs.Insert(idx, p);
            }
            else
            {
                _playersList.Insert(idx, p);
                Adapter.NotifyItemInserted(idx);
            }

            JumpToIndex(idx);
        }

        void TryRemoveAt()
        {
            if (!int.TryParse(_removeIndexStr, out int idx))
            {
                Debug.LogWarning("Invalid remove index");
                return;
            }

            int count = (Mode == DataMode.Observable) ? PlayersObs.Count : _playersList.Count;
            if (idx < 0 || idx >= count)
            {
                Debug.LogWarning("Remove index out of range");
                return;
            }

            if (Mode == DataMode.Observable)
            {
                PlayersObs.RemoveAt(idx);
            }
            else
            {
                _playersList.RemoveAt(idx);
                Adapter.NotifyItemRemoved(idx);
            }
        }

        void TryJumpToInput()
        {
            if (Adapter == null) return;
            if (!int.TryParse(_jumpIndexStr, out int idx))
            {
                Debug.LogWarning("Invalid index input.");
                return;
            }

            float dur = ParseDurationOrDefault(_jumpDurationStr, JumpDurationDefault);
            JumpPosition pos = ViewportPosToJumpPosition(_viewportPosIndex);

            int clamped = Mathf.Clamp(idx, 0,
                Mathf.Max(0, ((Mode == DataMode.Observable) ? PlayersObs.Count : _playersList.Count) - 1));
            Adapter.JumpTo(clamped, pos, dur, JumpEase);
        }

        private void JumpToIndex(int index)
        {
            if (Adapter == null) return;
            int count = (Mode == DataMode.Observable) ? PlayersObs.Count : _playersList.Count;
            int clamped = Mathf.Clamp(index, 0, Mathf.Max(0, count - 1));
            Adapter.JumpTo(clamped, JumpPosition.Center, JumpDurationDefault, JumpEase);
        }


        float ParseDurationOrDefault(string s, float def)
        {
            if (float.TryParse(s, out float d))
            {
                if (d < 0f) return def;
                return d;
            }

            return def;
        }

        JumpPosition ViewportPosToJumpPosition(int posIndex)
        {
            switch (posIndex)
            {
                case 0: return JumpPosition.Top;
                case 1: return JumpPosition.Center;
                case 2: return JumpPosition.Bottom;
                default: return JumpPosition.Top;
            }
        }

        void ClearAndRefillSample()
        {
            PlayersObs.Clear();
            _playersList.Clear();
            for (int i = 0; i < 10; i++)
            {
                var p = GenerateRandomPlayer(i);
                PlayersObs.Add(p);
                _playersList.Add(new PlayerDataa { Name = p.Name, Score = p.Score, Avatar = p.Avatar });
            }

            // re-apply current mode's source
            if (Mode == DataMode.Observable) Adapter.SetSource(PlayersObs);
            else Adapter.SetData(_playersList);
        }

        PlayerDataa GenerateRandomPlayer(int id)
        {
            return new PlayerDataa
            {
                Name = $"Player {id}",
                Score = Random.Range(0, 9999),
                Avatar = null
            };
        }
    }
}