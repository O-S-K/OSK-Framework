using UnityEngine;

namespace OSK
{
    /// <summary>
    /// Cầu nối giữa Unity GameObject và Pure ECS Entity.
    /// Dùng để render hình ảnh, đồng bộ Transform và xử lý va chạm (Physics).
    /// </summary>
    public class EntityView : MonoBehaviour, IPoolable
    {
        public Entity LinkedEntity { get; private set; }

        public void Link(Entity entity)
        {
            LinkedEntity = entity;
        }

        public virtual void OnSpawn()
        {
        }

        public virtual void OnDespawn()
        {
            if (LinkedEntity != Entity.Null && Main.Entity != null)
            {
                Main.Entity.CommandBuffer.DestroyEntity(LinkedEntity);
            }
            LinkedEntity = Entity.Null;
        }

        protected virtual void OnDestroy()
        {
            if (LinkedEntity != Entity.Null && Main.Entity != null)
            {
                // Dọn rác khi GameObject bị huỷ cứng (chuyển Scene, Destroy, v.v)
                Main.Entity.CommandBuffer.DestroyEntity(LinkedEntity);
                LinkedEntity = Entity.Null;
            }
        }
    }
}
