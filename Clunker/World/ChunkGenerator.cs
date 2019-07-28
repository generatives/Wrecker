using Clunker.Diagnostics;
using Clunker.Graphics;
using Clunker.Math;
using Clunker.Physics.Voxels;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
using Clunker.SceneGraph.Core;
using Clunker.Voxels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Clunker.World
{
    public class ChunkGenerator
    {
        private VoxelTypes _types;
        private MaterialInstance _materialInstance;
        private int _chunkSize;
        private int _voxelSize;
        private FastNoise _noise;

        public ChunkGenerator(VoxelTypes types, MaterialInstance materialInstance, int chunkSize, int voxelSize)
        {
            _types = types;
            _materialInstance = materialInstance;
            _chunkSize = chunkSize;
            _voxelSize = voxelSize;
            _noise = new FastNoise(DateTime.Now.Second);
            _noise.SetFrequency(0.08f);
        }

        public Chunk GenerateChunk(Vector3i coordinates)
        {
            var random = new Random((coordinates.X << 20) ^ (coordinates.Y << 10) ^ (coordinates.Z));
            var voxelSpaceData = new VoxelGridData(_chunkSize, _chunkSize, _chunkSize, _voxelSize);

            var voxels = GenerateSpheres(random);
            //JoinVoxels(voxels);
            SplatterHoles(voxels, random);

            for (int x = 0; x < _chunkSize; x++)
                for (int y = 0; y < _chunkSize; y++)
                    for (int z = 0; z < _chunkSize; z++)
                    {
                        voxelSpaceData[x, y, z] = new Voxel() { Exists = voxels[x, y, z] != 0 };
                        //voxelSpaceData[x, y, z] = new Voxel() { Exists = _noise.GetPerlin(coordinates.X * _chunkSize + x, coordinates.Y * _chunkSize + y, coordinates.Z * _chunkSize + z) > 0f };
                    }

            var voxelSpace = new VoxelGrid(voxelSpaceData, new Dictionary<Vector3i, GameObject>());
            var chunk = new Chunk(coordinates);
            var gameObject = new GameObject($"Chunk {coordinates}");
            gameObject.AddComponent(voxelSpace);
            gameObject.AddComponent(chunk);
            gameObject.AddComponent(new VoxelShape());
            gameObject.AddComponent(new StaticVoxelBody());
            gameObject.AddComponent(new VoxelMeshRenderable(_types, _materialInstance));

            return chunk;
        }

        public byte[,,] GenerateSpheres(Random random)
        {
            var voxels = new byte[_chunkSize, _chunkSize, _chunkSize];
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
                        if(strength > 0.5f)
                        {
                            voxels[x, y, z] = 1;
                        }
                    }

            return voxels;
        }

        public void SplatterHoles(byte[,,] voxels, Random random)
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
                                voxels[aX + xOffset, aY + yOffset, aZ + zOffset] = 0;
                            }
                        }
            }
        }
    }
}
