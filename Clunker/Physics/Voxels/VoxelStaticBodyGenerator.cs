using BepuPhysics;
using BepuPhysics.Collidables;
using Clunker.Core;
using Clunker.ECS;
using Clunker.Geometry;
using Clunker.Voxels;
using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace Clunker.Physics.Voxels
{
    public class VoxelStaticBodyGenerator : ComponentChangeSystem<double>
    {
        private PhysicsSystem _physicsSystem;

        public VoxelStaticBodyGenerator(PhysicsSystem physicsSystem, World world) : base(world, typeof(ExposedVoxels), typeof(VoxelGrid), typeof(Transform), typeof(VoxelStaticBody))
        {
            _physicsSystem = physicsSystem;
        }

        protected override void Compute(double time, in Entity entity)
        {
            ref var exposedVoxels = ref entity.Get<ExposedVoxels>();

            if (exposedVoxels.Exposed.Count > 0)
            {
                ref var voxels = ref entity.Get<VoxelGrid>();
                ref var body = ref entity.Get<VoxelStaticBody>();
                var transform = entity.Get<Transform>();

                var size = voxels.VoxelSize;
                var exposed = exposedVoxels.Exposed;

                using (var compoundBuilder = new CompoundBuilder(_physicsSystem.Pool, _physicsSystem.Simulation.Shapes, 8))
                {
                    for (int i = 0; i < exposed.Count; ++i)
                    {
                        var position = exposed[i];
                        var box = new Box(size, size, size);
                        var pose = new RigidPose(new Vector3(
                            position.X * size + size / 2,
                            position.Y * size + size / 2,
                            position.Z * size + size / 2));
                        compoundBuilder.Add(box, pose, 1);
                    }

                    compoundBuilder.BuildKinematicCompound(out var compoundChildren, out var offset);

                    var shape = new BigCompound(compoundChildren, _physicsSystem.Simulation.Shapes, _physicsSystem.Pool);

                    if (body.VoxelShape.Exists)
                    {
                        var oldShape = _physicsSystem.GetShape<BigCompound>(body.VoxelShape);
                        oldShape.Dispose(_physicsSystem.Pool);
                        _physicsSystem.RemoveShape(body.VoxelShape);
                    }

                    body.VoxelShape = _physicsSystem.AddShape(shape, entity);

                    if (body.VoxelStatic.Exists)
                    {
                        _physicsSystem.RemoveStatic(body.VoxelStatic);
                    }
                    var transformedOffset = Vector3.Transform(offset, transform.WorldOrientation);
                    body.VoxelStatic = _physicsSystem.AddStatic(new StaticDescription(transform.WorldPosition + transformedOffset, new CollidableDescription(body.VoxelShape, 0.1f)), entity);
                }
            }
        }

        protected override void Remove(in Entity entity)
        {
            ref var body = ref entity.Get<VoxelStaticBody>();

            if (body.VoxelShape.Exists)
            {
                var oldShape = _physicsSystem.GetShape<BigCompound>(body.VoxelShape);
                oldShape.Dispose(_physicsSystem.Pool);
                _physicsSystem.RemoveShape(body.VoxelShape);
            }

            if (body.VoxelStatic.Exists)
            {
                _physicsSystem.RemoveStatic(body.VoxelStatic);
            }
        }
    }
}
