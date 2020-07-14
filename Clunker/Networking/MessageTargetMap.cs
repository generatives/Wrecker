using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Networking
{
    public class MessageTargetMap
    {
        public IReadOnlyDictionary<Type, int> ByType { get; private set; }
        public IReadOnlyDictionary<int, Type> ByNum { get; private set; }

        public MessageTargetMap(IReadOnlyDictionary<Type, int> byType, IReadOnlyDictionary<int, Type> byNum)
        {
            ByType = byType;
            ByNum = byNum;
        }
    }
}
