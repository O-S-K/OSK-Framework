using System;
using System.Collections.Generic;
using System.Linq;

namespace OSK
{
    public class EntityAspect
    {
        private readonly HashSet<Type> _all = new HashSet<Type>();
        private readonly HashSet<Type> _any = new HashSet<Type>();
        private readonly HashSet<Type> _none = new HashSet<Type>();

        /// <summary>
        /// Entity phải CHỨA TẤT CẢ các Component này.
        /// </summary>
        public EntityAspect All(params Type[] types)
        {
            foreach (var t in types) _all.Add(t);
            return this;
        }

        /// <summary>
        /// Entity phải CHỨA ÍT NHẤT MỘT trong các Component này.
        /// </summary>
        public EntityAspect Any(params Type[] types)
        {
            foreach (var t in types) _any.Add(t);
            return this;
        }

        /// <summary>
        /// Entity KHÔNG ĐƯỢC CHỨA bất kỳ Component nào trong danh sách này.
        /// </summary>
        public EntityAspect None(params Type[] types)
        {
            foreach (var t in types) _none.Add(t);
            return this;
        }

        /// <summary>
        /// Kiểm tra xem một Entity có khớp với Aspect này không.
        /// </summary>
        public bool Matches(EntityManager manager, Entity entity)
        {
            // Kiểm tra All
            if (_all.Count > 0)
            {
                foreach (var type in _all)
                {
                    if (!manager.HasComponent(entity, type))
                        return false;
                }
            }

            // Kiểm tra None
            if (_none.Count > 0)
            {
                foreach (var type in _none)
                {
                    if (manager.HasComponent(entity, type))
                        return false;
                }
            }

            // Kiểm tra Any
            if (_any.Count > 0)
            {
                bool hasAny = false;
                foreach (var type in _any)
                {
                    if (manager.HasComponent(entity, type))
                    {
                        hasAny = true;
                        break;
                    }
                }
                if (!hasAny) return false;
            }

            return true;
        }
    }
}
