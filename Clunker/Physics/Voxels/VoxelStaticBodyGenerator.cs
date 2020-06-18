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

        List<double> _times = new List<double>();
        List<int> _shapes = new List<int>();

        public VoxelStaticBodyGenerator(PhysicsSystem physicsSystem, World world) : base(world, typeof(ExposedVoxels), typeof(VoxelGrid), typeof(Transform), typeof(VoxelStaticBody))
        {
            _physicsSystem = physicsSystem;
        }

        protected override void Compute(double time, in Entity entity)
        {
            var watch = Stopwatch.StartNew();

            ref var voxels = ref entity.Get<VoxelGrid>();
            ref var body = ref entity.Get<VoxelStaticBody>();
            var transform = entity.Get<Transform>();

            var size = voxels.VoxelSize;

            var num = 0;
            using (var compoundBuilder = new CompoundBuilder(_physicsSystem.Pool, _physicsSystem.Simulation.Shapes, 8))
            {
                var any = false;
                GreedyBlockFinder.FindBlocks(voxels, (blockType, position, size) =>
                {
                    var box = new Box(size.X, size.Y, size.Z);
                    var pose = new RigidPose(new Vector3(
                        position.X + size.X / 2f,
                        position.Y + size.Y / 2f,
                        position.Z + size.Z / 2f));
                    compoundBuilder.Add(box, pose, size.X * size.Y * size.Z);
                    any = true;
                    num++;
                });

                if (any)
                {
                    //_shapes.Add(num);
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

            watch.Stop();
            if(num > 0)
            {
                //_times.Add(watch.Elapsed.TotalMilliseconds);
                //var avgNum = _shapes.Any() ? _shapes.Average() : 0;
                //var avgTime = _times.Skip(10).Any() ? _times.Skip(10).Average() : 0;

                //Console.WriteLine($"{avgNum} {avgTime}");
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
