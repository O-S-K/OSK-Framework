using System;
using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    public class EntityManager : GameFrameworkComponent, IUpdateable, IFixedUpdateable, ILateUpdateable
    {
        // Quản lý Entity ID & Version
        private int _nextEntityId = 0;
        private readonly List<int> _entityVersions = new List<int>();
        private readonly Queue<int> _recycledIds = new Queue<int>();
        
        // Quản lý Data & System
        private readonly Dictionary<Type, IComponentPool> _componentPools = new Dictionary<Type, IComponentPool>();
        private readonly List<EntitySystem> _systems = new List<EntitySystem>();
        private readonly List<EntityQueryBase> _queries = new List<EntityQueryBase>();
        private readonly Dictionary<Type, object> _singletons = new Dictionary<Type, object>();
        
        public EntityCommandBuffer CommandBuffer { get; private set; }

        public override void OnInit()
        {
            CommandBuffer = new EntityCommandBuffer();
            Main.RegisterTick(this);
        }

        public override void OnDestroy()
        {
            Main.UnRegisterTick(this);
            base.OnDestroy();
        }

        #region Entity Lifecycle (ID Management)

        public Entity CreateEntity()
        {
            int id;
            int version;
            if (_recycledIds.Count > 0)
            {
                id = _recycledIds.Dequeue();
                version = _entityVersions[id];
            }
            else
            {
                id = _nextEntityId++;
                version = 1;
                _entityVersions.Add(version);
            }
            return new Entity(id, version);
        }

        public bool IsAlive(Entity entity)
        {
            if (entity.ID < 0 || entity.ID >= _entityVersions.Count) return false;
            return _entityVersions[entity.ID] == entity.Version;
        }

        public void DestroyEntity(Entity entity)
        {
            if (!IsAlive(entity)) return;

            // Tăng version để các reference cũ trở thành rác
            _entityVersions[entity.ID]++;
            _recycledIds.Enqueue(entity.ID);

            // Xoá toàn bộ component của Entity này
            foreach (var pool in _componentPools.Values)
            {
                pool.Remove(entity.ID);
            }

            // Phải thông báo cho các System biết để nó xoá Entity khỏi danh sách Cache
            EvaluateEntity(entity);
        }

        #endregion

        #region Component Management (Data-Oriented)

        private ComponentPool<T> GetPool<T>() where T : struct, IComponentData
        {
            var type = typeof(T);
            if (!_componentPools.TryGetValue(type, out var pool))
            {
                pool = new ComponentPool<T>();
                _componentPools[type] = pool;
            }
            return (ComponentPool<T>)pool;
        }

        public void AddComponent<T>(Entity entity, T component) where T : struct, IComponentData
        {
            if (!IsAlive(entity)) return;
            GetPool<T>().Add(entity.ID, component);
            EvaluateEntity(entity);
        }

        public ref T GetComponent<T>(Entity entity) where T : struct, IComponentData
        {
            // Throw exception hoặc return mặc định nếu ko có. Ở đây mặc định throw nếu Dev dùng sai.
            return ref GetPool<T>().Get(entity.ID);
        }

        public bool HasComponent<T>(Entity entity) where T : struct, IComponentData
        {
            if (!IsAlive(entity)) return false;
            return GetPool<T>().Has(entity.ID);
        }

        public bool HasComponent(Entity entity, Type componentType)
        {
            if (!IsAlive(entity)) return false;
            if (_componentPools.TryGetValue(componentType, out var pool))
            {
                return pool.Has(entity.ID);
            }
            return false;
        }

        public void RemoveComponent<T>(Entity entity) where T : struct, IComponentData
        {
            if (!IsAlive(entity)) return;
            GetPool<T>().Remove(entity.ID);
            EvaluateEntity(entity);
        }

        // Hỗ trợ Reflection cho Hàng đợi (Command Buffer)
        internal void SetComponentDataRaw(Entity entity, Type componentType, object data)
        {
            if (!IsAlive(entity)) return;
            // Tạo Pool tự động nếu chưa có
            if (!_componentPools.TryGetValue(componentType, out var pool))
            {
                var poolType = typeof(ComponentPool<>).MakeGenericType(componentType);
                pool = (IComponentPool)Activator.CreateInstance(poolType);
                _componentPools[componentType] = pool;
            }
            
            // Invoke phương thức Add qua reflection
            var method = pool.GetType().GetMethod("Add");
            method?.Invoke(pool, new object[] { entity.ID, data });
            EvaluateEntity(entity);
        }

        internal void RemoveComponentRaw(Entity entity, Type componentType)
        {
            if (!IsAlive(entity)) return;
            if (_componentPools.TryGetValue(componentType, out var pool))
            {
                pool.Remove(entity.ID);
                EvaluateEntity(entity);
            }
        }

        #endregion

        #region ECS Query & System

        public void RegisterSystem(EntitySystem system)
        {
            var type = system.GetType();
            for (int i = 0; i < _systems.Count; i++)
            {
                if (_systems[i].GetType() == type)
                {
                    return; // Ngăn chặn đăng ký trùng lặp System khi Reset Game
                }
            }

            system.Initialize(this);
            _systems.Add(system);
        }

        public void UnregisterSystem(EntitySystem system)
        {
            _systems.Remove(system);
        }

        public void RegisterQuery(EntityQueryBase query)
        {
            if (!_queries.Contains(query))
            {
                query.Initialize(this);
                _queries.Add(query);
                
                // Nạp trước các Entity đang tồn tại vào Cache của Query mới
                for (int i = 0; i < _nextEntityId; i++)
                {
                    var entity = new Entity(i, _entityVersions[i]);
                    if (IsAlive(entity)) query.EvaluateEntity(entity);
                }
            }
        }

        private void EvaluateEntity(Entity entity)
        {
            for (int i = 0; i < _systems.Count; i++)
            {
                _systems[i].EvaluateEntity(entity);
            }
            
            for (int i = 0; i < _queries.Count; i++)
            {
                _queries[i].EvaluateEntity(entity);
            }
        }

        /// <summary>
        /// Quét tất cả Entity thoả mãn Aspect. Rất tốn kém (O(N)), nên System sẽ cache lại để chạy nhanh.
        /// </summary>
        public List<Entity> Query(EntityAspect aspect)
        {
            var result = new List<Entity>();
            for (int i = 0; i < _nextEntityId; i++)
            {
                var entity = new Entity(i, _entityVersions[i]);
                if (!IsAlive(entity)) continue;

                if (aspect.Matches(this, entity))
                {
                    result.Add(entity);
                }
            }
            return result;
        }

        #endregion

        #region Singletons

        public void SetSingleton<T>(T component) where T : class
        {
            _singletons[typeof(T)] = component;
        }

        public T GetSingleton<T>() where T : class
        {
            if (_singletons.TryGetValue(typeof(T), out var comp))
                return (T)comp;
            return null;
        }

        #endregion

        #region Centralized Tick

        public void OnUpdate()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _systems.Count; i++)
            {
                _systems[i].OnUpdate(dt);
            }

            // Đồng bộ dữ liệu rác (Command Buffer) sau khi tất cả hệ thống đã hoàn tất vòng lặp
            CommandBuffer.Flush(this);
        }

        public void OnFixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            for (int i = 0; i < _systems.Count; i++)
            {
                _systems[i].OnFixedUpdate(dt);
            }
        }

        public void OnLateUpdate()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _systems.Count; i++)
            {
                _systems[i].OnLateUpdate(dt);
            }
        }

        #endregion
    }
}
