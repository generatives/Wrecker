using Clunker.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Voxels
{
    public interface IVoxels
    {
        public float VoxelSize { get; }
        void SetVoxel(Vector3i index, Voxel voxel);
        Voxel? GetVoxel(Vector3i index);
    }
}
