using Clunker.SceneGraph.ComponentsInterfaces;
using Clunker.SceneGraph.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clunker.SceneGraph
{
    public partial class GameObject
    {
        public Transform Transform { get => GetComponent<Transform>(); }

        public T GetComponent<T>()
        {
            return (T)GetComponent(typeof(T));
        }

        public bool HasComponent<T>()
        {
            return HasComponent(typeof(T));
        }
    }
}
