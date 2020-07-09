using Clunker.ECS;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Core
{
    [ClunkerComponent]
    public struct TransformLerp
    {
        public Queue<TransformMessage> Messages;
        public TransformMessage? CurrentTarget;
        public float Progress;
    }
}
