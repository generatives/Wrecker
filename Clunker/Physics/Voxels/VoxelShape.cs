using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Clunker.Math;
using Clunker.Physics;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
using Clunker.Voxels;
using Clunker.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Clunker.Physics.Voxels
{
    public class VoxelShape : Component, IComponentEventListener
    {
        public event EventHandler<NewVoxelShapeArgs> ColliderGenerated;

        public NewVoxelShapeArgs ShapeArgs { get; private set; }

        public void ComponentStarted()
        {
            var voxels = GameObject.GetComponent<VoxelGrid>();
            voxels.VoxelsChanged += Voxels_VoxelsChanged;
            Voxels_VoxelsChanged(voxels);
        }

        public void ComponentStopped()
        {
            var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();
            ShapeArgs.shape.Dispose(physicsSystem.Pool);
        }

        private void Voxels_VoxelsChanged(VoxelGrid voxels)
        {
            var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();
            var chunk = GameObject.GetComponent<Chunk>();
            if (voxels.Data.Any(t => t.Item2.Exists))
            {
                EnqueueBestEffortFrameJob(() =>
                {
                    ShapeArgs = CreateCollisionShape(voxels);
                    ColliderGenerated?.Invoke(this, ShapeArgs);
                });
            }
        }

        private NewVoxelShapeArgs CreateCollisionShape(VoxelGrid space)
        {
            var physicsSystem = GameObject.CurrentScene.GetOrCreateSystem<PhysicsSystem>();

            var voxels = space.Data;
            var size = voxels.VoxelSize;
            var exposedVoxels = new List<Vector3i>(voxels.XLength * voxels.YLength * voxels.ZLength / 6);
            voxels.FindExposedBlocks((v, x, y, z) =>
            {
                exposedVoxels.Add(new Vector3i(x, y, z));
            });

            var voxelIndicesByChildIndex = exposedVoxels.ToArray();

            using (var compoundBuilder = new CompoundBuilder(physicsSystem.Pool, physicsSystem.Simulation.Shapes, 8))
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
                var args = new NewVoxelShapeArgs()
                {
                    shape = new BigCompound(compoundChildren, physicsSystem.Simulation.Shapes, physicsSystem.Pool),
                    inertia = compoundInertia,
                    offset = offset,
                    voxelIndicesByChildIndex = voxelIndicesByChildIndex
                };
                return args;
            }
        }
    }

    public class NewVoxelShapeArgs
    {
        public BigCompound shape;
        public BodyInertia inertia;
        public Vector3 offset;
        public Vector3i[] voxelIndicesByChildIndex;
    }
}
