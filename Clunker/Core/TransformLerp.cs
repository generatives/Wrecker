using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Core
{
    public struct TransformLerp
    {
        public Queue<TransformMessage> Messages;
        public TransformMessage? CurrentTarget;
        public float Progress;
    }
}
