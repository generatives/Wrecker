using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Clunker.Networking
{
    public struct ServerSystemUpdate
    {
        public double DeltaTime { get; set; }
        public TargetedMessageChannel MainChannel { get; set; }
        public bool NewClients { get; set; }
        public TargetedMessageChannel NewClientChannel { get; set; }
    }
}
