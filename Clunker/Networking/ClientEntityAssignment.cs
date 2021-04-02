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
            entity.Set(new DirectionalLight()
            {
                ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(0.2f, 1.0f, 0.1f, 128f),
                LightProperties = new Clunker.Graphics.Data.LightProperties()
                {
                    NearStrength = 15,
                    FarStrength = 0,
                    MinDistance = 0,
                    MaxDistance = 64f
                }
            });
        }
    }
}
