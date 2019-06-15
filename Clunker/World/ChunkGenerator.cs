using Clunker.Graphics;
using Clunker.Math;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentsInterfaces;
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

        public ChunkGenerator(VoxelTypes types, MaterialInstance materialInstance, int chunkSize, int voxelSize)
        {
            _types = types;
            _materialInstance = materialInstance;
            _chunkSize = chunkSize;
            _voxelSize = voxelSize;
        }

        public Chunk GenerateChunk(Vector3i coordinates)
        {
            //var watch = Stopwatch.StartNew();
            var random = new Random((coordinates.X << 20) ^ (coordinates.Y << 10) ^ (coordinates.Z));
            var voxelSpaceData = new VoxelSpaceData(_chunkSize, _chunkSize, _chunkSize, _voxelSize);

            //for (int x = 0; x < voxelSpaceData.XLength; x++)
            //    for (int y = 0; y < voxelSpaceData.YLength; y++)
            //        for (int z = 0; z < voxelSpaceData.ZLength; z++)
            //        {
            //        }

            var numAstroids = random.Next(1, 4);
            for (int a = 0; a < numAstroids; a++)
            {
                int r = random.Next(2, 10);
                int aX = random.Next(r, _chunkSize - r);
                int aY = random.Next(r, _chunkSize - r);
                int aZ = random.Next(r, _chunkSize - r);
                for (int xOffset = -r; xOffset <= r; xOffset++)
                    for (int yOffset = -r; yOffset <= r; yOffset++)
                        for (int zOffset = -r; zOffset <= r; zOffset++)
                        {
                            if((xOffset * xOffset + yOffset * yOffset + zOffset * zOffset) <= r * r)
                            {
                                voxelSpaceData[aX + xOffset, aY + yOffset, aZ + zOffset] = new Voxel() { Exists = true };
                            }
                        }
            }
            var voxelSpace = new VoxelSpace(voxelSpaceData);
            var chunk = new Chunk(coordinates);
            var gameObject = new GameObject();
            gameObject.AddComponent(voxelSpace);
            gameObject.AddComponent(chunk);
            gameObject.AddComponent(new VoxelBody());
            gameObject.AddComponent(new VoxelMesh(_types, _materialInstance));
            
            gameObject.GetComponent<Transform>().Position = coordinates * _chunkSize * _voxelSize;
            //Console.WriteLine($"Chunk gen: {watch.Elapsed.TotalMilliseconds}");

            return chunk;
        }
    }
}
