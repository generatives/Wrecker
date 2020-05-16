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
using Clunker.Physics.Voxels;

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
            var voxelSpaceData = new VoxelGrid(_chunkSize, _voxelSize);

            GenerateSpheres(voxelSpaceData, random);
            SplatterHoles(voxelSpaceData, random);
            bool anyExist = true;

            //bool anyExist = false;

            //var planetPosition = new Vector3(_chunkSize / 2f);
            //var planetSize = _chunkSize * 0.75f / 2f;

            //if (coordinates == new Vector3i())
            //{
            //    for (int x = 0; x < _chunkSize; x++)
            //        for (int y = 0; y < _chunkSize; y++)
            //            for (int z = 0; z < _chunkSize; z++)
            //            {
            //                var voxelPosition = new Vector3(x, y, z);

            //                //var exists = _noise.GetPerlin(coordinates.X * _chunkSize + x, coordinates.Y * _chunkSize + y, coordinates.Z * _chunkSize + z) > 0f;
            //                //var exists = planetPosition == voxelPosition;
            //                var density = Math.Max(1 - (Vector3.Distance(planetPosition, voxelPosition) / planetSize), 0) * byte.MaxValue;
            //                anyExist = anyExist || density > 0;
            //                if (density != 0)
            //                {

            //                }
            //                voxelSpaceData[x, y, z] = new Voxel() { Density = (byte)density };
            //            }
            //}

            //for (int x = 0; x < _chunkSize; x++)
            //    for (int y = 0; y < _chunkSize; y++)
            //        for (int z = 0; z < _chunkSize; z++)
            //        {
            //            var planetPosition = new Vector3(0, 0, -350);
            //            var planetSize = 300;
            //            var voxelPosition = new Vector3(coordinates.X * _chunkSize + x, coordinates.Y * _chunkSize + y, coordinates.Z * _chunkSize + z);

            //            //var exists = _noise.GetPerlin(coordinates.X * _chunkSize + x, coordinates.Y * _chunkSize + y, coordinates.Z * _chunkSize + z) > 0f;
            //            var exists = Vector3.Distance(planetPosition, voxelPosition) < planetSize;
            //            anyExist = anyExist || exists;
            //            voxelSpaceData[x, y, z] = new Voxel() { Exists = exists };
            //        }

            if (anyExist)
            {
                var transform = new Transform();
                transform.Position = new Vector3(coordinates.X * _chunkSize * _voxelSize, coordinates.Y * _chunkSize * _voxelSize, coordinates.Z * _chunkSize * _voxelSize);
                entity.Set(transform);

                entity.Set(_materialInstance);
                //entity.Set(new VoxelStaticBody());
                //entity.Set(new ExposedVoxels());
                entity.Set(voxelSpaceData);
            }
        }

        public void GenerateSpheres(VoxelGrid voxels, Random random)
        {
            var numAstroids = random.Next(1, 3);
            var locations = new (Vector3i, int)[numAstroids];
            for (var a = 0; a < numAstroids; a++)
            {
                int r = random.Next(4, 6);
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
                                strength = 1;
                                break;
                            }
                            else
                            {
                                strength += (float)(radius * radius) / rSq;
                            }
                        }
                        voxels[x, y, z] = new Voxel() { Density = (byte)(255 * Math.Max(0, strength - 0.5f)) };
                    }
        }

        public void SplatterHoles(VoxelGrid voxels, Random random)
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
