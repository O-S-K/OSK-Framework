using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace OSK
{
    public class EntityManager : GameFrameworkComponent
    {
        [ShowInInspector, ReadOnly] 
        private readonly Dictionary<int, Entity> _entities = new Dictionary<int, Entity>();

        public override void OnInit() {}

        public Entity Create(string name, int id = -1)
        {
            if (id != -1 && _entities.ContainsKey(id))
                return _entities[id];

            var entity = new GameObject(name).AddComponent<Entity>();
            if (id != -1) entity.ID = id;

            _entities[entity.ID] = entity;
            return entity;
        }

        public Entity Create(IEntity entity, int id)
        {
            if (_entities.ContainsKey(id))
                return _entities[id];

            if (entity is Entity newEntity)
            {
                newEntity.ID = id;
                _entities[id] = newEntity;
                return newEntity;
            }
            return null;
        }

        public Entity Create<T>(string name, int id = -1) where T : Component
        {
            if (id != -1 && _entities.ContainsKey(id))
                return _entities[id];

            var entity = new GameObject(name).AddComponent<Entity>();
            entity.gameObject.AddComponent<T>();
            if (id != -1) entity.ID = id;

            _entities[entity.ID] = entity;
            return entity;
        }

        public bool Has(int id) => _entities.ContainsKey(id);

        public Entity Get(int id) => _entities.TryGetValue(id, out var entity) ? entity : null;

        public Entity GetEntityWith<T>() where T : Component
        {
            return _entities.Values.FirstOrDefault(e => e.gameObject.TryGetComponent<T>(out _));
        }

        public T GetComponentFromEntity<T>(string name) where T : Component
        {
            var entity = _entities.Values
                .FirstOrDefault(e => e.name == name && e.gameObject.TryGetComponent<T>(out _));

            return entity != null ? entity.GetComponent<T>() : null;
        }

        public Entity GetByID(int id) => Get(id);

        public List<Entity> GetAll() => _entities.Values.ToList();

        public void Destroy(int id)
        {
            if (!_entities.TryGetValue(id, out var entity)) 
                return;

            UnityEngine.Object.Destroy(entity.gameObject);
            _entities.Remove(id);
        }

        public void Destroy(Entity entity)
        {
            if (entity == null) return;

            if (_entities.ContainsKey(entity.ID))
            {
                UnityEngine.Object.Destroy(entity.gameObject);
                _entities.Remove(entity.ID);
            }
        }

        public void Remove(Entity entity)
        {
            if (entity != null && _entities.ContainsKey(entity.ID))
                _entities.Remove(entity.ID);
        }
    }
}
