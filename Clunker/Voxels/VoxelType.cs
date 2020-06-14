using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Voxels
{
    public struct VoxelType
    {
        public string Name;
        public bool Transparent;
        public Vector2 TopTexCoords;
        public Vector2 BottomTexCoords;
        public Vector2 NorthTexCoords;
        public Vector2 SouthTexCoords;
        public Vector2 EastTexCoords;
        public Vector2 WestTexCoords;

        public VoxelType(string name, bool transparent, Vector2 topTexCoords, Vector2 bottomTexCoords, Vector2 sideTexCoords)
        {
            Name = name;
            Transparent = transparent;
            TopTexCoords = topTexCoords;
            BottomTexCoords = bottomTexCoords;
            NorthTexCoords = sideTexCoords;
            SouthTexCoords = sideTexCoords;
            EastTexCoords = sideTexCoords;
            WestTexCoords = sideTexCoords;
        }

        public VoxelType(string name, bool transparent, Vector2 topTexCoords, Vector2 bottomTexCoords, Vector2 northTexCoords, Vector2 southTexCoords, Vector2 eastTexCoords, Vector2 westTexCoords)
        {
            Name = name;
            Transparent = transparent;
            TopTexCoords = topTexCoords;
            BottomTexCoords = bottomTexCoords;
            NorthTexCoords = northTexCoords;
            SouthTexCoords = southTexCoords;
            EastTexCoords = eastTexCoords;
            WestTexCoords = westTexCoords;
        }
    }
}
