using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Geometry
{
    public static class ClunkerMath
    {
        public static float ToRadians(float degrees)
        {
            return ((float)System.Math.PI / 180) * degrees;
        }

        public static float ToDegrees(float radians)
        {
            return radians / ((float)System.Math.PI / 180);
        }
    }
}
