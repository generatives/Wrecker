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
        private FastNoise _noise;

        public ChunkGenerator()
        {
            _noise = new FastNoise();
            _noise.SetFrequency(0.08f);
        }

        public void GenerateChunk(Entity entity, EntityRecord entityRecord, Vector3i coordinates)
        {
            var random = new Random((coordinates.X << 20) ^ (coordinates.Y << 10) ^ (coordinates.Z));

            ref var voxelSpaceData = ref entity.Get<VoxelGrid>();
            bool anyExist = true;

            for (int x = 0; x < voxelSpaceData.GridSize; x++)
                for (int y = 0; y < voxelSpaceData.GridSize; y++)
                    for (int z = 0; z < voxelSpaceData.GridSize; z++)
                    {
                        var voxelPosition = new Vector3(coordinates.X * voxelSpaceData.GridSize + x, coordinates.Y * voxelSpaceData.GridSize + y, coordinates.Z * voxelSpaceData.GridSize + z);

                        //if (voxelPosition.Y == 0)
                        //{
                        //    voxelSpaceData[x, y, z] = new Voxel() { Exists = true, BlockType = (ushort)5 }; // stone
                        //}

                        //if ((/*voxelPosition.Y == 1 ||*/ voxelPosition.Y == 2) &&
                        //    (Math.Abs(voxelPosition.X) == 5 || Math.Abs(voxelPosition.Z) == 4 || Math.Abs(voxelPosition.Z) == 5 || Math.Abs(voxelPosition.Z) == 6))
                        //{
                        //    voxelSpaceData[x, y, z] = new Voxel() { Exists = true, BlockType = (ushort)0 }; // stone
                        //}

                        var islandValue = _noise.GetPerlin(voxelPosition.X / 16, voxelPosition.Z / 16) * voxelSpaceData.GridSize;

                        if (islandValue > 0)
                        {
                            if (voxelPosition.Y > voxelSpaceData.GridSize)
                            {
                                var heightValue = (_noise.GetPerlin(voxelPosition.X / 5f, voxelPosition.Z / 5f) + 1) * 1;
                                var exists = (islandValue * heightValue * heightValue) > (voxelPosition.Y - voxelSpaceData.GridSize);
                                voxelSpaceData[x, y, z] = new Voxel() { Exists = exists, BlockType = (ushort)7 }; // cactus top
                            }
                            else
                            {
                                if ((voxelPosition.Y - voxelSpaceData.GridSize) > -islandValue)
                                {
                                    voxelSpaceData[x, y, z] = new Voxel() { Exists = true, BlockType = (ushort)5 }; // stone
                                }
                            }
                        }
                    }


            if (anyExist)
            {
                entityRecord.Set(voxelSpaceData);
            }
        }
        public void GenerateSpheres(VoxelGrid voxels, Random random)
        {
            var numAstroids = random.Next(1, 3);
            var locations = new (Vector3i, int)[numAstroids];
            for (var a = 0; a < locations.Length; a++)
            {
                int r = random.Next(5, 7);
                int aX = random.Next(r, voxels.GridSize - r);
                int aY = random.Next(r, voxels.GridSize - r);
                int aZ = random.Next(r, voxels.GridSize - r);
                locations[a] = (new Vector3i(aX, aY, aZ), r);
            }

            for (int x = 0; x < voxels.GridSize; x++)
                for (int y = 0; y < voxels.GridSize; y++)
                    for (int z = 0; z < voxels.GridSize; z++)
                    {
                        var strength = 0f;
                        for (byte a = 0; a < numAstroids; a++)
                        {
                            var (location, radius) = locations[a];
                            if (y <= location.Y + 2)
                            {
                                var rSq = ((x - location.X) * (x - location.X)) + ((y - location.Y) * (y - location.Y)) + ((z - location.Z) * (z - location.Z));
                                if (rSq == 0)
                                {
                                    strength = 1;
                                    break;
                                }
                                else
                                {
                                    strength += (float)(radius * radius) / rSq;
                                }
                            }
                        }
                        voxels[x, y, z] = new Voxel() { Exists = (255 * Math.Max(0, strength - 0.5f) > 0 )};
                    }
        }

        public void SetTypes(VoxelGrid voxels)
        {
            for (int x = 0; x < voxels.GridSize; x++)
                for (int y = 0; y < voxels.GridSize; y++)
                    for (int z = 0; z < voxels.GridSize; z++)
                    {
                        var voxel = voxels[x, y, z];
                        if (voxel.Exists)
                        {
                            var topBlock = !voxels.ContainsIndex(x, y + 1, z) || !voxels[x, y + 1, z].Exists;
                            voxel.BlockType = (ushort)(topBlock ? 7 : 3);
                            voxels[x, y, z] = voxel;
                        }
                    }
        }
    }
}
