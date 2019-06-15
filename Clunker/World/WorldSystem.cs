using Clunker.Math;
using Clunker.SceneGraph;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Clunker.SceneGraph.SceneSystemInterfaces;

namespace Clunker.World
{
    public class WorldSystem : SceneSystem, IUpdatableSystem, IEnumerable<Chunk>
    {
        private Dictionary<Vector3i, Chunk> _chunkMap { get; set; }

        public Chunk this[int x, int y, int z]
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

        public Chunk this[Vector3i position]
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

        private GameObject _player;
        private ChunkStorage _storage;
        private ChunkGenerator _generator;
        private int _chunkLength;
        public Vector3i CenterChunk { get; private set; }
        public int LoadRadius { get; set; }

        public WorldSystem(GameObject player, ChunkStorage storage, ChunkGenerator generator, int loadRadius, int chunkLength)
        {
            _player = player;
            _storage = storage;
            _generator = generator;
            LoadRadius = loadRadius;
            _chunkLength = chunkLength;

            _chunkMap = new Dictionary<Vector3i, Chunk>();
        }

        public void Update(float time)
        {
            var position = _player.Transform.Position;
            SetCenterChunk((int)MathF.Floor(position.X / _chunkLength), (int)MathF.Floor(position.Y / _chunkLength), (int)MathF.Floor(position.Z / _chunkLength));
            //SetCenterChunk(0, 0, 0);
        }

        public void SetCenterChunk(int x, int y, int z)
        {
            if (_chunkMap.Count == 0 || x != CenterChunk.X || y != CenterChunk.Y || z != CenterChunk.Z)
            {
                CenterChunk = new Vector3i(x, y, z);
                //Console.WriteLine($"Player is {_player.Transform.Position}");
                //Console.WriteLine($"Chunk is {x}, {y}, {z}");

                foreach (var chunk in new List<Chunk>(this))
                {
                    if (!AreaContainsChunk(chunk))
                    {
                        UnloadChunk(chunk);
                    }
                }

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
                                    LoadChunk(coordinates);
                                }
                            }
                        }
                    }
                }

                //foreach (var chunk in new List<Chunk>(this))
                //{
                //    Console.WriteLine($"Loaded {chunk.Coordinates}");
                //}
            }
        }

        private void UnloadChunk(Chunk chunk)
        {
            _chunkMap.Remove(chunk.Coordinates);
            CurrentScene.RemoveGameObject(chunk.GameObject);
            _storage.StoreChunk(chunk);
        }

        private void LoadChunk(Vector3i coordinates)
        {
            Chunk chunk;
            if(_storage.ChunkExists(coordinates))
            {
                chunk = _storage.LoadChunk(coordinates);
            }
            else
            {
                chunk = _generator.GenerateChunk(coordinates);
            }
            _chunkMap[coordinates] = chunk;
            CurrentScene.AddGameObject(chunk.GameObject);
        }

        public bool AreaContainsChunk(Chunk chunk)
        {
            return AreaContainsChunk(chunk.Coordinates);
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerator<Chunk> GetEnumerator()
        {
            foreach (var kvp in _chunkMap)
                yield return kvp.Value;
        }
    }
}
