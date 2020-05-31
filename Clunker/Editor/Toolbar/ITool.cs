using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Editor.Toolbar
{
    public class ITool
    {
        public virtual string Name { get; }
        public virtual void Selected() { }
        public virtual void Run() { }
        public virtual void UnSelected() { }
    }
}
