using DefaultEcs;
using DefaultEcs.System;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;
using Clunker.Networking;

namespace Clunker.Graphics
{
    [MessagePackObject]
    public struct CameraMessage
    {
    }


    public class CameraServerSystem : ISystem<double>
    {
        private EntitySet _cameras;

        public bool IsEnabled { get; set; } = false;

        public CameraServerSystem(World world)
        {
            _cameras = world.GetEntities().With<NetworkedEntity>().With<Camera>().AsSet();
        }

        [Subscribe]
        public void On(in NewClientConnected clientConnected)
        {
            foreach(var entity in _cameras.GetEntities())
            {
                var id = entity.Get<NetworkedEntity>().Id;
                var message = new EntityMessage<CameraMessage>() { Id = id, Data = new CameraMessage() };

                var target = clientConnected.Entity.Get<ClientMessagingTarget>();
                target.Channel.AddBuffered<CameraMessageApplier, EntityMessage<CameraMessage>>(message);
            }
        }

        public void Update(double state)
        {
        }

        public void Dispose()
        {
            _cameras.Dispose();
        }
    }

    public class CameraMessageApplier : EntityMessageApplier<CameraMessage>
    {
        public CameraMessageApplier(NetworkedEntities entities) : base(entities) { }

        protected override void MessageReceived(in CameraMessage action, in Entity entity)
        {
            entity.Set(new Camera());
        }
    }
}
