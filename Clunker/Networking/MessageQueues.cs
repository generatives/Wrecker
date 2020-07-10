using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Networking
{
    public enum MessageType
    {
        GENERIC, VOXEL
    }

    public class MessageQueues
    {
        public List<object> GenericQueue { get; private set; } = new List<object>();
        public List<object> VoxelQueue { get; private set; } = new List<object>();

        public void Send(object message, MessageType type)
        {
            switch(type)
            {
                case MessageType.VOXEL:
                    VoxelQueue.Add(message);
                    break;
                case MessageType.GENERIC:
                default:
                    GenericQueue.Add(message);
                    break;
            }
        }
    }
}
