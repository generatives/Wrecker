using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Clunker.Networking
{
    public interface IMessageReceiver
    {
        public void MessageReceived(Stream stream);
    }
}
