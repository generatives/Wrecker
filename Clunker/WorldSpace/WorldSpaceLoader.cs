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
using Clunker.Physics.Voxels;
using Clunker.Voxels.Lighting;
using Clunker.Voxels.Meshing;

namespace Clunker.WorldSpace
{
    public class WorldSpaceLoader : ISystem<double>
    {
        public bool IsEnabled { get; set; } = true;

        private VoxelSpace _voxelSpace;
        private Entity _voxelSpaceEntity;

        private World _world;
        private Transform _player;
        private int _chunkLength;
        public Vector3i CenterChunk { get; private set; }
        public int LoadRadius { get; set; }
        public int LoadHeight { get; set; }

        private Action<Entity> _setVoxelRendering;

        public WorldSpaceLoader(Action<Entity> setVoxelRendering, World world, Transform player, Entity voxelSpaceEntity, int loadRadius, int loadHeight, int chunkLength)
        {
            _setVoxelRendering = setVoxelRendering;
            _world = world;
            _player = player;
            _voxelSpace = voxelSpaceEntity.Get<VoxelSpace>();
            _voxelSpaceEntity = voxelSpaceEntity;
            LoadRadius = loadRadius;
            LoadHeight = loadHeight;
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

            foreach (var coordinates in _voxelSpace.Select(kvp => kvp.Key).ToList())
            {
                if (!AreaContainsChunk(coordinates))
                {
                    UnloadChunk(coordinates);
                }
            }

            var chunksLoaded = 0;
            for (int xOffset = -LoadRadius; xOffset <= LoadRadius; xOffset++)
            {
                //for (int yOffset = -LoadRadius; yOffset <= LoadRadius; yOffset++)
                for (int yLoad = 0; yLoad < LoadHeight; yLoad++)
                {
                    for (int zOffset = -LoadRadius; zOffset <= LoadRadius; zOffset++)
                    {
                        if ((xOffset * xOffset + zOffset * zOffset) <= LoadRadius * LoadRadius)
                        {
                            var coordinates = new Vector3i(x + xOffset, yLoad, z + zOffset);
                            if (!_voxelSpace.ContainsMember(coordinates))
                            {
                                var chunk = _world.CreateEntity();
                                chunk.Set(new Transform()
                                {
                                    Position = new Vector3(coordinates.X * _chunkLength * 1, coordinates.Y * _chunkLength * 1, coordinates.Z * _chunkLength * 1)
                                });
                                _setVoxelRendering(chunk);
                                chunk.Set(new Chunk());
                                chunk.Set(new VoxelStaticBody());
                                chunk.Set(new PhysicsBlocks());
                                chunk.Set(new LightVertexResources());
                                chunk.Set(new VoxelGrid(_chunkLength, 1, _voxelSpace, coordinates));
                                _voxelSpace[coordinates] = chunk;

                                chunksLoaded++;
                                //if (chunksLoaded == Environment.ProcessorCount * 3) return;
                            }
                        }
                    }
                }
            }
        }

        private void UnloadChunk(Vector3i coordinates)
        {
            if(_voxelSpace.ContainsMember(coordinates))
            {
                var chunk = _voxelSpace[coordinates];
                chunk.Dispose();
                _voxelSpace.Remove(coordinates);
            }
        }

        public bool AreaContainsChunk(Vector3i coordinates)
        {
            return AreaContainsChunk(coordinates.X, coordinates.Y, coordinates.Z);
        }

        public bool AreaContainsChunk(int x, int y, int z)
        {
            return CenterChunk.X - LoadRadius <= x && CenterChunk.X + LoadRadius >= x &&
                y >= -1 && y < LoadHeight &&
                CenterChunk.Z - LoadRadius <= z && CenterChunk.Z + LoadRadius >= z;
        }

        public void Dispose() { }
    }
}
