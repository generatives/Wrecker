using Clunker.Math;
using Clunker.SceneGraph;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Voxels
{
    public class VoxelEntity : Component
    {
        public Vector3i Index { get; internal set; }
        public VoxelSpace Space { get; internal set; }
        public Voxel Voxel { get; internal set; }
    }
}
