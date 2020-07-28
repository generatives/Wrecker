using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.ECS
{
    public abstract class UpdateSystem<T> : ISystem<T>
    {
        public bool IsEnabled { get; set; } = true;

        public abstract void Update(T state);

        public virtual void Dispose() { }
    }
}
