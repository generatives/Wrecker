using BepuPhysics;
using BepuPhysics.Collidables;
using Clunker.Core;
using Clunker.ECS;
using Clunker.Geometry;
using Clunker.Voxels;
using Clunker.Voxels.Meshing;
using Collections.Pooled;
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
    public class VoxelStaticSmoothBodyGenerator : ComponentChangeSystem<double>
    {
        private PhysicsSystem _physicsSystem;

        private List<double> _times = new List<double>();

        public VoxelStaticSmoothBodyGenerator(PhysicsSystem physicsSystem, World world) : base(world, typeof(PhysicsBlocks), typeof(VoxelGrid), typeof(Transform), typeof(VoxelStaticBody))
        {
            _physicsSystem = physicsSystem;
        }

        protected override void Compute(double time, in Entity entity)
        {
            //var watch = Stopwatch.StartNew();

            ref var voxels = ref entity.Get<VoxelGrid>();
            ref var body = ref entity.Get<VoxelStaticBody>();
            ref var physicsBlocks = ref entity.Get<PhysicsBlocks>();
            var transform = entity.Get<Transform>();
            
            if (body.VoxelShape.Exists)
            {
                var oldShape = _physicsSystem.GetShape<Mesh>(body.VoxelShape);
                oldShape.Dispose(_physicsSystem.Pool);
                _physicsSystem.RemoveShape(body.VoxelShape);
            }

            if (body.VoxelStatic.Exists)
            {
                _physicsSystem.RemoveStatic(body.VoxelStatic);
            }

            using var triangles = new PooledList<BepuPhysics.Collidables.Triangle>();

            MarchingCubesGenerator.GenerateMesh(voxels, (tri) =>
            {
                triangles.Add(new BepuPhysics.Collidables.Triangle(tri.A, tri.B, tri.C));
            });

            if (triangles.Count > 0)
            {
                _physicsSystem.Pool.Take<BepuPhysics.Collidables.Triangle>(triangles.Count, out var buffer);

                for (int i = 0; i < triangles.Count; i++)
                {
                    buffer[i] = triangles[i];
                }

                var mesh = new Mesh(buffer, Vector3.One, _physicsSystem.Pool);

                body.VoxelShape = _physicsSystem.AddShape(mesh, entity);
                var transformedOffset = Vector3.Transform(Vector3.Zero, transform.WorldOrientation);
                body.VoxelStatic = _physicsSystem.AddStatic(new StaticDescription(transform.WorldPosition + transformedOffset, new CollidableDescription(body.VoxelShape, 0.1f)), entity);
            }
        }

        protected override void Remove(in Entity entity)
        {
            ref var body = ref entity.Get<VoxelStaticBody>();

            if (body.VoxelShape.Exists)
            {
                var oldShape = _physicsSystem.GetShape<Mesh>(body.VoxelShape);
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
