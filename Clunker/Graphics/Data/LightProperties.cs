using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Graphics.Data
{
    public struct LightProperties
    {
        public static uint Size = sizeof(float) * 4 + sizeof(float) * 4;
        public float NearStrength;
        public float FarStrength;
        public float MinDistance;
        public float MaxDistance;
        public Vector4 LightWorldPosition;
    }
}
