using System;
using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    /// <summary>
    /// Base class cho các thành phần UI nhỏ (không phải là 1 màn hình/View hoàn chỉnh).
    /// Rất lý tưởng cho các Item trong ScrollView/List/Grid hoặc các cụm UI con dùng chung.
    /// </summary>
    public abstract class BaseBinder<TModel> : MonoBehaviour
    {
        public TModel Model { get; private set; }
        private List<Action> _unbindActions = new List<Action>();

        /// <summary>
        /// Gọi hàm này để truyền Data vào và tự động cập nhật UI.
        /// </summary>
        public virtual void SetModel(TModel data)
        {
            // Xóa các Binding cũ trước khi nạp Data mới (rất quan trọng khi xài Object Pool)
            ClearBindings();

            Model = data;
            RefreshUI();
        }

        /// <summary>
        /// Ràng buộc dữ liệu từ BindableProperty vào UI. Tự động gỡ khi Item bị Destroy hoặc SetModel mới.
        /// </summary>
        protected void Bind<T>(BindableProperty<T> property, Action<T> onValueChanged, bool triggerImmediately = true)
        {
            if (property == null) return;
            property.Bind(onValueChanged, triggerImmediately);
            _unbindActions.Add(() => property.Unbind(onValueChanged));
        }

        /// <summary>
        /// Nơi thực hiện gán dữ liệu từ Model lên giao diện (Text, Image...)
        /// </summary>
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
