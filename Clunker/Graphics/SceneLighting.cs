using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace Clunker.Graphics
{
    public struct SceneLighting
    {
        public static uint Size = sizeof(float) * (4 + 3 + 4 + 1);
        public RgbaFloat DiffuseLightColour;
        public Vector3 DiffuseLightDirection;
        public RgbaFloat AmbientLightColour;
        public float AmbientLightStrength;
    }
}
