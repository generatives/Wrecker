using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.ECS
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ClunkerComponentAttribute : Attribute
    {
    }
}
