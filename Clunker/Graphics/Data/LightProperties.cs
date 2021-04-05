using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;

namespace Clunker.Graphics.Data
{
    public struct LightProperties
    {
        public static uint Size = sizeof(float) * 4 + sizeof(float) * 4 + sizeof(float) * 4 + sizeof(float) * 4;
        public Vector4 NearColour;
        public Vector4 FarColour;
        public float MinDistance;
        public float MaxDistance;
        public float Pad1;
        public float Pad2;
        public Vector4 LightWorldPosition;
    }
}
