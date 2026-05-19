using System;
using System.Collections.Generic;


namespace OSK
{
    public abstract class View<TModel> : View
    {
        public TModel Model { get; private set; }
        private List<Action> _unbindActions = new List<Action>();

        protected override bool OnValidateData(object data)
        {
            return data is TModel;
        }

        protected override void SetData(object data = null)
        {
            base.SetData(data);
            if (data is TModel validData)
            {
                Model = validData;
                RefreshUI();
            }
        }
        protected void Bind<T>(BindableProperty<T> property, Action<T> onValueChanged, bool triggerImmediately = true)
        {
            if (property == null) return;
            property.Bind(onValueChanged, triggerImmediately);
            _unbindActions.Add(() => property.Unbind(onValueChanged));
        }

        protected void Bind<T>(BindableList<T> list, Action<IList<T>> onListChanged, bool triggerImmediately = true)
        {
            if (list == null) return;
            list.Bind(onListChanged, triggerImmediately);
            _unbindActions.Add(() => list.Unbind(onListChanged));
        }

        protected void BindDetailed<T>(BindableList<T> list, Action<CollectionChangedEventArgs<T>> onItemChanged)
        {
            if (list == null) return;
            list.BindDetailed(onItemChanged);
            _unbindActions.Add(() => list.UnbindDetailed(onItemChanged));
        }

        protected void Bind<TKey, TValue>(BindableDictionary<TKey, TValue> dict, Action<IDictionary<TKey, TValue>> onDictChanged, bool triggerImmediately = true)
        {
            if (dict == null) return;
            dict.Bind(onDictChanged, triggerImmediately);
            _unbindActions.Add(() => dict.Unbind(onDictChanged));
        }

        protected void Bind(BindableTrigger trigger, Action onTrigger)
        {
            if (trigger == null) return;
            trigger.Bind(onTrigger);
            _unbindActions.Add(() => trigger.Unbind(onTrigger));
        }

        protected void Bind<T>(BindableTrigger<T> trigger, Action<T> onTrigger)
        {
            if (trigger == null) return;
            trigger.Bind(onTrigger);
            _unbindActions.Add(() => trigger.Unbind(onTrigger));
        }

        // --- Bind 2 chiều (Two-Way Binding) ---

        protected void BindTwoWay(UnityEngine.UI.InputField inputField, BindableProperty<string> property)
        {
            if (property == null || inputField == null) return;
            
            // Model -> View
            Action<string> updateView = (val) => {
                if (inputField.text != val) inputField.text = val;
            };
            property.Bind(updateView, true);
            _unbindActions.Add(() => property.Unbind(updateView));

            // View -> Model
            UnityEngine.Events.UnityAction<string> updateModel = (val) => {
                if (property.Value != val) property.Value = val;
            };
            inputField.onValueChanged.AddListener(updateModel);
            _unbindActions.Add(() => inputField.onValueChanged.RemoveListener(updateModel));
        }

        protected void BindTwoWay(UnityEngine.UI.Toggle toggle, BindableProperty<bool> property)
        {
            if (property == null || toggle == null) return;

            // Model -> View
            Action<bool> updateView = (val) => {
                if (toggle.isOn != val) toggle.isOn = val;
            };
            property.Bind(updateView, true);
            _unbindActions.Add(() => property.Unbind(updateView));

            // View -> Model
            UnityEngine.Events.UnityAction<bool> updateModel = (val) => {
                if (property.Value != val) property.Value = val;
            };
            toggle.onValueChanged.AddListener(updateModel);
            _unbindActions.Add(() => toggle.onValueChanged.RemoveListener(updateModel));
        }

        protected void BindTwoWay(UnityEngine.UI.Slider slider, BindableProperty<float> property)
        {
            if (property == null || slider == null) return;

            // Model -> View
            Action<float> updateView = (val) => {
                // Kiểm tra bằng toán học xấp xỉ để tránh vòng lặp vô tận do float
                if (UnityEngine.Mathf.Abs(slider.value - val) > 0.0001f) slider.value = val;
            };
            property.Bind(updateView, true);
            _unbindActions.Add(() => property.Unbind(updateView));

            // View -> Model
            UnityEngine.Events.UnityAction<float> updateModel = (val) => {
                if (UnityEngine.Mathf.Abs(property.Value - val) > 0.0001f) property.Value = val;
            };
            slider.onValueChanged.AddListener(updateModel);
            _unbindActions.Add(() => slider.onValueChanged.RemoveListener(updateModel));
        }

        protected void BindTwoWay(UnityEngine.UI.Dropdown dropdown, BindableProperty<int> property)
        {
            if (property == null || dropdown == null) return;

            // Model -> View
            Action<int> updateView = (val) => {
                if (dropdown.value != val) dropdown.value = val;
            };
            property.Bind(updateView, true);
            _unbindActions.Add(() => property.Unbind(updateView));

            // View -> Model
            UnityEngine.Events.UnityAction<int> updateModel = (val) => {
                if (property.Value != val) property.Value = val;
            };
            dropdown.onValueChanged.AddListener(updateModel);
            _unbindActions.Add(() => dropdown.onValueChanged.RemoveListener(updateModel));
        }

        protected void BindTwoWay(UnityEngine.UI.Scrollbar scrollbar, BindableProperty<float> property)
        {
            if (property == null || scrollbar == null) return;

            // Model -> View
            Action<float> updateView = (val) => {
                if (UnityEngine.Mathf.Abs(scrollbar.value - val) > 0.0001f) scrollbar.value = val;
            };
            property.Bind(updateView, true);
            _unbindActions.Add(() => property.Unbind(updateView));

            // View -> Model
            UnityEngine.Events.UnityAction<float> updateModel = (val) => {
                if (UnityEngine.Mathf.Abs(property.Value - val) > 0.0001f) property.Value = val;
            };
            scrollbar.onValueChanged.AddListener(updateModel);
            _unbindActions.Add(() => scrollbar.onValueChanged.RemoveListener(updateModel));
        }

        protected abstract void RefreshUI();

        public override void Hide()
        {
            foreach (var unbind in _unbindActions)
            {
                unbind?.Invoke();
            }
            _unbindActions.Clear();
            
            base.Hide();
        }
    }
}
