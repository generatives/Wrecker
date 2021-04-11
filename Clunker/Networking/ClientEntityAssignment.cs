using Clunker.Core;
using Clunker.Geometry;
using Clunker.Graphics;
using Clunker.Graphics.Components;
using DefaultEcs;
using DefaultEcs.System;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Networking
{

    [MessagePackObject]
    public struct ClientEntityAssignment
    {
    }

    public class ClientEntityAssignmentSystem : ISystem<double>
    {
        public bool IsEnabled { get; set; } = false;

        [Subscribe]
        public void On(in NewClientConnected newClientConnected)
        {
            var target = newClientConnected.Entity.Get<ClientMessagingTarget>();
            var networkedEntity = newClientConnected.Entity.Get<NetworkedEntity>();

            target.Channel.AddBuffered<ClientEntityAssignmentApplier, EntityMessage<ClientEntityAssignment>>(new EntityMessage<ClientEntityAssignment>(networkedEntity.Id, new ClientEntityAssignment()));
        }

        public void Update(double state)
        {
        }

        public void Dispose()
        {
        }
    }

    public class ClientEntityAssignmentApplier : EntityMessageApplier<ClientEntityAssignment>
    {
        public ClientEntityAssignmentApplier(NetworkedEntities entities) : base(entities)
        {
        }

        protected override void MessageReceived(in ClientEntityAssignment messageData, in Entity entity)
        {
            entity.Set(new Camera());
        }
    }
}
