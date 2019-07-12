using Clunker.Input;
using Clunker.Math;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentsInterfaces;
using Clunker.Voxels;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Tooling
{
    public abstract class ClickEditingTool : Component, IUpdateable
    {
        public abstract void DrawMenu();
        public abstract void DoAction();

        public void Update(float time)
        {
            DrawMenu();
            if(InputTracker.LockMouse && InputTracker.WasMouseButtonDowned(Veldrid.MouseButton.Left))
            {
                DoAction();
            }
        }
    }
}
