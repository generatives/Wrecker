using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Clunker.Voxels.Serialization
{
    public class VoxelSpaceDataSerializer
    {
        public static VoxelSpaceData Deserialize(Stream stream)
        {
            return MessagePackSerializer.Deserialize<VoxelSpaceData>(stream);
        }

        public static void Serialize(VoxelSpaceData data, Stream stream)
        {
            MessagePackSerializer.Serialize(stream, data);
        }
    }
}
