using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Networking
{
    public struct ServerSystemUpdate
    {
        public double DeltaTime { get; set; }
        public MessageQueues Messages { get; set; }
        public bool NewClients { get; set; }
        public MessageQueues NewClientMessages { get; set; }
    }
}
