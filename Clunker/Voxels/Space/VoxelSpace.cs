using Clunker.ECS;
using Clunker.Geometry;
using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Voxels.Space
{
    [ClunkerComponent]
    public struct VoxelSpace : IVoxels
    {
        public int GridSize { get; set; }
        public float VoxelSize { get; set; }
        public Dictionary<Vector3i, Entity> Members { get; set; }

        public Voxel? GetVoxel(Vector3i index)
        {
            var memberIndex = new Vector3i(
                (int)Math.Floor((float)index.X / GridSize),
                (int)Math.Floor((float)index.Y / GridSize),
                (int)Math.Floor((float)index.Z / GridSize));

            var voxelIndex = new Vector3i(
                index.X - memberIndex.X * GridSize,
                index.Y - memberIndex.Y * GridSize,
                index.Z - memberIndex.Z * GridSize);

            if(Members.ContainsKey(memberIndex))
            {
                var grid = Members[memberIndex].Get<VoxelGrid>();

                return grid[voxelIndex];
            }
            else
            {
                return null;
            }
        }

        public void SetVoxel(Vector3i index, Voxel voxel)
        {
            var memberIndex = new Vector3i(
                (int)Math.Floor((float)index.X / GridSize),
                (int)Math.Floor((float)index.Y / GridSize),
                (int)Math.Floor((float)index.Z / GridSize));

            var voxelIndex = new Vector3i(
                index.X - memberIndex.X * GridSize,
                index.Y - memberIndex.Y * GridSize,
                index.Z - memberIndex.Z * GridSize);

            if(Members.ContainsKey(memberIndex))
            {
                var member = Members[memberIndex];
                var grid = member.Get<VoxelGrid>();

                grid.SetVoxel(voxelIndex, voxel);
                member.Set(grid);
            }
        }

        public Vector3i GetMemberIndexFromLocalPosition(Vector3 position)
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

        public Vector3i GetMemberIndexFromSpaceIndex(Vector3i spaceIndex)
        {
            return new Vector3i(
                (int)Math.Floor((float)spaceIndex.X / GridSize),
                (int)Math.Floor((float)spaceIndex.Y / GridSize),
                (int)Math.Floor((float)spaceIndex.Z / GridSize));
        }

        public Vector3i GetVoxelIndexFromSpaceIndex(Vector3i memberIndex, Vector3i spaceIndex)
        {
            return new Vector3i(
                spaceIndex.X - memberIndex.X * GridSize,
                spaceIndex.Y - memberIndex.Y * GridSize,
                spaceIndex.Z - memberIndex.Z * GridSize);
        }

        public Vector3i GetSpaceIndexFromVoxelIndex(Vector3i memberIndex, Vector3i voxelIndex)
        {
            return memberIndex * GridSize + voxelIndex;
        }
    }
}
