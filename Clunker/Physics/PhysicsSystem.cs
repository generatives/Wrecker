using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.CollisionDetection.CollisionTasks;
using BepuPhysics.CollisionDetection.SweepTasks;
using BepuPhysics.Constraints;
using BepuPhysics.Trees;
using BepuUtilities.Memory;
using Clunker.Physics.CharacterController;
using Clunker.SceneGraph;
using Clunker.SceneGraph.SceneSystemInterfaces;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Clunker.Physics
{
    public class PhysicsSystem : SceneSystem, IUpdatableSystem, IDisposable
    {
        private Simulation _simulation;
        private SimpleThreadDispatcher _threadDispatcher;
        public BufferPool Pool { get; private set; }

        private CharacterControllers _characters;

        public PhysicsSystem()
        {
            //The buffer pool is a source of raw memory blobs for the engine to use.
            Pool = new BufferPool();
            _characters = new CharacterControllers(Pool);
            //Note that you can also control the order of internal stage execution using a different ITimestepper implementation.
            //For the purposes of this demo, we just use the default by passing in nothing (which happens to be PositionFirstTimestepper at the time of writing).
            _simulation = Simulation.Create(Pool, new CharacterNarrowphaseCallbacks(_characters), new PoseIntegratorCallbacks(new Vector3(0, -20, 0)));

            //The narrow phase must be notified about the existence of the new collidable type. For every pair type we want to support, a collision task must be registered.
            //All of the default engine types are registered upon simulation creation by a call to DefaultTypes.CreateDefaultCollisionTaskRegistry.
            _simulation.NarrowPhase.CollisionTaskRegistry.Register(new ConvexCompoundCollisionTask<Sphere, VoxelCollidable, ConvexCompoundOverlapFinder<Sphere, SphereWide, VoxelCollidable>, ConvexVoxelsContinuations, NonconvexReduction>());
            _simulation.NarrowPhase.CollisionTaskRegistry.Register(new ConvexCompoundCollisionTask<Capsule, VoxelCollidable, ConvexCompoundOverlapFinder<Capsule, CapsuleWide, VoxelCollidable>, ConvexVoxelsContinuations, NonconvexReduction>());
            _simulation.NarrowPhase.CollisionTaskRegistry.Register(new ConvexCompoundCollisionTask<Box, VoxelCollidable, ConvexCompoundOverlapFinder<Box, BoxWide, VoxelCollidable>, ConvexVoxelsContinuations, NonconvexReduction>());
            _simulation.NarrowPhase.CollisionTaskRegistry.Register(new ConvexCompoundCollisionTask<Cylinder, VoxelCollidable, ConvexCompoundOverlapFinder<Cylinder, CylinderWide, VoxelCollidable>, ConvexVoxelsContinuations, NonconvexReduction>());
            _simulation.NarrowPhase.CollisionTaskRegistry.Register(new ConvexCompoundCollisionTask<Triangle, VoxelCollidable, ConvexCompoundOverlapFinder<Triangle, TriangleWide, VoxelCollidable>, ConvexVoxelsContinuations, NonconvexReduction>());

            _simulation.NarrowPhase.CollisionTaskRegistry.Register(new CompoundPairCollisionTask<Compound, VoxelCollidable, CompoundPairOverlapFinder<Compound, VoxelCollidable>, CompoundVoxelsContinuations<Compound>, NonconvexReduction>());
            _simulation.NarrowPhase.CollisionTaskRegistry.Register(new CompoundPairCollisionTask<BigCompound, VoxelCollidable, CompoundPairOverlapFinder<BigCompound, VoxelCollidable>, CompoundVoxelsContinuations<BigCompound>, NonconvexReduction>());

            //Note that this demo excludes mesh-voxels and voxels-voxels pairs. Those get a little more complicated since there's some gaps in the pre-built helpers.
            //If you wanted to make your own, look into the various types related to meshes. They're a good starting point, although I'm not exactly happy with the complexity of the
            //current design. They might receive some significant changes- keep that in mind if you create anything which depends heavily on their current implementation.

            //To support sweep tests, we must also register sweep tasks. No extra work is required to support these; the interface implementation on the shape is good enough.
            _simulation.NarrowPhase.SweepTaskRegistry.Register(new ConvexHomogeneousCompoundSweepTask<Sphere, SphereWide, VoxelCollidable, Box, BoxWide, ConvexCompoundSweepOverlapFinder<Sphere, VoxelCollidable>>());
            _simulation.NarrowPhase.SweepTaskRegistry.Register(new ConvexHomogeneousCompoundSweepTask<Capsule, CapsuleWide, VoxelCollidable, Box, BoxWide, ConvexCompoundSweepOverlapFinder<Capsule, VoxelCollidable>>());
            _simulation.NarrowPhase.SweepTaskRegistry.Register(new ConvexHomogeneousCompoundSweepTask<Box, BoxWide, VoxelCollidable, Box, BoxWide, ConvexCompoundSweepOverlapFinder<Box, VoxelCollidable>>());
            _simulation.NarrowPhase.SweepTaskRegistry.Register(new ConvexHomogeneousCompoundSweepTask<Cylinder, CylinderWide, VoxelCollidable, Box, BoxWide, ConvexCompoundSweepOverlapFinder<Cylinder, VoxelCollidable>>());
            _simulation.NarrowPhase.SweepTaskRegistry.Register(new ConvexHomogeneousCompoundSweepTask<Triangle, TriangleWide, VoxelCollidable, Box, BoxWide, ConvexCompoundSweepOverlapFinder<Triangle, VoxelCollidable>>());

            _simulation.NarrowPhase.SweepTaskRegistry.Register(new CompoundHomogeneousCompoundSweepTask<Compound, VoxelCollidable, Box, BoxWide, CompoundPairSweepOverlapFinder<Compound, VoxelCollidable>>());
            _simulation.NarrowPhase.SweepTaskRegistry.Register(new CompoundHomogeneousCompoundSweepTask<BigCompound, VoxelCollidable, Box, BoxWide, CompoundPairSweepOverlapFinder<BigCompound, VoxelCollidable>>());
            //Supporting voxels-mesh and voxels-voxels would again require a bit more effort, though a bit less than the collision task equivalents would.

            //Drop a ball on a big static box.
            var sphere = new Sphere(1);
            sphere.ComputeInertia(1, out var sphereInertia);
            
            _threadDispatcher = new SimpleThreadDispatcher(Environment.ProcessorCount);
        }

        public CharacterControllerRef CreateCharacter(Vector3 position, Capsule capsule, float speculativeMargin, float mass, float maximumHorizontalForce,
            float maximumVerticalGlueForce, float jumpVelocity, float speed, float maximumSlope)
        {
            return new CharacterControllerRef(_characters, position, capsule, speculativeMargin, mass, maximumHorizontalForce, maximumVerticalGlueForce, jumpVelocity, speed, 0.75f, 0.75f, maximumSlope);
        }

        public int AddStatic(StaticDescription description)
        {
            return _simulation.Statics.Add(description);
        }

        public BodyReference AddDynamic(BodyDescription description)
        {
            var handle = _simulation.Bodies.Add(description);
            return new BodyReference(handle, _simulation.Bodies);
        }

        public TypedIndex AddShape<TShape>(TShape shape) where TShape : struct, IShape
        {
            return _simulation.Shapes.Add(shape);
        }

        public void RemoveStatic(int handle)
        {
            _simulation.Statics.Remove(handle);
        }

        public void RemoveDynamic(BodyReference body)
        {
            _simulation.Bodies.Remove(body.Handle);
        }

        public void RemoveShape(TypedIndex shapeIndex)
        {
            _simulation.Shapes.Remove(shapeIndex);
        }

        public BodyReference GetBodyReference(int handle)
        {
            return new BodyReference(handle, _simulation.Bodies);
        }

        public StaticReference GetStaticReference(int handle)
        {
            return new StaticReference(handle, _simulation.Statics);
        }

        public T GetShape<T>(int typeIndex) where T : struct, IShape
        {
            return _simulation.Shapes.GetShape<T>(typeIndex);
        }

        public void Raycast<THitHandler>(in Vector3 origin, in Vector3 direction, float maximumT, ref THitHandler hitHandler, int id = 0) where THitHandler : IRayHitHandler
        {
            _simulation.RayCast<THitHandler>(origin, direction, maximumT, ref hitHandler, id);
        }

        public void Update(float time)
        {
            _simulation.Timestep(time);
        }

        public void Dispose()
        {
            _characters.Dispose();
            //If you intend to reuse the BufferPool, disposing the simulation is a good idea- it returns all the buffers to the pool for reuse.
            //Here, we dispose it, but it's not really required; we immediately thereafter clear the BufferPool of all held memory.
            //Note that failing to dispose buffer pools can result in memory leaks.
            _simulation.Dispose();
            _threadDispatcher.Dispose();
            Pool.Clear();
        }
    }
}
