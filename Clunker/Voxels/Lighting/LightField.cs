using Clunker.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Voxels.Lighting
{
    public class LightField
    {
        public byte[] Lights { get; private set; }
        public int GridSize { get; private set; }

        public LightField(int gridSize)
        {
            GridSize = gridSize;
            Lights = new byte[GridSize * GridSize * GridSize];
        }

        public byte this[Vector3i index]
        {
            get
            {
                return this[index.X, index.Y, index.Z];
            }
            set
            {
                this[index.X, index.Y, index.Z] = value;
            }
        }

        public byte this[int x, int y, int z]
        {
            get
            {
                return Lights[x + GridSize * (y + GridSize * z)];
            }
            set
            {
                Lights[x + GridSize * (y + GridSize * z)] = value;
            }
        }
    }
}
