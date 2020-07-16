using Collections.Pooled;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Clunker.Voxels
{
    public static class LengthEncodedVoxels
    {
        public static void ToStream(Voxel[] voxels, Stream stream)
        {
            using(var writer = new BinaryWriter(stream, Encoding.ASCII, true))
            {
                var encoded = new PooledList<long>();
                var currentVoxel = voxels[0];
                ushort currentLength = 1;
                for (int i = 1; i < voxels.Length; i++)
                {
                    if (voxels[i] == currentVoxel)
                    {
                        currentLength++;
                    }
                    else
                    {
                        writer.Write(currentLength);
                        writer.Write(currentVoxel.Data);
                        currentVoxel = voxels[i];
                        currentLength = 1;
                    }
                }
                writer.Write(currentLength);
                writer.Write(currentVoxel.Data);
            }
        }

        public static Voxel[] FromStream(int length, Stream stream)
        {
            using(var reader = new BinaryReader(stream, Encoding.ASCII, true))
            {
                var voxels = new Voxel[length];
                var voxelIndex = 0;
                while(voxelIndex < length)
                {
                    var runLength = reader.ReadUInt16();
                    var voxelData = reader.ReadInt32();
                    var end = voxelIndex + runLength;
                    while (voxelIndex < end)
                    {
                        voxels[voxelIndex] = new Voxel() { Data = voxelData };
                        voxelIndex++;
                    }
                }

                return voxels;
            }
        }
    }
}
