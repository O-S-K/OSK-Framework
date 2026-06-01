using System;
using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    /// <summary>
    /// Base class for small UI elements (not a complete screen/View).
    /// </summary>
    public abstract class BaseBinder<TModel> : MonoBehaviour
    {
        public TModel Model { get; private set; }
        private List<Action> _unbindActions = new List<Action>();

        /// <summary>
        /// Call this function to pass data in and automatically update the UI.
        /// </summary>
        public virtual void SetModel(TModel data)
        {
            ClearBindings();
            Model = data;
            RefreshUI();
        }

        /// <summary>
        /// Bind data from BindableProperty to the UI. Automatically unbind when an item is destroyed or a new SetModel is created.
        /// </summary>
        protected void Bind<T>(BindableProperty<T> property, Action<T> onValueChanged, bool triggerImmediately = true)
        {
            if (property == null) return;
            property.Bind(onValueChanged, triggerImmediately);
            _unbindActions.Add(() => property.Unbind(onValueChanged));
        }

        /// <summary>
        /// Two-way binding between UI Input and BindableProperty. When the UI changes, the Model will be updated, and vice versa.
        /// /// </summary>
        protected abstract void RefreshUI();

        protected virtual void ClearBindings()
        {
            foreach (var unbind in _unbindActions)
            {
                unbind?.Invoke();
            }
            _unbindActions.Clear();
        }

        protected virtual void OnDestroy()
        {
            ClearBindings();
        }
    }
}
