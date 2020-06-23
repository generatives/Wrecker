using Clunker.ECS;
using Clunker.Geometry;
using DefaultEcs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Clunker.Voxels
{
    [ClunkerComponent]
    public struct VoxelGrid : IEnumerable<(Vector3, Voxel)>
    {
        public Entity VoxelSpace { get; set; }
        public Vector3i MemberIndex { get; set; }
        public Voxel[] Voxels { get; private set; }
        public float VoxelSize { get; private set; }
        public int GridSize { get; private set; }
        public int CoordinateDimSize { get; private set; }
        public int CoordinateDimSize2x { get; private set; }
        public bool HasExistingVoxels => this.Any(v => v.Item2.Exists);

        public VoxelGrid(int gridSize, float voxelSize, Entity voxelSpace, Vector3i spaceIndex) : this(voxelSize, gridSize, voxelSpace, spaceIndex, new Voxel[gridSize * gridSize * gridSize])
        {
        }

        public VoxelGrid(float voxelSize, int gridSize, Entity voxelSpace, Vector3i spaceIndex, Voxel[] voxels)
        {
            Debug.Assert(voxels.Length == gridSize * gridSize * gridSize, "Voxel array must be sized to gridSize");

            VoxelSize = voxelSize;
            GridSize = gridSize;
            VoxelSpace = voxelSpace;
            MemberIndex = spaceIndex;
            Voxels = voxels;

            CoordinateDimSize = (int)Math.Log(GridSize, 2);
            CoordinateDimSize2x = CoordinateDimSize * 2;
        }

        public Voxel this[Vector3i index]
        {
            get
            {
                return this[index.X, index.Y, index.Z];
            }
            set
            {
                this[index.X, index.Y, index.Z] = value;
            }
        }

        public Voxel this[int x, int y, int z]
        {
            get
            {
                return Voxels[(z << CoordinateDimSize2x) + (y << CoordinateDimSize) + x];
            }
            set
            {
                Voxels[(z << CoordinateDimSize2x) + (y << CoordinateDimSize) + x] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AsFlatIndex(Vector3i coordinate) => AsFlatIndex(coordinate.X, coordinate.Y, coordinate.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AsFlatIndex(int x, int y, int z)
        {
            return (z << CoordinateDimSize2x) + (y << CoordinateDimSize) + x;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3i AsCoordinate(int flatIndex)
        {
            return new Vector3i(flatIndex & GridSize - 1, (flatIndex >> CoordinateDimSize) & GridSize - 1, (flatIndex >> CoordinateDimSize2x) & GridSize - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Exists(Vector3i index) => Exists(index.X, index.Y, index.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Exists(int x, int y, int z)
        {
            return ContainsIndex(x, y, z) && this[x, y, z].Exists;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsIndex(Vector3i index) => ContainsIndex(index.X, index.Y, index.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsIndex(int x, int y, int z)
        {
            return x >= 0 && x < GridSize &&
                y >= 0 && y < GridSize &&
                z >= 0 && z < GridSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsFlatIndex(int flatIndex)
        {
            return flatIndex >= 0 && flatIndex < Voxels.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetVoxel(int x, int y, int z, Voxel voxel) => SetVoxel(new Vector3i(x, y, z), voxel);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetVoxel(Vector3i index, Voxel voxel)
        {
            if (ContainsIndex(index))
            {
                this[index] = voxel;
                return true;
            }
            else
            {
                return false;
            }
        }

        public IEnumerator<(Vector3, Voxel)> GetEnumerator()
        {
            for (int x = 0; x < GridSize; x++)
                for (int y = 0; y < GridSize; y++)
                    for (int z = 0; z < GridSize; z++)
                    {
                        yield return (new Vector3(x, y, z), this[x, y, z]);
                    }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
