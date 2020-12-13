using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.ECS
{
    public interface IPreSystem<T> : IDisposable
    {
        bool IsEnabled { get; set; }
        void PreUpdate(T state);
    }
}
