using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Networking
{
    public struct ServerSystemUpdate
    {
        public double DeltaTime { get; set; }
        public List<object> Messages { get; set; }
        public bool NewClients { get; set; }
        public List<object> NewClientMessages { get; set; }
    }
}
