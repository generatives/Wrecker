using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.ECS
{
    public static class EntityExtensions
    {
        public static T GetOrCreate<T>(this Entity entity)
            where T : new()
        {
            if(entity.Has<T>())
            {
                return entity.Get<T>();
            }
            else
            {
                var component = new T();
                entity.Set(component);
                return component;
            }
        }

        public static T GetOrCreate<T>(this Entity entity, Func<Entity, T> constructor)
        {
            if (entity.Has<T>())
            {
                return entity.Get<T>();
            }
            else
            {
                var component = constructor(entity);
                entity.Set(component);
                return component;
            }
        }

        public static T GetOrCreate<T>(this Entity entity, Func<T> constructor)
        {
            if (entity.Has<T>())
            {
                return entity.Get<T>();
            }
            else
            {
                var component = constructor();
                entity.Set(component);
                return component;
            }
        }
    }
}
