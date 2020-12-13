using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Clunker.Networking
{
    public abstract class MessagePackMessageReciever<T> : IMessageReceiver
    {
        public void MessageReceived(Stream stream)
        {
            var message = Serializer.Deserialize<T>(stream);
            MessageReceived(message);
        }

        protected abstract void MessageReceived(in T message);
    }
}
