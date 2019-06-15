using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Voxels
{
    public struct VoxelType
    {
        public Vector2 TopTexCoords;
        public Vector2 SideTexCoords;
        public Vector2 BottomTexCoords;

        public VoxelType(Vector2 topTexCoords, Vector2 sideTexCoords, Vector2 bottomTexCoords)
        {
            TopTexCoords = topTexCoords;
            SideTexCoords = sideTexCoords;
            BottomTexCoords = bottomTexCoords;
        }
    }
}
