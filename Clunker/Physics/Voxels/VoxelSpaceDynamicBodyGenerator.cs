using BepuPhysics;
using BepuPhysics.Collidables;
using Clunker.Core;
using Clunker.ECS;
using Clunker.Geometry;
using Clunker.Voxels;
using Clunker.Voxels.Meshing;
using Clunker.Voxels.Space;
using Collections.Pooled;
using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Clunker.Physics.Voxels
{
    public class VoxelSpaceDynamicBodyGenerator : ComponentChangeSystem<double>
    {
        private PhysicsSystem _physicsSystem;

        public VoxelSpaceDynamicBodyGenerator(PhysicsSystem physicsSystem, World world) : base(world, typeof(VoxelSpace), typeof(Transform), typeof(VoxelSpaceDynamicBody), typeof(DynamicBody))
        {
            _physicsSystem = physicsSystem;
        }

        protected override void Compute(double time, in Entity entity)
        {
            ref var transform = ref entity.Get<Transform>();
            ref var space = ref entity.Get<VoxelSpace>();
            ref var spaceBody = ref entity.Get<VoxelSpaceDynamicBody>();
            ref var body = ref entity.Get<DynamicBody>();
            body.LockedAxis = (true, false, true);
            body.Gravity = Vector3.Zero;

            using (var compoundBuilder = new CompoundBuilder(_physicsSystem.Pool, _physicsSystem.Simulation.Shapes, 8))
            {
                var any = false;
                foreach (var kvp in space.Members)
                {
                    var memberIndex = kvp.Key;
                    var member = kvp.Value;
                    if(member.Has<PhysicsBlocks>() && member.Has<Transform>())
                    {
                        var voxels = member.Get<VoxelGrid>();
                        var memberTransform = member.Get<Transform>();
                        var physicsBlocks = member.Get<PhysicsBlocks>();

                        if(physicsBlocks.Blocks?.Any() ?? false)
                        {
                            var size = space.VoxelSize;
                            foreach (var block in physicsBlocks.Blocks)
                            {
                                var box = new Box(block.Size.X, block.Size.Y, block.Size.Z);
                                var position = memberTransform.Position + new Vector3(
                                    block.Index.X + block.Size.X / 2f,
                                    block.Index.Y + block.Size.Y / 2f,
                                    block.Index.Z + block.Size.Z / 2f);
                                var pose = new RigidPose(position);
                                compoundBuilder.Add(box, pose, block.Size.X * block.Size.Y * block.Size.Z);
                                any = true;
                            }
                        }
                    }
                }

                if(any)
                {
                    compoundBuilder.BuildDynamicCompound(out var compoundChildren, out var compoundInertia, out var offset);
                    spaceBody.VoxelCompound = new BigCompound(compoundChildren, _physicsSystem.Simulation.Shapes, _physicsSystem.Pool);
                    spaceBody.VoxelShape = _physicsSystem.AddShape(spaceBody.VoxelCompound, entity);
                    var offsetDiff = offset - body.BodyOffset;
                    body.BodyOffset = offset;

                    if(body.LockedAxis.HasValue)
                    {
                        var locked = body.LockedAxis.Value;
                        var tensor = compoundInertia.InverseInertiaTensor;
                        compoundInertia.InverseInertiaTensor = new BepuUtilities.Symmetric3x3()
                        {
                            XX = locked.X ? 0 : tensor.XX,
                            YX = 0,
                            YY = locked.Y ? 0 : tensor.YY,
                            ZX = 0,
                            ZY = 0,
                            ZZ = locked.Z ? 0 : tensor.ZZ,
                        };
                    }

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
            }
        }
    }
}
