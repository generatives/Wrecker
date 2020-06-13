using Clunker.ECS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid.Sdl2;

namespace Clunker.Graphics
{
    [ClunkerComponent]
    public struct Camera
    {
        public float Yaw;
        public float Pitch;
        public float Roll;
    }
}
