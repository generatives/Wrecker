using Clunker.Math;
using Clunker.SceneGraph;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.World
{
    public class Chunk : Component
    {
        public Vector3i Coordinates { get; private set; }

        public Chunk(Vector3i coordinates)
        {
            Coordinates = coordinates;
        }
    }
}
