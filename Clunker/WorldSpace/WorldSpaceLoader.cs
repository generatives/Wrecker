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

namespace Clunker.WorldSpace
{
    public class WorldSpaceLoader : ISystem<double>
    {
        public bool IsEnabled { get; set; } = true;

        private Dictionary<Vector3i, Entity> _chunkMap { get; set; }

        public Entity this[int x, int y, int z]
        {
            get
            {
                return this[new Vector3i(x, y, z)];
            }
            protected set
            {
                this[new Vector3i(x, y, z)] = value;
            }
        }

        public Entity this[Vector3i position]
        {
            get
            {
                return _chunkMap[position];
            }
            protected set
            {
                _chunkMap[position] = value;
            }
        }

        private World _world;
        private Transform _player;
        private int _chunkLength;
        public Vector3i CenterChunk { get; private set; }
        public int LoadRadius { get; set; }

        public WorldSpaceLoader(World world, Transform player, int loadRadius, int chunkLength)
        {
            _world = world;
            _player = player;
            LoadRadius = loadRadius;
            _chunkLength = chunkLength;

            _chunkMap = new Dictionary<Vector3i, Entity>();
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

            foreach (var coordinates in _chunkMap.Keys.ToList())
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
                            if (!_chunkMap.ContainsKey(coordinates))
                            {
                                var chunk = _world.CreateEntity();
                                chunk.Set(new Chunk() { Coordinates = coordinates });
                                _chunkMap[coordinates] = chunk;

                                chunksLoaded++;
                                if (chunksLoaded == Environment.ProcessorCount) return;
                            }
                        }
                    }
                }
            }
        }

        private void UnloadChunk(Vector3i coordinates)
        {
            if(_chunkMap.ContainsKey(coordinates))
            {
                var chunk = _chunkMap[coordinates];
                chunk.Dispose();
                _chunkMap.Remove(coordinates);
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

        public void Dispose()
        {
            foreach(var chunk in _chunkMap.Values)
            {
                chunk.Dispose();
            }

            _chunkMap.Clear();
        }
    }
}
