using System;
using System.Collections.Generic;
using System.Numerics;

namespace Clunker.Geometry
{
    public static class QuaternionExt
    {
        public static Vector3 GetUpVector(this Quaternion quat)
        {
            return new Vector3(-2 * (quat.X * quat.Y - quat.W * quat.Z), 2 * (quat.X * quat.X + quat.Z * quat.Z) - 1, -2 * (quat.Y * quat.Z + quat.W * quat.X));
        }
        public static Vector3 GetForwardVector(this Quaternion quat)
        {
            return new Vector3(-2 * (quat.X * quat.Z + quat.W * quat.Y), -2 * (quat.Y * quat.Z - quat.W * quat.X), 2 * (quat.X * quat.X + quat.Y * quat.Y) - 1);
        }
        public static Vector3 GetRightVector(this Quaternion quat)
        {
            return new Vector3(2 * (quat.Y * quat.Y + quat.Z * quat.Z) - 1, -2 * (quat.X * quat.Y + quat.W * quat.Z), -2 * (quat.X * quat.Z - quat.W * quat.Y));
        }

        public static (float, float, float) GetYawPitchRoll(this Quaternion q)
        {
            // roll (x-axis rotation)
            var sinr_cosp = +2f * (q.W * q.X + q.Y * q.Z);
            var cosr_cosp = +1f - 2f * (q.X * q.X + q.Y * q.Y);
            var roll = (float)System.Math.Atan2(sinr_cosp, cosr_cosp);

            // pitch (y-axis rotation)
            float pitch;
            var sinp = +2f * (q.W * q.Y - q.Z * q.X);
            if (System.Math.Abs(sinp) >= 1)
                pitch = System.Math.Sign(sinp) * (float)System.Math.PI / 2; // use 90 degrees if out of range
            else
                pitch = (float)System.Math.Asin(sinp);

            // yaw (z-axis rotation)
            var siny_cosp = +2f * (q.W * q.Z + q.X * q.Y);
            var cosy_cosp = +1f - 2f * (q.Y * q.Y + q.Z * q.Z);
            var yaw = (float)System.Math.Atan2(siny_cosp, cosy_cosp);

            return (yaw, pitch, roll);
        }

        //public static BepuUtilities.Quaternion ToPhysics(this Quaternion q)
        //{
        //    return new BepuUtilities.Quaternion(q.X, q.Y, q.Z, q.W);
        //}

        //public static Quaternion ToStandard(this BepuUtilities.Quaternion q)
        //{
        //    return new Quaternion(q.X, q.Y, q.Z, q.W);
        //}
    }
}
