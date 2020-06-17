using BepuPhysics;
using BepuPhysics.Collidables;
using Clunker.Core;
using Clunker.ECS;
using Clunker.Geometry;
using Clunker.Voxels;
using Clunker.Voxels.Meshing;
using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            ref var voxels = ref entity.Get<VoxelGrid>();
            ref var body = ref entity.Get<VoxelStaticBody>();
            var transform = entity.Get<Transform>();

            var size = voxels.VoxelSize;

            using (var compoundBuilder = new CompoundBuilder(_physicsSystem.Pool, _physicsSystem.Simulation.Shapes, 8))
            {
                var any = false;
                GreedyBlockFinder.GenerateMesh(voxels, (blockType, position, size) =>
                {
                    var box = new Box(size.X, size.Y, size.Z);
                    var pose = new RigidPose(new Vector3(
                        position.X + size.X / 2f,
                        position.Y + size.Y / 2f,
                        position.Z + size.Z / 2f));
                    compoundBuilder.Add(box, pose, size.X * size.Y * size.Z);
                    any = true;
                });

                if (any)
                {
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
