using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace OSK
{
    public abstract class EntitySystem
    {
        public EntityAspect Aspect { get; protected set; }
        protected EntityManager Manager;
        
        // Cache các Entity thoả mãn Aspect
        protected readonly List<Entity> _activeEntities = new List<Entity>();
        
        /// <summary>
        /// Danh sách các Entity thoả mãn Aspect của System này.
        /// </summary>
        public IReadOnlyList<Entity> Entities => _activeEntities;

        public void Initialize(EntityManager manager)
        {
            Manager = manager;
            OnInit();
        }

        protected virtual void OnInit() {}

        /// <summary>
        /// Được gọi mỗi khi EntityManager đánh giá lại một Entity xem có khớp với Aspect hay không.
        /// </summary>
        public void EvaluateEntity(Entity entity)
        {
            if (Aspect == null) return;
            
            bool matches = Aspect.Matches(Manager, entity);
            bool contains = _activeEntities.Contains(entity);

            if (matches && !contains)
            {
                _activeEntities.Add(entity);
                OnEntityAdded(entity);
            }
            else if (!matches && contains)
            {
                _activeEntities.Remove(entity);
                OnEntityRemoved(entity);
            }
        }

        public void RemoveEntity(Entity entity)
        {
            if (_activeEntities.Remove(entity))
            {
                OnEntityRemoved(entity);
            }
        }

        protected virtual void OnEntityAdded(Entity entity) {}
        protected virtual void OnEntityRemoved(Entity entity) {}

        /// <summary>
        /// Hàm Update vòng lặp chính của System.
        /// Người dùng tự duyệt qua mảng `Entities` bằng `foreach` hoặc `Parallel.ForEach`.
        /// </summary>
        public abstract void OnUpdate(float deltaTime);

        public virtual void OnFixedUpdate(float fixedDeltaTime) {}
        public virtual void OnLateUpdate(float deltaTime) {}
    }
}
