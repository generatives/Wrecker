﻿using Clunker.Geometry;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace Clunker.Voxels
{
    [MessagePackObject]
    public struct Voxel
    {
        [Key(0)]
        public uint Data;
        public bool Exists { get => (1 & Data) == 1; set => Data = Data | (uint)(value ? 1 : 0); }
        public ushort BlockType { get => (ushort)((Data >> 1) & 0xFFF); set => Data = Data | (uint)(value << 1); }
        public VoxelSide Orientation { get => (VoxelSide)((Data >> 13) & 0x7); set => Data = Data | ((uint)value << 13); }

        public static bool operator ==(Voxel v, Voxel v1)
        {
            return v.Data == v1.Data;
        }

        public static bool operator !=(Voxel v, Voxel v1)
        {
            return !(v == v1);
        }

        public override bool Equals(object obj)
        {
            return obj is Voxel voxel && this == voxel;
        }

        public override int GetHashCode()
        {
            return (int)Data;
        }
    }

    public enum VoxelSide
    {
        TOP, BOTTOM, NORTH, SOUTH, EAST, WEST
    }

    public static class VoxelSideExt
    {
        public static Quaternion GetQuaternion(this VoxelSide side)
        {
            //return new Quaternion(GetDirection(side), 0);
            switch (side)
            {
                case VoxelSide.TOP:
                    return Quaternion.CreateFromAxisAngle(-Vector3.UnitX, (float)Math.PI / 2f);
                case VoxelSide.BOTTOM:
                    return Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)Math.PI / 2f);
                case VoxelSide.NORTH:
                    return Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)Math.PI);
                case VoxelSide.SOUTH:
                    return Quaternion.Identity;
                case VoxelSide.EAST:
                    return Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)Math.PI / 2f);
                case VoxelSide.WEST:
                    return Quaternion.CreateFromAxisAngle(-Vector3.UnitY, (float)Math.PI / 2f);
                default:
                    return Quaternion.Identity;
            }
        }

        public static Vector3 GetDirection(this VoxelSide side)
        {
            switch (side)
            {
                case VoxelSide.TOP:
                    return Vector3.UnitY;
                case VoxelSide.BOTTOM:
                    return -Vector3.UnitY;
                case VoxelSide.NORTH:
                    return -Vector3.UnitZ;
                case VoxelSide.SOUTH:
                    return Vector3.UnitZ;
                case VoxelSide.EAST:
                    return Vector3.UnitX;
                case VoxelSide.WEST:
                    return -Vector3.UnitX;
                default:
                    return Vector3.Zero;
            }
        }

        public static Vector3i GetGridOffset(this VoxelSide side)
        {
            switch (side)
            {
                case VoxelSide.TOP:
                    return Vector3i.UnitY;
                case VoxelSide.BOTTOM:
                    return -Vector3i.UnitY;
                case VoxelSide.NORTH:
                    return -Vector3i.UnitZ;
                case VoxelSide.SOUTH:
                    return Vector3i.UnitZ;
                case VoxelSide.EAST:
                    return Vector3i.UnitX;
                case VoxelSide.WEST:
                    return -Vector3i.UnitX;
                default:
                    return Vector3i.Zero;
            }
        }

        public static Quad GetQuad(this VoxelSide side)
        {
            switch (side)
            {
                case VoxelSide.BOTTOM:
                    return new Quad(new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 0, 0), -Vector3.UnitY);
                case VoxelSide.EAST:
                    return new Quad(new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0), new Vector3(1, 0, 0), Vector3.UnitX);
                case VoxelSide.WEST:
                    return new Quad(new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 1, 1), new Vector3(0, 0, 1), -Vector3.UnitX);
                case VoxelSide.TOP:
                    return new Quad(new Vector3(0, 1, 1), new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), Vector3.UnitY);
                case VoxelSide.NORTH:
                    return new Quad(new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 0), -Vector3.UnitZ);
                case VoxelSide.SOUTH:
                    return new Quad(new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 0, 1), Vector3.UnitZ);
                default:
                    throw new Exception("how did this happen?");
            }
        }
    }
}
