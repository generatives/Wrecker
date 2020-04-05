using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clunker.Voxels
{
    public class VoxelTypes
    {
        private VoxelType[] _types;
        private Dictionary<string, VoxelType> _byName;

        public VoxelType this[int index] => _types[index];
        public VoxelType this[string name] => _byName[name];

        public VoxelTypes(VoxelType[] types)
        {
            _types = types;
            _byName = types.ToDictionary(v => v.Name);
        }
    }
}
