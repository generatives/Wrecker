using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Clunker.Editor;
using Clunker.Networking;
using Clunker.ECS;

namespace Clunker.Core
{
    [With(typeof(Transform))]
    [With(typeof(TransformLerp))]
    public class TransformLerper : AEntitySystem<double>
    {
        public float SpareTimeScaler { get; set; } = 0.2f;
        public float SpareTimeTarget { get; set; } = 0.065f;

        private NetworkedEntities _entities;

        public TransformLerper(NetworkedEntities networkedEntities, World world) : base(world)
        {
            _entities = networkedEntities;
        }

        protected override void Update(double deltaSeconds, in Entity entity)
        {
            ref var transform = ref entity.Get<Transform>();
            ref var lerp = ref entity.Get<TransformLerp>();

            Utilties.Logging.Metrics.LogMetric($"LogicSystems:TransformLerper:MessageCount", lerp.Messages.Count, TimeSpan.FromSeconds(5));

            if (!lerp.CurrentTarget.HasValue)
            {
                if (DequeueNextTarget(transform, ref lerp, in entity))
                {
                    entity.Set(transform);
                    entity.Set(lerp);
                }
                else
                {
                    return;
                }
            }

            var timeRemaining = lerp.CurrentTarget.Value.DeltaTime + lerp.Messages.Sum(m => m.DeltaTime);
            var frameTime = Math.Max((float)deltaSeconds + (timeRemaining - SpareTimeTarget) * SpareTimeScaler, 0.001f);
            while (frameTime > 0 && lerp.CurrentTarget.HasValue)
            {
                var serverFrameTime = lerp.CurrentTarget.Value.DeltaTime;
                var remainingOnTarget = serverFrameTime - lerp.Progress;

                if(remainingOnTarget > frameTime)
                {
                    LerpTransform(transform, lerp.CurrentTarget.Value, remainingOnTarget, frameTime);
                    entity.Set(transform);
                    lerp.Progress += frameTime;
                    entity.Set(lerp);
                }
                else
                {
                    LerpTransform(transform, lerp.CurrentTarget.Value, remainingOnTarget, remainingOnTarget);
                    entity.Set(transform);
                    frameTime = frameTime - remainingOnTarget;
                    DequeueNextTarget(transform, ref lerp, in entity);
                }
            }

        }

        private bool DequeueNextTarget(Transform transform, ref TransformLerp lerp, in Entity entity)
        {
            if (lerp.Messages.Any())
            {
                lerp.CurrentTarget = lerp.Messages.Dequeue();
                transform.Parent = lerp.CurrentTarget.Value.ParentId.HasValue ? _entities.GetEntity(lerp.CurrentTarget.Value.ParentId.Value).GetOrCreate<Transform>((e) => new Transform(e)) : null;
                lerp.Progress = 0;
                entity.Set(lerp);
                entity.Set(transform);
                return true;
            }
            else
            {
                if (lerp.CurrentTarget.HasValue || lerp.Progress != 0)
                {
                    lerp.CurrentTarget = null;
                    lerp.Progress = 0;
                    entity.Set(lerp);
                }
                return false;
            }
        }

        private void LerpTransform(Transform transform, TransformMessage target, float remainingTime, float deltaTime)
        {
            var multiplier = deltaTime / remainingTime;
            transform.Position += (target.Position - transform.Position) * multiplier;
            transform.Orientation += (target.Orientation - transform.Orientation) * multiplier;
            transform.Scale += (target.Scale - transform.Scale) * multiplier;
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
