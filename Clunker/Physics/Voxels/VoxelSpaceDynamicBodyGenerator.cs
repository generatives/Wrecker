using BepuPhysics;
using BepuPhysics.Collidables;
using Clunker.Core;
using Clunker.ECS;
using Clunker.Geometry;
using Clunker.Voxels;
using Clunker.Voxels.Space;
using Collections.Pooled;
using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Physics.Voxels
{
    public class VoxelSpaceDynamicBodyGenerator : ComputedComponentSystem<double>
    {
        private PhysicsSystem _physicsSystem;

        public VoxelSpaceDynamicBodyGenerator(PhysicsSystem physicsSystem, World world) : base(world, typeof(VoxelSpace), typeof(Transform), typeof(VoxelSpaceDynamicBody))
        {
            _physicsSystem = physicsSystem;
        }

        protected override void Compute(double time, in Entity entity)
        {
            ref var transform = ref entity.Get<Transform>();
            ref var space = ref entity.Get<VoxelSpace>();
            ref var spaceBody = ref entity.Get<VoxelSpaceDynamicBody>();
            ref var body = ref entity.Get<DynamicBody>();

            using (var compoundBuilder = new CompoundBuilder(_physicsSystem.Pool, _physicsSystem.Simulation.Shapes, 8))
            {
                spaceBody.VoxelIndicesByChildIndex = spaceBody.VoxelIndicesByChildIndex ?? new PooledList<Vector3i>();
                spaceBody.VoxelIndicesByChildIndex.Clear();
                foreach (var kvp in space.Members)
                {
                    var memberIndex = kvp.Key;
                    var member = kvp.Value;
                    if(member.Has<ExposedVoxels>() && member.Has<Transform>())
                    {
                        var memberTransform = member.Get<Transform>();
                        var exposedVoxels = member.Get<ExposedVoxels>().Exposed;

                        var size = space.VoxelSize;
                        for (int i = 0; i < exposedVoxels.Count; ++i)
                        {
                            var voxelIndex = exposedVoxels[i];
                            var box = new Box(size, size, size);
                            var position = memberTransform.Position + new Vector3(voxelIndex.X * size + size / 2, voxelIndex.Y * size + size / 2, voxelIndex.Z * size + size / 2);
                            var pose = new RigidPose(position);
                            compoundBuilder.Add(box, pose, 10);
                            spaceBody.VoxelIndicesByChildIndex.Add(space.GetSpaceIndexFromVoxelIndex(memberIndex, voxelIndex));
                        }
                    }
                }

                if(spaceBody.VoxelIndicesByChildIndex.Count > 0)
                {
                    compoundBuilder.BuildDynamicCompound(out var compoundChildren, out var compoundInertia, out var offset);
                    spaceBody.VoxelCompound = new BigCompound(compoundChildren, _physicsSystem.Simulation.Shapes, _physicsSystem.Pool);
                    spaceBody.VoxelShape = _physicsSystem.AddShape(spaceBody.VoxelCompound, entity);
                    var offsetDiff = offset - body.BodyOffset;
                    body.BodyOffset = offset;

                    if (body.Body.Exists)
                    {
                        _physicsSystem.Simulation.Bodies.SetShape(body.Body.Handle, spaceBody.VoxelShape);
                        _physicsSystem.Simulation.Bodies.SetLocalInertia(body.Body.Handle, compoundInertia);
                        body.Body.Pose.Position += offsetDiff;
                    }
                    else
                    {
                        var worldBodyOffset = body.GetWorldBodyOffset(transform);
                        var desc = BodyDescription.CreateDynamic(
                            new RigidPose(transform.WorldPosition + worldBodyOffset, transform.WorldOrientation),
                            compoundInertia,
                            new CollidableDescription(spaceBody.VoxelShape, 0.1f),
                            new BodyActivityDescription(-1));
                        body.Body = _physicsSystem.AddDynamic(desc, entity);
                    }
                }
                else if(body.Body.Exists)
                {
                    spaceBody.VoxelCompound.Dispose(_physicsSystem.Pool);
                    _physicsSystem.RemoveShape(spaceBody.VoxelShape);
                    _physicsSystem.RemoveDynamic(body.Body);
                    spaceBody.VoxelIndicesByChildIndex.Clear();
                }
            }
        }

        protected override void Remove(in Entity entity)
        {
            ref var spaceBody = ref entity.Get<VoxelSpaceDynamicBody>();
            ref var body = ref entity.Get<DynamicBody>();

            if (body.Body.Exists)
            {
                spaceBody.VoxelCompound.Dispose(_physicsSystem.Pool);
                _physicsSystem.RemoveShape(spaceBody.VoxelShape);
                _physicsSystem.RemoveDynamic(body.Body);
                spaceBody.VoxelIndicesByChildIndex.Clear();
            }
        }
    }
}
