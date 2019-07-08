using Clunker.Graphics;
using Clunker.Graphics.Materials;
using Clunker.Math;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentsInterfaces;
using Clunker.Voxels;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Clunker.Voxels
{
    public class VoxelSpace : Component
    {
        public VoxelSpaceData Data { get; private set; }

        public event Action VoxelsChanged;
        private bool _requestedVoxelsChanged;

        public VoxelSpace(VoxelSpaceData voxels)
        {
            Data = voxels;
            Data.Changed += Data_Changed;
        }

        private void Data_Changed()
        {
            if (!_requestedVoxelsChanged && VoxelsChanged != null)
            {
                this.EnqueueFrameJob(StartVoxelsChanged);
                _requestedVoxelsChanged = true;
            }
        }

        private void StartVoxelsChanged()
        {
            VoxelsChanged?.Invoke();
            _requestedVoxelsChanged = false;
        }

        public Vector3i GetVoxelIndex(Vector3 worldPosition)
        {
            var localPosition = GameObject.Transform.GetLocal(worldPosition);
            var voxelPosition = localPosition / Data.VoxelSize;
            return new Vector3i((int)voxelPosition.X, (int)voxelPosition.Y, (int)voxelPosition.Z);
        }
    }
}
