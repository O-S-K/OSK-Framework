using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    /// <summary>
    /// RecycleViewAdapter: map ObservableCollection<TModel> -> pooled item views (prefab)
    /// Usage:
    /// - Attach to a GameObject with a Vertical Layout Group or Container (content)
    /// - Set prefab (TView component implementing IRecyclerItem<TModel>) and content transform.
    /// - Call SetSource(observableCollection).
    /// example: public class PlayerListAdapter : RecycleViewAdapter<PlayerDataa, PlayerItemView>
    /// </summary>
    public class RecycleViewAdapter<TModel, TView> : MonoBehaviour
        where TView : Component, IRecyclerItem<TModel>
    {
        [Header("Setup")] public TView ItemPrefab;
        public Transform content; // parent for item views

        // Optional: limit number of active items at once (virtualization), not implemented full here
        public int MaxActiveItems = 1000;

        private ObservableCollection<TModel> _source;
        private readonly List<TView> _active = new List<TView>();
        private PoolRecycleView<TView> _poolRecycleView;

        protected virtual void Awake()
        {
            if (ItemPrefab == null) Debug.LogWarning("ItemPrefab is null on RecycleViewAdapter");
            if (content == null) content = this.transform;
            if (ItemPrefab != null) _poolRecycleView = new PoolRecycleView<TView>(ItemPrefab, content);
        }

        public void SetSource(ObservableCollection<TModel> source)
        {
            if (_source != null) UnbindSource();
            _source = source;
            if (_source != null) BindSource();
            RefreshAll();
        }

        protected void BindSource()
        {
            _source.OnAdd += OnAdd;
            _source.OnRemove += OnRemove;
            _source.OnReplace += OnReplace;
            _source.OnReset += OnReset; 
        }

        protected void UnbindSource()
        {
            _source.OnAdd -= OnAdd;
            _source.OnRemove -= OnRemove;
            _source.OnReplace -= OnReplace;
            _source.OnReset -= OnReset;
        }

        // Create item view at the end or at index
        protected void OnAdd(int index, TModel item)
        {
            // instantiate and insert at correct position
            var view = _poolRecycleView.Get();
            _active.Insert(index, view);
            view.transform.SetSiblingIndex(index);
            view.gameObject.SetActive(true);
            if (view is IRecyclerItem<TModel> r) r.SetData(item, index);
            // update subsequent indices
            UpdateIndicesFrom(index + 1);
        }

        protected void OnRemove(int index, TModel item)
        {
            if (index < 0 || index >= _active.Count) return;
            var v = _active[index];
            _active.RemoveAt(index);
            if (v is IRecyclerItem<TModel> r) r.Clear();
            _poolRecycleView.Release(v);
            UpdateIndicesFrom(index);
        }

        protected void OnReplace(int index, TModel oldItem, TModel newItem)
        {
            if (index < 0 || index >= _active.Count) return;
            var v = _active[index];
            if (v is IRecyclerItem<TModel> r) r.SetData(newItem, index);
        }

        protected void OnReset()
        {
            ClearAll();
        }

        protected void ClearAll()
        {
            for (int i = _active.Count - 1; i >= 0; --i)
            {
                var v = _active[i];
                if (v is IRecyclerItem<TModel> r) r.Clear();
                _poolRecycleView.Release(v);
            }

            _active.Clear();
        }

        protected void RefreshAll()
        {
            ClearAll();
            if (_source == null) return;
            for (int i = 0; i < _source.Count && i < MaxActiveItems; i++)
            {
                var view = _poolRecycleView.Get();
                _active.Add(view);
                view.transform.SetSiblingIndex(i);
                (view as IRecyclerItem<TModel>)?.SetData(_source[i], i);
            }
        }

        private void UpdateIndicesFrom(int startIndex)
        {
            for (int i = startIndex; i < _active.Count; i++)
            {
                var v = _active[i];
                (v as IRecyclerItem<TModel>)?.SetData(_source[i], i);
            }
        }

        protected void OnDestroy()
        {
            if (_source != null) UnbindSource();
            _poolRecycleView?.Clear();
        }
    }
}