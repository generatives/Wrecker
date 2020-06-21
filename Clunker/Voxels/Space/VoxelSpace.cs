using Clunker.ECS;
using Clunker.Geometry;
using Clunker.Utilties;
using DefaultEcs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Clunker.Voxels.Space
{
    [ClunkerComponent]
    public struct VoxelSpace : IEnumerable<KeyValuePair<Vector3i, Entity>>
    {
        public int GridSize { get; set; }
        public float VoxelSize { get; set; }
        private Dictionary<Vector3i, Entity> _members { get; set; }

        public VoxelSpace(int gridSize, float voxelSize)
        {
            GridSize = gridSize;
            VoxelSize = voxelSize;
            _members = new Dictionary<Vector3i, Entity>();
        }

        public VoxelSpace(int gridSize, float voxelSize, Dictionary<Vector3i, Entity> members)
        {
            GridSize = gridSize;
            VoxelSize = voxelSize;
            _members = members;
        }

        public Entity this[Vector3i memberIndex]
        {
            get
            {
                return _members[memberIndex];
            }
            set
            {
                _members[memberIndex] = value;
                foreach(var offset in GeometricIterators.SixNeighbours)
                {
                    var otherIndex = memberIndex + offset;
                    if (otherIndex != memberIndex && _members.ContainsKey(otherIndex))
                    {
                        var member = _members[otherIndex];
                        member.NotifyChanged<VoxelGrid>();
                    }
                }
            }
        }

        public bool ContainsMember(Vector3i memberIndex)
        {
            return _members.ContainsKey(memberIndex);
        }

        public void Remove(Vector3i memberIndex)
        {
            _members.Remove(memberIndex);
            foreach (var offset in GeometricIterators.SixNeighbours)
            {
                var otherIndex = memberIndex + offset;
                if (otherIndex != memberIndex && _members.ContainsKey(otherIndex))
                {
                    var member = _members[otherIndex];
                    member.NotifyChanged<VoxelGrid>();
                }
            }
        }

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

            if(_members.ContainsKey(memberIndex))
            {
                var grid = _members[memberIndex].Get<VoxelGrid>();

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

            if(_members.ContainsKey(memberIndex))
            {
                var member = _members[memberIndex];
                var grid = member.Get<VoxelGrid>();

                grid.SetVoxel(voxelIndex, voxel);
                member.Set(grid);

                foreach (var offset in GeometricIterators.SixNeighbours)
                {
                    var otherIndex = memberIndex + offset;
                    if (otherIndex != memberIndex && _members.ContainsKey(otherIndex))
                    {
                        _members[otherIndex].NotifyChanged<VoxelGrid>();
                    }
                }
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

        public IEnumerator<KeyValuePair<Vector3i, Entity>> GetEnumerator()
        {
            return _members.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _members.GetEnumerator();
        }
    }
}
