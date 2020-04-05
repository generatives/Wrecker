using Hyperion;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace Clunker.Graphics
{
    public struct VertexPositionTextureNormal
    {
        [Ignore]
        public const byte SizeInBytes = 32;

        public readonly Vector3 Position;
        public readonly Vector2 TextureCoordinates;
        public readonly Vector3 Normal;

        public VertexPositionTextureNormal(Vector3 position, Vector2 texCoords, Vector3 normal)
        {
            Position = position;
            TextureCoordinates = texCoords;
            Normal = normal;
        }
    }
}
