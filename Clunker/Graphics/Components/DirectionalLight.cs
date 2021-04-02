using Clunker.Graphics.Data;
using System.Numerics;

namespace Clunker.Graphics.Components
{
    public struct DirectionalLight
    {
        public Matrix4x4 ProjectionMatrix;
        public LightProperties LightProperties;
    }
}
