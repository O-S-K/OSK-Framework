using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OSK
{
    public class Entity : MonoBehaviour, IEntity, IUpdate, IFixedUpdate, ILateUpdate
    {
        public int ID { get; set; }
        public bool IsActive { get; set; }

        private readonly Dictionary<System.Type, EComponent> _components = new();

        #region Components
        public T Add<T>() where T : EComponent
        {
            var type = typeof(T);
            if (_components.ContainsKey(type))
            {
                Logg.LogError($"Component {type} already exists");
                return null;
            }

            var component = gameObject.AddComponent<T>();
            _components[type] = component;
            return component;
        }

        public T Get<T>() where T : EComponent
        {
            var type = typeof(T);
            return _components.TryGetValue(type, out var comp) ? (T)comp : null;
        }

        public void Remove<T>() where T : EComponent
        {
            var type = typeof(T);
            if (_components.TryGetValue(type, out var comp))
            {
                _components.Remove(type);
                Destroy(comp);
            }
        }

        #endregion
        
        
        public virtual void Show()
        {
            Main.Mono.Register(this);
            gameObject.SetActive(true);
            IsActive = true;
        }

        public virtual void Hide()
        {
            Main.Mono.UnRegister(this);
            gameObject.SetActive(false);
            IsActive = false;
        }
        
        public virtual void Delete()
        {
            Main.Mono.UnRegister(this);
            Main.Entity.Destroy(this);
            //_components.Values.ToList().ForEach(Destroy);
            //_components.Clear();
            Destroy(gameObject);
        }

        public void SetParent(Transform parent)
        {
            transform.SetParent(parent);
        }

        public void SetParentNull()
        {
            transform.SetParent(null);
        }

        public virtual void Tick(float deltaTime){}
        public virtual void FixedTick(float fixedDeltaTime){}
        public virtual void LateTick(float deltaTime) {}
    }
}