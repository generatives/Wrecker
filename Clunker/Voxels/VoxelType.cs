using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Voxels
{
    public struct VoxelType
    {
        public Vector2 TopTexCoords;
        public Vector2 BottomTexCoords;
        public Vector2 NorthTexCoords;
        public Vector2 SouthTexCoords;
        public Vector2 EastTexCoords;
        public Vector2 WestTexCoords;

        public VoxelType(Vector2 topTexCoords, Vector2 bottomTexCoords, Vector2 sideTexCoords)
        {
            TopTexCoords = topTexCoords;
            BottomTexCoords = bottomTexCoords;
            NorthTexCoords = sideTexCoords;
            SouthTexCoords = sideTexCoords;
            EastTexCoords = sideTexCoords;
            WestTexCoords = sideTexCoords;
        }

        public VoxelType(Vector2 topTexCoords, Vector2 bottomTexCoords, Vector2 northTexCoords, Vector2 southTexCoords, Vector2 eastTexCoords, Vector2 westTexCoords)
        {
            TopTexCoords = topTexCoords;
            BottomTexCoords = bottomTexCoords;
            NorthTexCoords = northTexCoords;
            SouthTexCoords = southTexCoords;
            EastTexCoords = eastTexCoords;
            WestTexCoords = westTexCoords;
        }
    }
}
