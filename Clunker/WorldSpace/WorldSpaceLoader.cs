using Clunker.Geometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Collections.Concurrent;
using Clunker.Voxels;
using DefaultEcs.System;
using Clunker.Core;
using DefaultEcs;
using Clunker.Voxels.Space;
using System.Numerics;

namespace Clunker.WorldSpace
{
    public class WorldSpaceLoader : ISystem<double>
    {
        public bool IsEnabled { get; set; } = true;

        private VoxelSpace WorldSpace => _worldVoxelSpace.Get<VoxelSpace>();

        private World _world;
        private Transform _player;
        private Entity _worldVoxelSpace;
        private int _chunkLength;
        public Vector3i CenterChunk { get; private set; }
        public int LoadRadius { get; set; }

        public WorldSpaceLoader(World world, Transform player, Entity worldVoxelSpace, int loadRadius, int chunkLength)
        {
            _world = world;
            _player = player;
            _worldVoxelSpace = worldVoxelSpace;
            LoadRadius = loadRadius;
            _chunkLength = chunkLength;
        }
        
        public void Update(double deltaSec)
        {
            var position = _player.WorldPosition;
            SetCenterChunk((int)Math.Floor(position.X / _chunkLength), (int)Math.Floor(position.Y / _chunkLength), (int)Math.Floor(position.Z / _chunkLength));
            //SetCenterChunk(0, 0, 0);
        }

        public void SetCenterChunk(int x, int y, int z)
        {
            CenterChunk = new Vector3i(x, y, z);

            foreach (var coordinates in WorldSpace.Members.Keys.ToList())
            {
                if (!AreaContainsChunk(coordinates))
                {
                    UnloadChunk(coordinates);
                }
            }

            var chunksLoaded = 0;
            for (int xOffset = -LoadRadius; xOffset <= LoadRadius; xOffset++)
            {
                for (int yOffset = -LoadRadius; yOffset <= LoadRadius; yOffset++)
                {
                    for (int zOffset = -LoadRadius; zOffset <= LoadRadius; zOffset++)
                    {
                        if ((xOffset * xOffset + yOffset * yOffset + zOffset * zOffset) <= LoadRadius * LoadRadius)
                        {
                            var coordinates = new Vector3i(x + xOffset, y + yOffset, z + zOffset);
                            if (!WorldSpace.Members.ContainsKey(coordinates))
                            {
                                var chunk = _world.CreateEntity();
                                chunk.Set(new Transform()
                                {
                                    Position = new Vector3(coordinates.X * _chunkLength * 1, coordinates.Y * _chunkLength * 1, coordinates.Z * _chunkLength * 1)
                                });
                                chunk.Set(new VoxelGrid(_chunkLength, 1, _worldVoxelSpace, coordinates));
                                chunk.Set(new Chunk());
                                WorldSpace.Members[coordinates] = chunk;

                                chunksLoaded++;
                                if (chunksLoaded == Environment.ProcessorCount * 3) return;
                            }
                        }
                    }
                }
            }
        }

        private void UnloadChunk(Vector3i coordinates)
        {
            if(WorldSpace.Members.ContainsKey(coordinates))
            {
                var chunk = WorldSpace.Members[coordinates];
                chunk.Dispose();
                WorldSpace.Members.Remove(coordinates);
            }
        }

        public bool AreaContainsChunk(Vector3i coordinates)
        {
            return AreaContainsChunk(coordinates.X, coordinates.Y, coordinates.Z);
        }

        public bool AreaContainsChunk(int x, int y, int z)
        {
            return CenterChunk.X - LoadRadius <= x && CenterChunk.X + LoadRadius >= x &&
                CenterChunk.Y - LoadRadius <= y && CenterChunk.Y + LoadRadius >= y &&
                CenterChunk.Z - LoadRadius <= z && CenterChunk.Z + LoadRadius >= z;
        }

        public void Dispose() { }
    }
}
