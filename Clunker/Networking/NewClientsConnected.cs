using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Networking
{
    public struct NewClientsConnected
    {
        public MessagingChannel Channel { get; private set; }

        public NewClientsConnected(MessagingChannel channel)
        {
            Channel = channel;
        }
    }
}
