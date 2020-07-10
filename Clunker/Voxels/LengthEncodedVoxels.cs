using Collections.Pooled;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Voxels
{
    public static class LengthEncodedVoxels
    {
        public static long[] FromVoxels(Voxel[] voxels)
        {
            var encoded = new PooledList<long>();
            var currentVoxel = voxels[0];
            var currentLength = 1;
            for(int i = 1; i < voxels.Length; i++)
            {
                if(voxels[i] == currentVoxel)
                {
                    currentLength++;
                }
                else
                {
                    encoded.Add(((long)currentLength << 32) | (long)currentVoxel.Data);
                    currentVoxel = voxels[i];
                    currentLength = 1;
                }
            }
            encoded.Add(((long)currentLength << 32) | (long)currentVoxel.Data);

            return encoded.ToArray();
        }

        public static Voxel[] ToVoxels(int length, long[] encoded)
        {
            var voxels = new Voxel[length];
            var voxelIndex = 0;
            for(int i = 0; i < encoded.Length; i++)
            {
                var run = encoded[i];
                var voxelData = (int)(run & uint.MaxValue);
                var runLength = (int)(run >> 32);
                var end = voxelIndex + runLength;
                while(voxelIndex < end)
                {
                    voxels[voxelIndex] = new Voxel() { Data = voxelData };
                    voxelIndex++;
                }
            }

            return voxels;
        }
    }
}
