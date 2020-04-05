using Clunker.Geometry;
using Collections.Pooled;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Voxels
{
    public struct ExposedVoxels
    {
        public PooledList<Vector3i> Exposed { get; set; }
    }
}
