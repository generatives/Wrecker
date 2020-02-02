using BepuPhysics;
using BepuPhysics.Collidables;
using Clunker.Core;
using Clunker.Geometry;
using Clunker.Voxels;
using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Physics.Voxels
{
    public class VoxelShapeGenerator : AEntitySystem<double>
    {
        private PhysicsSystem _physicsSystem;

        public VoxelShapeGenerator(PhysicsSystem physicsSystem, World world) : base(world.GetEntities().With<Transform>().With<VoxelBody>().WhenAdded<VoxelGrid>().WhenChanged<VoxelGrid>().AsSet())
        {
            _physicsSystem = physicsSystem;
        }

        protected override void Update(double state, in Entity entity)
        {
            var voxels = entity.Get<VoxelGrid>();
            var body = entity.Get<VoxelBody>();
            var transform = entity.Get<Transform>();

            var size = voxels.VoxelSize;
            var exposedVoxels = new List<Vector3i>(voxels.GridSize * voxels.GridSize * voxels.GridSize / 6);
            voxels.FindExposedBlocks((v, x, y, z) =>
            {
                exposedVoxels.Add(new Vector3i(x, y, z));
            });

            if(exposedVoxels.Count > 0)
            {
                var voxelIndicesByChildIndex = exposedVoxels.ToArray();

                using (var compoundBuilder = new CompoundBuilder(_physicsSystem.Pool, _physicsSystem.Simulation.Shapes, 8))
                {
                    for (int i = 0; i < exposedVoxels.Count; ++i)
                    {
                        var position = exposedVoxels[i];
                        var box = new Box(size, size, size);
                        var pose = new RigidPose(new Vector3(
                            position.X * size + size / 2,
                            position.Y * size + size / 2,
                            position.Z * size + size / 2));
                        compoundBuilder.Add(box, pose, 1);
                    }

                    compoundBuilder.BuildDynamicCompound(out var compoundChildren, out var compoundInertia, out var offset);

                    var shape = new BigCompound(compoundChildren, _physicsSystem.Simulation.Shapes, _physicsSystem.Pool);

                    if (body.VoxelShape.Exists)
                    {
                        _physicsSystem.RemoveShape(body.VoxelShape);
                    }

                    body.VoxelShape = _physicsSystem.AddShape(shape);
                    body.VoxelIndicesByChildIndex = voxelIndicesByChildIndex;

                    if (body.VoxelStatic.Exists)
                    {
                        _physicsSystem.RemoveStatic(body.VoxelStatic);
                    }
                    var transformedOffset = Vector3.Transform(offset, transform.WorldOrientation);
                    body.VoxelStatic = _physicsSystem.AddStatic(new StaticDescription(transform.WorldPosition + transformedOffset, new CollidableDescription(body.VoxelShape, 0.1f)), entity);

                    entity.Set(body);
                }
            }
        }
    }
}
