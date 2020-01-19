using Clunker.Input;
using Clunker.Geometry;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
using Clunker.Voxels;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Tooling
{
    public abstract class Tool : Component
    {
        public abstract void BuildMenu();
    }
}
