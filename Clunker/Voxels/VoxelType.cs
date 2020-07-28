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
        public Vector2[] TextureCoords;
        public Vector2 TopTexCoords => TextureCoords[0];
        public Vector2 BottomTexCoords => TextureCoords[1];
        public Vector2 NorthTexCoords => TextureCoords[2];
        public Vector2 SouthTexCoords => TextureCoords[3];
        public Vector2 EastTexCoords => TextureCoords[4];
        public Vector2 WestTexCoords => TextureCoords[5];

        public VoxelType(string name, bool transparent, Vector2 topTexCoords, Vector2 bottomTexCoords, Vector2 sideTexCoords)
        {
            Name = name;
            Transparent = transparent;
            TextureCoords = new Vector2[]
            {
                topTexCoords,
                bottomTexCoords,
                sideTexCoords,
                sideTexCoords,
                sideTexCoords,
                sideTexCoords
            };
        }

        public VoxelType(string name, bool transparent, Vector2 topTexCoords, Vector2 bottomTexCoords, Vector2 northTexCoords, Vector2 southTexCoords, Vector2 eastTexCoords, Vector2 westTexCoords)
        {
            Name = name;
            Transparent = transparent;
            TextureCoords = new Vector2[]
            {
                topTexCoords,
                bottomTexCoords,
                northTexCoords,
                southTexCoords,
                eastTexCoords,
                westTexCoords
            };
        }
    }
}
