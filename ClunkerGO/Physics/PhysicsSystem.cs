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
        public Simulation Simulation { get; private set; }
        private SimpleThreadDispatcher _threadDispatcher;
        public BufferPool Pool { get; private set; }

        private CharacterControllers _characters;

        private Dictionary<int, object> _staticContexts;
        private Dictionary<int, object> _dynamicContexts;
        private Dictionary<TypedIndex, object> _shapeContexts;

        public PhysicsSystem()
        {
            //The buffer pool is a source of raw memory blobs for the engine to use.
            Pool = new BufferPool();
            _characters = new CharacterControllers(Pool);
            //Note that you can also control the order of internal stage execution using a different ITimestepper implementation.
            //For the purposes of this demo, we just use the default by passing in nothing (which happens to be PositionFirstTimestepper at the time of writing).
            Simulation = Simulation.Create(Pool, new CharacterNarrowphaseCallbacks(_characters), new PoseIntegratorCallbacks(new Vector3(0, 0, 0)));

            //Drop a ball on a big static box.
            var sphere = new Sphere(1);
            sphere.ComputeInertia(1, out var sphereInertia);
            
            _threadDispatcher = new SimpleThreadDispatcher(Environment.ProcessorCount);

            _staticContexts = new Dictionary<int, object>();
            _dynamicContexts = new Dictionary<int, object>();
            _shapeContexts = new Dictionary<TypedIndex, object>();
        }

        public CharacterControllerRef CreateCharacter(Vector3 position, Capsule capsule, float speculativeMargin, float mass, float maximumHorizontalForce,
            float maximumVerticalGlueForce, float jumpVelocity, float speed, float maximumSlope)
        {
            return new CharacterControllerRef(_characters, position, capsule, speculativeMargin, mass, maximumHorizontalForce, maximumVerticalGlueForce, jumpVelocity, speed, 0.75f, 0.75f, maximumSlope);
        }

        public StaticReference AddStatic(StaticDescription description, object context = null)
        {
            var handle = Simulation.Statics.Add(description);
            if(context != null)
            {
                _staticContexts[handle] = context;
            }
            return new StaticReference(handle, Simulation.Statics);
        }

        public object GetStaticContext(int handle) => _staticContexts.ContainsKey(handle) ? _staticContexts[handle] : null;
        public object GetStaticContext(StaticReference reference) => GetStaticContext(reference.Handle);

        public BodyReference AddDynamic(BodyDescription description, object context = null)
        {
            var handle = Simulation.Bodies.Add(description);
            if (context != null)
            {
                _dynamicContexts[handle] = context;
            }
            return new BodyReference(handle, Simulation.Bodies);
        }

        public object GetDynamicContext(int handle) => _dynamicContexts.ContainsKey(handle) ? _dynamicContexts[handle] : null;
        public object GetDynamicContext(BodyReference reference) => GetDynamicContext(reference.Handle);

        public TypedIndex AddShape<TShape>(TShape shape, object context = null) where TShape : unmanaged, IShape
        {
            var index = Simulation.Shapes.Add(shape);
            if (context != null)
            {
                _shapeContexts[index] = context;
            }
            return index;
        }

        public object GetShapeContext(TypedIndex index) => _shapeContexts.ContainsKey(index) ? _shapeContexts[index] : null;

        public void RemoveStatic(StaticReference reference)
        {
            Simulation.Statics.Remove(reference.Handle);
            _staticContexts.Remove(reference.Handle);
        }

        public void RemoveDynamic(BodyReference body)
        {
            Simulation.Bodies.Remove(body.Handle);
            _dynamicContexts.Remove(body.Handle);
        }

        public void RemoveShape(TypedIndex shapeIndex)
        {
            Simulation.Shapes.Remove(shapeIndex);
            _shapeContexts.Remove(shapeIndex);
        }

        public BodyReference GetBodyReference(int handle)
        {
            return new BodyReference(handle, Simulation.Bodies);
        }

        public StaticReference GetStaticReference(int handle)
        {
            return new StaticReference(handle, Simulation.Statics);
        }

        public T GetShape<T>(int typeIndex) where T : unmanaged, IShape
        {
            return Simulation.Shapes.GetShape<T>(typeIndex);
        }

        public void Raycast<THitHandler>(in Vector3 origin, in Vector3 direction, float maximumT, ref THitHandler hitHandler, int id = 0) where THitHandler : IRayHitHandler
        {
            Simulation.RayCast(origin, direction, maximumT, ref hitHandler, id);
        }

        public void Update(float time)
        {
            Simulation.Timestep(time);
        }

        public void Dispose()
        {
            _characters.Dispose();
            //If you intend to reuse the BufferPool, disposing the simulation is a good idea- it returns all the buffers to the pool for reuse.
            //Here, we dispose it, but it's not really required; we immediately thereafter clear the BufferPool of all held memory.
            //Note that failing to dispose buffer pools can result in memory leaks.
            Simulation.Dispose();
            _threadDispatcher.Dispose();
            Pool.Clear();
        }
    }
}
