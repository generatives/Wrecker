using Clunker.Core;
using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Physics
{
    public class DynamicBodyPositionSync : AEntitySystem<double>
    {
        public DynamicBodyPositionSync(World world) : base(world.GetEntities().With<Transform>().With<DynamicBody>().AsSet())
        {

        }

        protected override void Update(double state, in Entity entity)
        {
            ref var body = ref entity.Get<DynamicBody>();
            ref var transform = ref entity.Get<Transform>();

            if(body.Body.Exists && body.Body.Awake)
            {
                transform.WorldOrientation = body.Body.Pose.Orientation;

                var worldBodyOffset = Vector3.Transform(body.BodyOffset, transform.WorldOrientation);
                transform.WorldPosition = body.Body.Pose.Position - worldBodyOffset;
                entity.Set(transform);
            }
        }
    }
}
