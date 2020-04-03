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
    public class VoxelDynamicBodyGenerator : ComputedComponentSystem<double>
    {
        private PhysicsSystem _physicsSystem;
        private List<Vector3i> _exposedVoxelsBuffer;

        public VoxelDynamicBodyGenerator(PhysicsSystem physicsSystem, World world) : base(world, typeof(VoxelGrid), typeof(Transform), typeof(VoxelDynamicBody))
        {
            _physicsSystem = physicsSystem;
            _exposedVoxelsBuffer = new List<Vector3i>();
        }

        protected override void Compute(double time, in Entity entity)
        {
            var voxels = entity.Get<VoxelGrid>();
            ref var body = ref entity.Get<VoxelDynamicBody>();
            var transform = entity.Get<Transform>();

            var size = voxels.VoxelSize;
            voxels.FindExposedBlocks((v, x, y, z) =>
            {
                _exposedVoxelsBuffer.Add(new Vector3i(x, y, z));
            });

            if (_exposedVoxelsBuffer.Count > 0)
            {
                var voxelIndicesByChildIndex = _exposedVoxelsBuffer.ToArray();

                var worldBodyOffset = Vector3.Transform(body.LocalBodyOffset, transform.WorldOrientation);

                using (var compoundBuilder = new CompoundBuilder(_physicsSystem.Pool, _physicsSystem.Simulation.Shapes, 8))
                {
                    for (int i = 0; i < _exposedVoxelsBuffer.Count; ++i)
                    {
                        var position = _exposedVoxelsBuffer[i];
                        var box = new Box(size, size, size);
                        var pose = new RigidPose(new Vector3(
                            position.X * size + size / 2,
                            position.Y * size + size / 2,
                            position.Z * size + size / 2));
                        compoundBuilder.Add(box, pose, 1);
                    }

                    compoundBuilder.BuildDynamicCompound(out var compoundChildren, out var compoundInertia, out var offset);
                    var compound = new BigCompound(compoundChildren, _physicsSystem.Simulation.Shapes, _physicsSystem.Pool);
                    var shape = _physicsSystem.AddShape(compound, entity);
                    var offsetDiff = offset - body.LocalBodyOffset;
                    body.LocalBodyOffset = offset;

                    if (body.VoxelBody.Exists)
                    {
                        _physicsSystem.Simulation.Bodies.SetShape(body.VoxelBody.Handle, shape);
                        _physicsSystem.Simulation.Bodies.SetLocalInertia(body.VoxelBody.Handle, compoundInertia);
                        body.VoxelBody.Pose.Position += offsetDiff;
                    }
                    else
                    {
                        var desc = BodyDescription.CreateDynamic(
                            new RigidPose(transform.WorldPosition + worldBodyOffset, transform.WorldOrientation),
                            compoundInertia,
                            new CollidableDescription(shape, 0.1f),
                            new BodyActivityDescription(-1));
                        body.VoxelBody = _physicsSystem.AddDynamic(desc, entity);
                    }
                }
            }

            _exposedVoxelsBuffer.Clear();
        }

        protected override void Remove(in Entity entity)
        {
            ref var body = ref entity.Get<VoxelStaticBody>();

            if (body.VoxelShape.Exists)
            {
                var oldShape = _physicsSystem.GetShape<BigCompound>(body.VoxelShape);
                oldShape.Dispose(_physicsSystem.Pool);
                _physicsSystem.RemoveShape<BigCompound>(body.VoxelShape);
            }

            if (body.VoxelStatic.Exists)
            {
                _physicsSystem.RemoveStatic(body.VoxelStatic);
            }

            body.VoxelIndicesByChildIndex = null;
        }
    }
}
