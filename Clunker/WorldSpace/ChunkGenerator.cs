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
        private float _radius = 400;

        public ChunkGenerator()
        {
            _noise = new FastNoise();
            _noise.SetFrequency(0.08f);
        }

        public void GenerateChunk(Entity entity, EntityRecord entityRecord, Vector3i coordinates)
        {
            ref var voxelSpaceData = ref entity.Get<VoxelGrid>();
            bool anyExist = true;

            for (int x = 0; x < voxelSpaceData.GridSize; x++)
                for (int y = 0; y < voxelSpaceData.GridSize; y++)
                    for (int z = 0; z < voxelSpaceData.GridSize; z++)
                    {
                        var voxelPosition = new Vector3(coordinates.X * voxelSpaceData.GridSize + x, coordinates.Y * voxelSpaceData.GridSize + y, coordinates.Z * voxelSpaceData.GridSize + z);

                        var dist = voxelPosition.Length();

                        var xz = new Vector3(voxelPosition.X, 0, voxelPosition.Z);
                        var xzDist = xz.Length();
                        var lat = ClunkerMath.ToDegrees((float)Math.Atan(voxelPosition.Y / xzDist));
                        var lng = ClunkerMath.ToDegrees((float)Math.Atan(voxelPosition.Z / voxelPosition.X));

                        var terrainHeight = (_noise.GetPerlin(lat, lng)) * 0.1f * _radius;
                        terrainHeight = terrainHeight + _radius;
                        if (dist > terrainHeight)
                        {
                            continue;
                        }
                        else if (terrainHeight > dist + 1)
                        {
                            voxelSpaceData[x, y, z] = new Voxel() { Density = 255 };
                        }
                        else
                        {
                            var aboveBase = terrainHeight - dist;
                            var density = (byte)(255 * aboveBase);
                            voxelSpaceData[x, y, z] = new Voxel() { Density = density };
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
