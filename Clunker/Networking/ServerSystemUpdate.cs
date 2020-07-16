using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Clunker.Networking
{
    public struct ServerSystemUpdate
    {
        public double DeltaTime { get; set; }
        public MessagingChannel MainChannel { get; set; }
        public bool NewClients { get; set; }
        public MessagingChannel NewClientChannel { get; set; }
    }
}
