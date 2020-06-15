using System;
using System.Collections.Generic;
using System.Numerics;
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

        public static Vector2 PerpendicularClockwise(this Vector2 vector2)
        {
            return new Vector2(vector2.Y, -vector2.X);
        }

        public static Vector2 PerpendicularCounterClockwise(this Vector2 vector2)
        {
            return new Vector2(-vector2.Y, vector2.X);
        }
    }
}
