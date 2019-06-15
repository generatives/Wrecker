using Clunker.Math;
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
    public class VoxelSpaceData : IEnumerable<(Vector3, Voxel)>
    {
        private Voxel[,,] _voxels;
        public float VoxelSize { get; private set; }
        public int XLength { get; private set; }
        public int YLength { get; private set; }
        public int ZLength { get; private set; }
        public event Action Changed;

        public VoxelSpaceData(int xLength, int yLength, int zLength, float voxelSize)
        {
            _voxels = new Voxel[xLength, yLength, zLength];
            XLength = xLength;
            YLength = yLength;
            ZLength = zLength;

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
                return _voxels[x, y, z];
            }
            set
            {
                _voxels[x, y, z] = value;
                Changed?.Invoke();
            }
        }

        public bool WithinBounds(int x, int y, int z)
        {
            return x >= 0 && x < XLength &&
                y >= 0 && y < YLength &&
                z >= 0 && z < ZLength;
        }

        public Voxel Get(int x, int y, int z)
        {
            return _voxels[x, y, z];
        }

        public void FindExposedSides(Action<Voxel, int, int, int, VoxelSide> sideProcessor)
        {
            for(int x = 0; x < XLength; x++)
                for (int y = 0; y < YLength; y++)
                    for (int z = 0; z < ZLength; z++)
                    {
                        Voxel voxel = this[x, y, z];
                        if (voxel.Exists)
                        {
                            if (!WithinBounds(x, y - 1, z) || !this[x, y - 1, z].Exists)
                            {
                                sideProcessor(voxel, x, y, z, VoxelSide.BOTTOM);
                            }

                            if (!WithinBounds(x + 1, y, z) || !this[x + 1, y, z].Exists)
                            {
                                sideProcessor(voxel, x, y, z, VoxelSide.EAST);
                            }

                            if (!WithinBounds(x - 1, y, z) || !this[x - 1, y, z].Exists)
                            {
                                sideProcessor(voxel, x, y, z, VoxelSide.WEST);
                            }

                            if (!WithinBounds(x, y + 1, z) || !this[x, y + 1, z].Exists)
                            {
                                sideProcessor(voxel, x, y, z, VoxelSide.TOP);
                            }

                            if (!WithinBounds(x, y, z - 1) || !this[x, y, z - 1].Exists)
                            {
                                sideProcessor(voxel, x, y, z, VoxelSide.NORTH);
                            }

                            if (!WithinBounds(x, y, z + 1) || !this[x, y, z + 1].Exists)
                            {
                                sideProcessor(voxel, x, y, z, VoxelSide.SOUTH);
                            }
                        }
                    }
        }

        public IEnumerator<(Vector3, Voxel)> GetEnumerator()
        {
            for (int x = 0; x < XLength; x++)
                for (int y = 0; y < YLength; y++)
                    for (int z = 0; z < ZLength; z++)
                    {
                        yield return (new Vector3(x, y, z), _voxels[x, y, z]);
                    }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
