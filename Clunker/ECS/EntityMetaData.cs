using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.ECS
{
    [ClunkerComponent]
    public struct EntityMetaData
    {
        public string Name { get; set; }
    }
}
