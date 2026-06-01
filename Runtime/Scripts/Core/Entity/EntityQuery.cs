using System.Collections.Generic;
using System.Threading.Tasks;

namespace OSK
{
    public abstract class EntityQueryBase
    {
        public EntityAspect Aspect { get; protected set; }
        protected EntityManager Manager;
        
        protected readonly List<Entity> _activeEntities = new List<Entity>();
        public IReadOnlyList<Entity> Entities => _activeEntities;

        public void Initialize(EntityManager manager)
        {
            Manager = manager;
        }

        public void EvaluateEntity(Entity entity)
        {
            if (Aspect == null) return;
            
            bool matches = Aspect.Matches(Manager, entity);
            bool contains = _activeEntities.Contains(entity);

            if (matches && !contains)
            {
                _activeEntities.Add(entity);
            }
            else if (!matches && contains)
            {
                _activeEntities.Remove(entity);
            }
        }
    }

    public delegate void QueryAction1<T1>(ref T1 c1);
    public delegate void QueryAction2<T1, T2>(ref T1 c1, ref T2 c2);
    public delegate void QueryAction3<T1, T2, T3>(ref T1 c1, ref T2 c2, ref T3 c3);
    public delegate void QueryAction4<T1, T2, T3, T4>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4);
    public delegate void QueryAction5<T1, T2, T3, T4, T5>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5);
    public delegate void QueryAction6<T1, T2, T3, T4, T5, T6>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6);
    public delegate void QueryAction7<T1, T2, T3, T4, T5, T6, T7>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7);
    public delegate void QueryAction8<T1, T2, T3, T4, T5, T6, T7, T8>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8);

    public class Query<T1> : EntityQueryBase 
        where T1 : struct, IComponentData
    {
        public Query(EntityManager manager)
        {
            Aspect = new EntityAspect().All(typeof(T1));
            manager.RegisterQuery(this);
        }

        public void ForEach(QueryAction1<T1> action)
        {
            for (int i = _activeEntities.Count - 1; i >= 0; i--)
            {
                var entity = _activeEntities[i];
                ref var c1 = ref Manager.GetComponent<T1>(entity);
                action(ref c1);
            }
        }

        public void ForEachParallel(QueryAction1<T1> action)
        {
            Parallel.For(0, _activeEntities.Count, i =>
            {
                var entity = _activeEntities[i];
                ref var c1 = ref Manager.GetComponent<T1>(entity);
                action(ref c1);
            });
        }
    }

    public class Query<T1, T2> : EntityQueryBase 
        where T1 : struct, IComponentData
        where T2 : struct, IComponentData
    {
        public Query(EntityManager manager)
        {
            Aspect = new EntityAspect().All(typeof(T1), typeof(T2));
            manager.RegisterQuery(this);
        }

        public void ForEach(QueryAction2<T1, T2> action)
        {
            for (int i = _activeEntities.Count - 1; i >= 0; i--)
            {
                var entity = _activeEntities[i];
                ref var c1 = ref Manager.GetComponent<T1>(entity);
                ref var c2 = ref Manager.GetComponent<T2>(entity);
                action(ref c1, ref c2);
            }
        }

        public void ForEachParallel(QueryAction2<T1, T2> action)
        {
            Parallel.For(0, _activeEntities.Count, i =>
            {
                var entity = _activeEntities[i];
                ref var c1 = ref Manager.GetComponent<T1>(entity);
                ref var c2 = ref Manager.GetComponent<T2>(entity);
                action(ref c1, ref c2);
            });
        }
    }

    public class Query<T1, T2, T3> : EntityQueryBase 
        where T1 : struct, IComponentData
        where T2 : struct, IComponentData
        where T3 : struct, IComponentData
    {
        public Query(EntityManager manager)
        {
            Aspect = new EntityAspect().All(typeof(T1), typeof(T2), typeof(T3));
            manager.RegisterQuery(this);
        }

        public void ForEach(QueryAction3<T1, T2, T3> action)
        {
            for (int i = _activeEntities.Count - 1; i >= 0; i--)
            {
                var entity = _activeEntities[i];
                ref var c1 = ref Manager.GetComponent<T1>(entity);
                ref var c2 = ref Manager.GetComponent<T2>(entity);
                ref var c3 = ref Manager.GetComponent<T3>(entity);
                action(ref c1, ref c2, ref c3);
            }
        }

        public void ForEachParallel(QueryAction3<T1, T2, T3> action)
        {
            Parallel.For(0, _activeEntities.Count, i =>
            {
                var entity = _activeEntities[i];
                ref var c1 = ref Manager.GetComponent<T1>(entity);
                ref var c2 = ref Manager.GetComponent<T2>(entity);
                ref var c3 = ref Manager.GetComponent<T3>(entity);
                action(ref c1, ref c2, ref c3);
            });
        }
    }

    public class Query<T1, T2, T3, T4> : EntityQueryBase 
        where T1 : struct, IComponentData
        where T2 : struct, IComponentData
        where T3 : struct, IComponentData
        where T4 : struct, IComponentData
    {
        public Query(EntityManager manager)
        {
            Aspect = new EntityAspect().All(typeof(T1), typeof(T2), typeof(T3), typeof(T4));
            manager.RegisterQuery(this);
        }

        public void ForEach(QueryAction4<T1, T2, T3, T4> action)
        {
            for (int i = _activeEntities.Count - 1; i >= 0; i--)
            {
                var entity = _activeEntities[i];
                ref var c1 = ref Manager.GetComponent<T1>(entity);
                ref var c2 = ref Manager.GetComponent<T2>(entity);
                ref var c3 = ref Manager.GetComponent<T3>(entity);
                ref var c4 = ref Manager.GetComponent<T4>(entity);
                action(ref c1, ref c2, ref c3, ref c4);
            }
        }

        public void ForEachParallel(QueryAction4<T1, T2, T3, T4> action)
        {
            Parallel.For(0, _activeEntities.Count, i =>
            {
                var entity = _activeEntities[i];
                ref var c1 = ref Manager.GetComponent<T1>(entity);
                ref var c2 = ref Manager.GetComponent<T2>(entity);
                ref var c3 = ref Manager.GetComponent<T3>(entity);
                ref var c4 = ref Manager.GetComponent<T4>(entity);
                action(ref c1, ref c2, ref c3, ref c4);
            });
        }
    }

    public class Query<T1, T2, T3, T4, T5> : EntityQueryBase 
        where T1 : struct, IComponentData
        where T2 : struct, IComponentData
        where T3 : struct, IComponentData
        where T4 : struct, IComponentData
        where T5 : struct, IComponentData
    {
        public Query(EntityManager manager)
        {
            Aspect = new EntityAspect().All(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));
            manager.RegisterQuery(this);
        }

        public void ForEach(QueryAction5<T1, T2, T3, T4, T5> action)
        {
            for (int i = _activeEntities.Count - 1; i >= 0; i--)
            {
                var entity = _activeEntities[i];
                ref var c1 = ref Manager.GetComponent<T1>(entity);
                ref var c2 = ref Manager.GetComponent<T2>(entity);
                ref var c3 = ref Manager.GetComponent<T3>(entity);
                ref var c4 = ref Manager.GetComponent<T4>(entity);
                ref var c5 = ref Manager.GetComponent<T5>(entity);
                action(ref c1, ref c2, ref c3, ref c4, ref c5);
            }
        }

        public void ForEachParallel(QueryAction5<T1, T2, T3, T4, T5> action)
        {
            Parallel.For(0, _activeEntities.Count, i =>
            {
                var entity = _activeEntities[i];
                ref var c1 = ref Manager.GetComponent<T1>(entity);
                ref var c2 = ref Manager.GetComponent<T2>(entity);
                ref var c3 = ref Manager.GetComponent<T3>(entity);
                ref var c4 = ref Manager.GetComponent<T4>(entity);
                ref var c5 = ref Manager.GetComponent<T5>(entity);
                action(ref c1, ref c2, ref c3, ref c4, ref c5);
            });
        }
    }

    public class Query<T1, T2, T3, T4, T5, T6> : EntityQueryBase 
        where T1 : struct, IComponentData
        where T2 : struct, IComponentData
        where T3 : struct, IComponentData
        where T4 : struct, IComponentData
        where T5 : struct, IComponentData
        where T6 : struct, IComponentData
    {
        public Query(EntityManager manager)
        {
            Aspect = new EntityAspect().All(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6));
            manager.RegisterQuery(this);
        }

        public void ForEach(QueryAction6<T1, T2, T3, T4, T5, T6> action)
        {
            for (int i = _activeEntities.Count - 1; i >= 0; i--)
            {
                var entity = _activeEntities[i];
                ref var c1 = ref Manager.GetComponent<T1>(entity);
                ref var c2 = ref Manager.GetComponent<T2>(entity);
                ref var c3 = ref Manager.GetComponent<T3>(entity);
                ref var c4 = ref Manager.GetComponent<T4>(entity);
                ref var c5 = ref Manager.GetComponent<T5>(entity);
                ref var c6 = ref Manager.GetComponent<T6>(entity);
                action(ref c1, ref c2, ref c3, ref c4, ref c5, ref c6);
            }
        }

        public void ForEachParallel(QueryAction6<T1, T2, T3, T4, T5, T6> action)
        {
            Parallel.For(0, _activeEntities.Count, i =>
            {
                var entity = _activeEntities[i];
                ref var c1 = ref Manager.GetComponent<T1>(entity);
                ref var c2 = ref Manager.GetComponent<T2>(entity);
                ref var c3 = ref Manager.GetComponent<T3>(entity);
                ref var c4 = ref Manager.GetComponent<T4>(entity);
                ref var c5 = ref Manager.GetComponent<T5>(entity);
                ref var c6 = ref Manager.GetComponent<T6>(entity);
                action(ref c1, ref c2, ref c3, ref c4, ref c5, ref c6);
            });
        }
    }

    public class Query<T1, T2, T3, T4, T5, T6, T7> : EntityQueryBase 
        where T1 : struct, IComponentData
        where T2 : struct, IComponentData
        where T3 : struct, IComponentData
        where T4 : struct, IComponentData
        where T5 : struct, IComponentData
        where T6 : struct, IComponentData
        where T7 : struct, IComponentData
    {
        public Query(EntityManager manager)
        {
            Aspect = new EntityAspect().All(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7));
            manager.RegisterQuery(this);
        }

        public void ForEach(QueryAction7<T1, T2, T3, T4, T5, T6, T7> action)
        {
            for (int i = _activeEntities.Count - 1; i >= 0; i--)
            {
                var entity = _activeEntities[i];
                ref var c1 = ref Manager.GetComponent<T1>(entity);
                ref var c2 = ref Manager.GetComponent<T2>(entity);
                ref var c3 = ref Manager.GetComponent<T3>(entity);
                ref var c4 = ref Manager.GetComponent<T4>(entity);
                ref var c5 = ref Manager.GetComponent<T5>(entity);
                ref var c6 = ref Manager.GetComponent<T6>(entity);
                ref var c7 = ref Manager.GetComponent<T7>(entity);
                action(ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7);
            }
        }

        public void ForEachParallel(QueryAction7<T1, T2, T3, T4, T5, T6, T7> action)
        {
            Parallel.For(0, _activeEntities.Count, i =>
            {
                var entity = _activeEntities[i];
                ref var c1 = ref Manager.GetComponent<T1>(entity);
                ref var c2 = ref Manager.GetComponent<T2>(entity);
                ref var c3 = ref Manager.GetComponent<T3>(entity);
                ref var c4 = ref Manager.GetComponent<T4>(entity);
                ref var c5 = ref Manager.GetComponent<T5>(entity);
                ref var c6 = ref Manager.GetComponent<T6>(entity);
                ref var c7 = ref Manager.GetComponent<T7>(entity);
                action(ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7);
            });
        }
    }

    public class Query<T1, T2, T3, T4, T5, T6, T7, T8> : EntityQueryBase 
        where T1 : struct, IComponentData
        where T2 : struct, IComponentData
        where T3 : struct, IComponentData
        where T4 : struct, IComponentData
        where T5 : struct, IComponentData
        where T6 : struct, IComponentData
        where T7 : struct, IComponentData
        where T8 : struct, IComponentData
    {
        public Query(EntityManager manager)
        {
            Aspect = new EntityAspect().All(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8));
            manager.RegisterQuery(this);
        }

        public void ForEach(QueryAction8<T1, T2, T3, T4, T5, T6, T7, T8> action)
        {
            for (int i = _activeEntities.Count - 1; i >= 0; i--)
            {
                var entity = _activeEntities[i];
                ref var c1 = ref Manager.GetComponent<T1>(entity);
                ref var c2 = ref Manager.GetComponent<T2>(entity);
                ref var c3 = ref Manager.GetComponent<T3>(entity);
                ref var c4 = ref Manager.GetComponent<T4>(entity);
                ref var c5 = ref Manager.GetComponent<T5>(entity);
                ref var c6 = ref Manager.GetComponent<T6>(entity);
                ref var c7 = ref Manager.GetComponent<T7>(entity);
                ref var c8 = ref Manager.GetComponent<T8>(entity);
                action(ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref c8);
            }
        }

        public void ForEachParallel(QueryAction8<T1, T2, T3, T4, T5, T6, T7, T8> action)
        {
            Parallel.For(0, _activeEntities.Count, i =>
            {
                var entity = _activeEntities[i];
                ref var c1 = ref Manager.GetComponent<T1>(entity);
                ref var c2 = ref Manager.GetComponent<T2>(entity);
                ref var c3 = ref Manager.GetComponent<T3>(entity);
                ref var c4 = ref Manager.GetComponent<T4>(entity);
                ref var c5 = ref Manager.GetComponent<T5>(entity);
                ref var c6 = ref Manager.GetComponent<T6>(entity);
                ref var c7 = ref Manager.GetComponent<T7>(entity);
                ref var c8 = ref Manager.GetComponent<T8>(entity);
                action(ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref c8);
            });
        }
    }
}
