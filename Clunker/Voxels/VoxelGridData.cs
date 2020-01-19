using Clunker.Geometry;
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
    public class VoxelGridData : IEnumerable<(Vector3, Voxel)>
    {
        public event Action Changed;

        private Voxel[] _voxels;
        public float VoxelSize { get; private set; }
        public int GridSize { get; private set; }
        public bool HasExistingVoxels => this.Any(v => v.Item2.Exists);

        public VoxelGridData(int gridSize, float voxelSize)
        {
            _voxels = new Voxel[gridSize * gridSize * gridSize];
            GridSize = gridSize;

            VoxelSize = voxelSize;
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
                return _voxels[x + GridSize * (y + GridSize * z)];
            }
            set
            {
                _voxels[x + GridSize * (y + GridSize * z)] = value;
                Changed?.Invoke();
            }
        }

        public bool Exists(Vector3i index) => Exists(index.X, index.Y, index.Z);
        public bool Exists(int x, int y, int z)
        {
            return WithinBounds(x, y, z) && this[x, y, z].Exists;
        }

        public bool WithinBounds(Vector3i index) => WithinBounds(index.X, index.Y, index.Z);
        public bool WithinBounds(int x, int y, int z)
        {
            return x >= 0 && x < GridSize &&
                y >= 0 && y < GridSize &&
                z >= 0 && z < GridSize;
        }

        public bool SetVoxel(int x, int y, int z, Voxel voxel) => SetVoxel(new Vector3i(x, y, z), voxel);
        public bool SetVoxel(Vector3i index, Voxel voxel)
        {
            if (WithinBounds(index))
            {
                this[index] = voxel;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void FindExposedSides(Action<Voxel, int, int, int, VoxelSide> sideProcessor)
        {
            for(int x = 0; x < GridSize; x++)
                for (int y = 0; y < GridSize; y++)
                    for (int z = 0; z < GridSize; z++)
                    {
                        Voxel voxel = this[x, y, z];
                        if (voxel.Exists)
                        {
                            if (!Exists(x, y - 1, z))
                            {
                                sideProcessor(voxel, x, y, z, VoxelSide.BOTTOM);
                            }

                            if (!Exists(x + 1, y, z))
                            {
                                sideProcessor(voxel, x, y, z, VoxelSide.EAST);
                            }

                            if (!Exists(x - 1, y, z))
                            {
                                sideProcessor(voxel, x, y, z, VoxelSide.WEST);
                            }

                            if (!Exists(x, y + 1, z))
                            {
                                sideProcessor(voxel, x, y, z, VoxelSide.TOP);
                            }

                            if (!Exists(x, y, z - 1))
                            {
                                sideProcessor(voxel, x, y, z, VoxelSide.NORTH);
                            }

                            if (!Exists(x, y, z + 1))
                            {
                                sideProcessor(voxel, x, y, z, VoxelSide.SOUTH);
                            }
                        }
                    }
        }

        public void FindExposedBlocks(Action<Voxel, int, int, int> blockProcessor)
        {
            for (int x = 0; x < GridSize; x++)
                for (int y = 0; y < GridSize; y++)
                    for (int z = 0; z < GridSize; z++)
                    {
                        Voxel voxel = this[x, y, z];
                        if (voxel.Exists)
                        {
                            if (!Exists(x, y - 1, z) ||
                                !Exists(x + 1, y, z) ||
                                !Exists(x - 1, y, z) ||
                                !Exists(x, y + 1, z) ||
                                !Exists(x, y, z - 1) ||
                                !Exists(x, y, z + 1))
                            {
                                blockProcessor(voxel, x, y, z);
                            }
                        }
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
