using Clunker.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Clunker.Voxels;
using DefaultEcs.System;
using Clunker.Core;
using DefaultEcs;
using Clunker.Voxels.Space;
using System.Numerics;
using Clunker.Physics.Voxels;
using Clunker.Graphics;
using Clunker.Networking;
using Clunker.ECS;

namespace Clunker.WorldSpace
{
    public class WorldSpaceLoader : ISystem<double>
    {
        public bool IsEnabled { get; set; } = true;

        private VoxelSpace _voxelSpace;
        private Entity _voxelSpaceEntity;

        private World _world;
        private EntitySet _cameras;
        private int _chunkLength;
        public int LoadRadius { get; set; }
        public int LoadHeight { get; set; }

        private Action<Entity> _setVoxelRendering;

        private List<Vector3i> _chunkOffsets;

        public WorldSpaceLoader(Action<Entity> setVoxelRendering, World world, Entity voxelSpaceEntity, int loadRadius, int loadHeight, int chunkLength)
        {
            _setVoxelRendering = setVoxelRendering;
            _world = world;
            _cameras = _world.GetEntities().With<Transform>().With<Camera>().AsSet();
            _voxelSpace = voxelSpaceEntity.Get<VoxelSpace>();
            _voxelSpaceEntity = voxelSpaceEntity;
            LoadRadius = loadRadius;
            LoadHeight = loadHeight;
            _chunkLength = chunkLength;

            GenerateChunkOffsets();
        }

        private void GenerateChunkOffsets()
        {
            var xzList = new List<Vector2i>();
            for (int xOffset = -LoadRadius; xOffset <= LoadRadius; xOffset++)
                for (int zOffset = -LoadRadius; zOffset <= LoadRadius; zOffset++)
                    if ((xOffset * xOffset + zOffset * zOffset) <= LoadRadius * LoadRadius)
                    {
                        xzList.Add(new Vector2i(xOffset, zOffset));
                    }

            _chunkOffsets = new List<Vector3i>();
            foreach (var xz in xzList.OrderBy((o) => o.LengthSquared()))
            {
                for (int yLoad = 0; yLoad < LoadHeight; yLoad++)
                {
                    _chunkOffsets.Add(new Vector3i(xz.X, yLoad, xz.Y));
                }
            }
        }

        public void Clear()
        {
            foreach (var coordinates in _voxelSpace.Select(kvp => kvp.Key).ToList())
            {
                UnloadChunk(coordinates);
            }
        }
        
        public void Update(double deltaSec)
        {
            foreach(var entity in _cameras.GetEntities())
            {
                var chunk = GetChunk(entity);
                LoadAroundChunk(chunk.X, chunk.Y, chunk.Z);
            }
        }

        public void LoadAroundChunk(int x, int y, int z)
        {
            foreach (var coordinates in _voxelSpace.Select(kvp => kvp.Key).ToList())
            {
                if (!AreaContainsChunk(coordinates))
                {
                    UnloadChunk(coordinates);
                }
            }

            var chunksLoaded = 0;
            for(int i = 0; i < _chunkOffsets.Count; i++)
            {
                var offset = _chunkOffsets[i];
                var coordinates = new Vector3i(x + offset.X, offset.Y, z + offset.Z);
                if (!_voxelSpace.ContainsMember(coordinates))
                {
                    var chunk = _world.CreateEntity();
                    chunk.Set(new NetworkedEntity() { Id = Guid.NewGuid() });
                    chunk.Set(new Transform(chunk)
                    {
                        Position = new Vector3(coordinates.X * _chunkLength * 1, coordinates.Y * _chunkLength * 1, coordinates.Z * _chunkLength * 1)
                    });
                    _setVoxelRendering(chunk);
                    chunk.Set(new Chunk());
                    chunk.Set(new VoxelStaticBody());
                    chunk.Set(new PhysicsBlocks());
                    chunk.Set(new VoxelGrid(_chunkLength, 1, _voxelSpace, coordinates));
                    chunk.Set(new EntityMetaData() { Name = $"Chunk {coordinates}" });
                    _voxelSpace[coordinates] = chunk;

                    chunksLoaded++;
                    if (chunksLoaded == LoadHeight) return;
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
            foreach(var entity in _cameras.GetEntities())
            {
                var CenterChunk = GetChunk(entity);
                var contained = CenterChunk.X - LoadRadius <= x && CenterChunk.X + LoadRadius >= x &&
                    y >= -1 && y < LoadHeight &&
                    CenterChunk.Z - LoadRadius <= z && CenterChunk.Z + LoadRadius >= z;

                if (contained) return true;
            }

            return false;
        }

        public Vector3i GetChunk(Entity entity)
        {
            //return Vector3i.Zero;

            var transform = entity.Get<Transform>();
            var position = transform.WorldPosition;
            return new Vector3i((int)Math.Floor(position.X / _chunkLength), (int)Math.Floor(position.Y / _chunkLength), (int)Math.Floor(position.Z / _chunkLength));
        }

        public void Dispose() { }
    }
}
