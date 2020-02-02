using Clunker.Geometry;
using Clunker.WorldSpace;
using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Voxels
{
    public class VoxelSpace : IDisposable
    {
        private int _gridLength;
        private int _voxelSize;
        private Dictionary<Vector3i, Entity> _grids;
        private EntitySet _gridSet;

        public VoxelSpace(World world, int gridLength, int voxelSize)
        {
            _gridLength = gridLength;
            _voxelSize = voxelSize;

            _grids = new Dictionary<Vector3i, Entity>();
            _gridSet = world.GetEntities().With<Chunk>().With<VoxelGrid>().AsSet();
            _gridSet.EntityAdded += _gridSet_EntityAdded;
            _gridSet.EntityRemoved += _gridSet_EntityRemoved;
        }

        private void _gridSet_EntityAdded(in Entity message)
        {
            ref var chunk = ref message.Get<Chunk>();
            _grids[chunk.Coordinates] = message;
        }

        private void _gridSet_EntityRemoved(in Entity message)
        {
            ref var chunk = ref message.Get<Chunk>();
            _grids.Remove(chunk.Coordinates);
        }

        public Vector3i GetSpaceIndexFromPosition(Vector3 position)
        {
            return new Vector3i(
                (int)Math.Floor(position.X / _voxelSize),
                (int)Math.Floor(position.Y / _voxelSize),
                (int)Math.Floor(position.Z / _voxelSize));
        }

        public Vector3i GetGridIndexFromPosition(Vector3 position)
        {
            var spaceIndex = GetSpaceIndexFromPosition(position);

            return new Vector3i(
                (int)Math.Floor((float)spaceIndex.X / _gridLength),
                (int)Math.Floor((float)spaceIndex.Y / _gridLength),
                (int)Math.Floor((float)spaceIndex.Z / _gridLength));
        }

        public Vector3i GetSpaceIndexFromGridIndex(Vector3i gridsIndex, Vector3i voxelIndex)
        {
            return gridsIndex * _gridLength + voxelIndex;
        }

        public void Dispose()
        {
            _gridSet.Dispose();
        }
    }
}
