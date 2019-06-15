using Clunker.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.SceneGraph.ComponentsInterfaces
{
    public interface IRenderUpdateable
    {
        void RenderUpdate(float delta);
    }
}
