using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace OSK
{
    /// <summary>
    /// Thread-safe Command Buffer.
    /// Giúp ghi lại các lệnh Tạo/Xoá/Sửa Component trong lúc chạy Đa luồng (Parallel).
    /// Các lệnh này sẽ được xử lý đồng loạt ở cuối Frame để tránh đụng độ bộ nhớ.
    /// </summary>
    public class EntityCommandBuffer
    {
        private enum CommandType
        {
            CreateEntity,
            DestroyEntity,
            AddComponent,
            RemoveComponent
        }

        private struct Command
        {
            public CommandType Type;
            public Entity Entity;
            public Type ComponentType;
            public object ComponentData;
        }

        private readonly ConcurrentQueue<Command> _commands = new ConcurrentQueue<Command>();

        public void CreateEntity()
        {
            _commands.Enqueue(new Command { Type = CommandType.CreateEntity });
        }

        public void DestroyEntity(Entity entity)
        {
            _commands.Enqueue(new Command { Type = CommandType.DestroyEntity, Entity = entity });
        }

        public void AddComponent<T>(Entity entity, T component) where T : struct, IComponentData
        {
            _commands.Enqueue(new Command
            {
                Type = CommandType.AddComponent,
                Entity = entity,
                ComponentType = typeof(T),
                ComponentData = component
            });
        }

        public void RemoveComponent<T>(Entity entity) where T : struct, IComponentData
        {
            _commands.Enqueue(new Command
            {
                Type = CommandType.RemoveComponent,
                Entity = entity,
                ComponentType = typeof(T)
            });
        }

        /// <summary>
        /// Xử lý tất cả các lệnh đã lưu trong Queue (Chỉ gọi trên luồng chính - Main Thread).
        /// </summary>
        public void Flush(EntityManager manager)
        {
            while (_commands.TryDequeue(out var cmd))
            {
                switch (cmd.Type)
                {
                    case CommandType.CreateEntity:
                        manager.CreateEntity();
                        break;
                    case CommandType.DestroyEntity:
                        manager.DestroyEntity(cmd.Entity);
                        break;
                    case CommandType.AddComponent:
                        // Dùng Reflection an toàn vì chỉ chạy ở Flush
                        manager.SetComponentDataRaw(cmd.Entity, cmd.ComponentType, cmd.ComponentData);
                        break;
                    case CommandType.RemoveComponent:
                        manager.RemoveComponentRaw(cmd.Entity, cmd.ComponentType);
                        break;
                }
            }
        }
    }
}
