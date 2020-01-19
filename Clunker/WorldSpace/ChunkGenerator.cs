using Clunker.Graphics;
using Clunker.Geometry;
using Clunker.Voxels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Numerics;
using DefaultEcs;
using Clunker.Core;
using DefaultEcs.Command;

namespace Clunker.WorldSpace
{
    public class ChunkGenerator
    {
        private MaterialInstance _materialInstance;
        private int _chunkSize;
        private int _voxelSize;
        private FastNoise _noise;

        public ChunkGenerator(MaterialInstance materialInstance, int chunkSize, int voxelSize)
        {
            _materialInstance = materialInstance;
            _chunkSize = chunkSize;
            _voxelSize = voxelSize;
            _noise = new FastNoise(DateTime.Now.Second);
            _noise.SetFrequency(0.08f);
        }

        public void GenerateChunk(EntityRecord entity, Vector3i coordinates)
        {
            var random = new Random((coordinates.X << 20) ^ (coordinates.Y << 10) ^ (coordinates.Z));
            var voxelSpaceData = new VoxelGridData(_chunkSize, _voxelSize);

            GenerateSpheres(voxelSpaceData, random);
            //JoinVoxels(voxels);
            SplatterHoles(voxelSpaceData, random);

            //for (int x = 0; x < _chunkSize; x++)
            //    for (int y = 0; y < _chunkSize; y++)
            //        for (int z = 0; z < _chunkSize; z++)
            //        {
            //            var planetPosition = new Vector3(0, 0, -1000);
            //            var planetSize = 750;
            //            var voxelPosition = new Vector3(coordinates.X * _chunkSize + x, coordinates.Y * _chunkSize + y, coordinates.Z * _chunkSize + z);
            //            voxelSpaceData[x, y, z] = new Voxel() { Exists = Vector3.Distance(planetPosition, voxelPosition) < planetSize };

            //            voxelSpaceData[x, y, z] = new Voxel() { Exists = _noise.GetPerlin(coordinates.X * _chunkSize + x, coordinates.Y * _chunkSize + y, coordinates.Z * _chunkSize + z) > 0f };
            //        }

            entity.Set(_materialInstance);

            entity.Set(voxelSpaceData);

            var transform = new Transform();
            transform.Position = new Vector3(coordinates.X * _chunkSize * _voxelSize, coordinates.Y * _chunkSize * _voxelSize, coordinates.Z * _chunkSize * _voxelSize);
            entity.Set(transform);
        }

        public void GenerateSpheres(VoxelGridData voxels, Random random)
        {
            var numAstroids = random.Next(3, 10);
            var locations = new (Vector3i, int)[numAstroids];
            for (byte a = 0; a < numAstroids; a++)
            {
                int r = random.Next(2, 4);
                int aX = random.Next(r, _chunkSize - r);
                int aY = random.Next(r, _chunkSize - r);
                int aZ = random.Next(r, _chunkSize - r);
                locations[a] = (new Vector3i(aX, aY, aZ), r);
            }

            for (int x = 0; x < _chunkSize; x++)
                for (int y = 0; y < _chunkSize; y++)
                    for (int z = 0; z < _chunkSize; z++)
                    {
                        var strength = 0f;
                        for (byte a = 0; a < numAstroids; a++)
                        {
                            var (location, radius) = locations[a];
                            var rSq = ((x - location.X) * (x - location.X)) + ((y - location.Y) * (y - location.Y)) + ((z - location.Z) * (z - location.Z));
                            if(rSq == 0)
                            {
                                strength = 500;
                                break;
                            }
                            else
                            {
                                strength += (float)(radius * radius) / rSq;
                            }
                        }
                        voxels[x, y, z] = new Voxel() { Exists = strength > 0.5f };
                    }
        }

        public void SplatterHoles(VoxelGridData voxels, Random random)
        {
            var numHoles = random.Next(0, 50);
            for (int a = 0; a < numHoles; a++)
            {
                int r = random.Next(1, 5);
                int aX = random.Next(r, _chunkSize - r);
                int aY = random.Next(r, _chunkSize - r);
                int aZ = random.Next(r, _chunkSize - r);
                for (int xOffset = -r; xOffset <= r; xOffset++)
                    for (int yOffset = -r; yOffset <= r; yOffset++)
                        for (int zOffset = -r; zOffset <= r; zOffset++)
                        {
                            if ((xOffset * xOffset + yOffset * yOffset + zOffset * zOffset) <= r * r)
                            {
                                voxels[aX + xOffset, aY + yOffset, aZ + zOffset] = new Voxel() { Exists = false };
                            }
                        }
            }
        }
    }
}
