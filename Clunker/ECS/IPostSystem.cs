using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.ECS
{
    public interface IPostSystem<T> : IDisposable
    {
        bool IsEnabled { get; set; }
        void PostUpdate(T state);
    }
}
