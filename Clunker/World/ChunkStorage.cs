using Clunker.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.World
{
    public class ChunkStorage
    {
        public bool ChunkExists(Vector3i coordinates)
        {
            return false;
        }

        public Chunk LoadChunk(Vector3i coordinates)
        {
            return new Chunk(coordinates);
        }

        public void StoreChunk(Chunk chunk)
        {

        }
    }
}
