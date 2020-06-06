using Clunker.ECS;
using Clunker.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.WorldSpace
{
    [ClunkerComponent]
    public struct Chunk
    {
        public Vector3i Coordinates;
    }
}
