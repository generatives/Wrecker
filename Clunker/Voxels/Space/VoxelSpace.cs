using Clunker.Geometry;
using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Voxels.Space
{
    public struct VoxelSpace
    {
        public int GridSize { get; set; }
        public float VoxelSize { get; set; }
        public Dictionary<Vector3i, Entity> Members { get; set; }

        public Vector3i GetGridIndexFromLocalPosition(Vector3 position)
        {
            var spaceIndex = new Vector3i(
                (int)Math.Floor(position.X / VoxelSize),
                (int)Math.Floor(position.Y / VoxelSize),
                (int)Math.Floor(position.Z / VoxelSize));

            return new Vector3i(
                (int)Math.Floor((float)spaceIndex.X / GridSize),
                (int)Math.Floor((float)spaceIndex.Y / GridSize),
                (int)Math.Floor((float)spaceIndex.Z / GridSize));
        }

        public Vector3i GetSpaceIndexFromVoxelIndex(Vector3i memberIndex, Vector3i voxelIndex)
        {
            return memberIndex * GridSize + voxelIndex;
        }
    }
}
