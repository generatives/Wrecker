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

        public VoxelStaticBodyGenerator(PhysicsSystem physicsSystem, World world) : base(world, typeof(PhysicsBlocks), typeof(VoxelGrid), typeof(Transform), typeof(VoxelStaticBody))
        {
            _physicsSystem = physicsSystem;
        }

        protected override void Compute(double time, in Entity entity)
        {
            var watch = Stopwatch.StartNew();

            ref var voxels = ref entity.Get<VoxelGrid>();
            ref var body = ref entity.Get<VoxelStaticBody>();
            ref var physicsBlocks = ref entity.Get<PhysicsBlocks>();
            var transform = entity.Get<Transform>();

            var size = voxels.VoxelSize;

            if(physicsBlocks.Blocks.Any())
            {
                using (var compoundBuilder = new CompoundBuilder(_physicsSystem.Pool, _physicsSystem.Simulation.Shapes, 8))
                {
                    foreach (var block in physicsBlocks.Blocks)
                    {
                        var box = new Box(block.Size.X, block.Size.Y, block.Size.Z);
                        var pose = new RigidPose(new Vector3(
                            block.Index.X + block.Size.X / 2f,
                            block.Index.Y + block.Size.Y / 2f,
                            block.Index.Z + block.Size.Z / 2f));
                        compoundBuilder.Add(box, pose, block.Size.X * block.Size.Y * block.Size.Z);
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
