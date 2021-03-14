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
    public class VoxelSpace : IEnumerable<KeyValuePair<Vector3i, Entity>>
    {
        public int GridSize { get; private set; }
        public float VoxelSize { get; private set; }
        private Dictionary<Vector3i, Entity> _members { get; set; }
        public Entity Self { get; private set; }

        public VoxelSpace(int gridSize, float voxelSize, Entity self)
        {
            GridSize = gridSize;
            VoxelSize = voxelSize;
            _members = new Dictionary<Vector3i, Entity>();
            Self = self;
        }

        public VoxelSpace(int gridSize, float voxelSize, List<(Vector3i, Entity)> members, Entity self) : this(gridSize, voxelSize, self)
        {
            foreach(var (index, entity) in members)
            {
                this[index] = entity;
            }
        }

        public Entity this[Vector3i memberIndex]
        {
            get
            {
                return _members[memberIndex];
            }
            set
            {
                if(value == null)
                {
                    Remove(memberIndex);
                }
                else
                {
                    _members[memberIndex] = value;
                    var voxelGrid = value.Get<VoxelGrid>();

                    foreach (var (side, offset, inverseSide) in GeometricUtils.SixNeighbourSides)
                    {
                        var otherIndex = memberIndex + offset;
                        if (_members.ContainsKey(otherIndex))
                        {
                            var member = _members[otherIndex];
                            var otherVoxels = member.Get<VoxelGrid>();
                            voxelGrid.NeighborGrids[(int)side] = otherVoxels;
                            otherVoxels.NeighborGrids[(int)inverseSide] = voxelGrid;
                            member.NotifyChanged<VoxelGrid>();
                        }
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
            if(_members.ContainsKey(memberIndex))
            {
                foreach (var (side, offset, inverseSide) in GeometricUtils.SixNeighbourSides)
                {
                    var otherIndex = memberIndex + offset;
                    if (_members.ContainsKey(otherIndex))
                    {
                        var otherMember = _members[otherIndex];
                        var otherVoxels = otherMember.Get<VoxelGrid>();
                        otherVoxels.NeighborGrids[(int)inverseSide] = null;
                        otherMember.NotifyChanged<VoxelGrid>();
                    }
                }

                _members.Remove(memberIndex);
            }

            foreach (var offset in GeometricUtils.SixNeighbours)
            {
                var otherIndex = memberIndex + offset;
                TryNotifyNeighbor(otherIndex);
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

                var previousVoxel = grid[voxelIndex];

                grid.SetVoxel(voxelIndex, voxel);
                member.Set(grid);
                member.World.Publish(new VoxelChanged() { Entity = member, VoxelIndex = voxelIndex, PreviousValue = previousVoxel, Value = voxel });

                if (voxelIndex.X == 0)
                    TryNotifyNeighbor(memberIndex - Vector3i.UnitX);
                if (voxelIndex.X == GridSize - 1)
                    TryNotifyNeighbor(memberIndex + Vector3i.UnitX);
                if (voxelIndex.Y == 0)
                    TryNotifyNeighbor(memberIndex - Vector3i.UnitY);
                if (voxelIndex.Y == GridSize - 1)
                    TryNotifyNeighbor(memberIndex + Vector3i.UnitY);
                if (voxelIndex.Z == 0)
                    TryNotifyNeighbor(memberIndex - Vector3i.UnitZ);
                if (voxelIndex.Z == GridSize - 1)
                    TryNotifyNeighbor(memberIndex + Vector3i.UnitZ);
            }
        }

        public byte? GetLight(Vector3i index)
        {
            var memberIndex = new Vector3i(
                (int)Math.Floor((float)index.X / GridSize),
                (int)Math.Floor((float)index.Y / GridSize),
                (int)Math.Floor((float)index.Z / GridSize));

            var voxelIndex = new Vector3i(
                index.X - memberIndex.X * GridSize,
                index.Y - memberIndex.Y * GridSize,
                index.Z - memberIndex.Z * GridSize);

            if (_members.ContainsKey(memberIndex))
            {
                var grid = _members[memberIndex].Get<VoxelGrid>();

                return grid.GetLight(voxelIndex);
            }
            else
            {
                return null;
            }
        }

        private void TryNotifyNeighbor(Vector3i index)
        {
            if (_members.ContainsKey(index))
            {
                _members[index].NotifyChanged<VoxelGrid>();
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
