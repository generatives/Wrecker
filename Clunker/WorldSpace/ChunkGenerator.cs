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
            var biome = _noise.GetPerlin(coordinates.X, coordinates.Z);
            var biomeBlockType =
                biome > -1 && biome < -0.5 ? 3 :
                biome > -0.5 && biome < 0 ? 5 :
                biome > 0 && biome < 0.5 ? 6 :
                biome > 0.5 && biome < 1 ? 7 : 0;

            ref var voxelSpaceData = ref entity.Get<VoxelGrid>();
            bool anyExist = true;

            if(voxelSpaceData.SpaceIndex.Y >= 2 && voxelSpaceData.SpaceIndex.Y <= 5)
            {
                if(random.Next(0, 29) == 0)
                {
                    GenerateSpheres(voxelSpaceData, random);
                    SetTypes(voxelSpaceData);
                }
            }
            else if (voxelSpaceData.SpaceIndex.Y >= 0)
            {
                for (int x = 0; x < voxelSpaceData.GridSize; x++)
                    for (int y = 0; y < voxelSpaceData.GridSize; y++)
                        for (int z = 0; z < voxelSpaceData.GridSize; z++)
                        {
                            var voxelPosition = new Vector3(coordinates.X * voxelSpaceData.GridSize + x, coordinates.Y * voxelSpaceData.GridSize + y, coordinates.Z * voxelSpaceData.GridSize + z);

                            var exists = ((_noise.GetPerlin(voxelPosition.X / 5f, voxelPosition.Z / 5f) + 1f) * 15) > voxelPosition.Y;
                            anyExist = anyExist || exists;
                            var water = voxelPosition.Y < 8 && !exists;
                            var blockType = water ? 4 : biomeBlockType;
                            exists = exists || water;
                            voxelSpaceData[x, y, z] = new Voxel() { Exists = exists, BlockType = (ushort)blockType };
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
