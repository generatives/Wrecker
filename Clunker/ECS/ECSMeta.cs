using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clunker.ECS
{
    public class ECSMeta
    {
        private static Lazy<List<Type>> _componentTypes = new Lazy<List<Type>>(() =>
        {
            return (
                from a in AppDomain.CurrentDomain.GetAssemblies()
                from t in a.GetTypes()
                let attributes = t.GetCustomAttributes(typeof(ClunkerComponentAttribute), true)
                where attributes != null && attributes.Length > 0
                select t).ToList();
        });

        public static IEnumerable<Type> ComponentTypes => _componentTypes.Value;
    }
}
