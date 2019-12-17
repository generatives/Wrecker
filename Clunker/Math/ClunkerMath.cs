using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Math
{
    public static class ClunkerMath
    {
        public static float ToRadians(float degrees)
        {
            return (MathF.PI / 180) * degrees;
        }

        public static float ToDegrees(float radians)
        {
            return radians / (MathF.PI / 180);
        }
    }
}
