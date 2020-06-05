﻿using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Clunker.Core;
using Clunker.Geometry;
using Clunker.Physics.Bepu;
using DefaultEcs;
using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Physics
{
    public class PhysicsSystem : ISystem<double>
    {
        public Simulation Simulation { get; private set; }
        private SimpleThreadDispatcher _threadDispatcher;
        public BufferPool Pool { get; private set; }
        public bool IsEnabled { get; set; } = true;

        private Dictionary<int, object> _staticContexts;
        private Dictionary<int, object> _dynamicContexts;
        private Dictionary<TypedIndex, object> _shapeContexts;

        public PhysicsSystem()
        {
            //The buffer pool is a source of raw memory blobs for the engine to use.
            Pool = new BufferPool();
            //Note that you can also control the order of internal stage execution using a different ITimestepper implementation.
            //For the purposes of this demo, we just use the default by passing in nothing (which happens to be PositionFirstTimestepper at the time of writing).
            Simulation = Simulation.Create(Pool, new NarrowPhaseCallbacks(), new PoseIntegratorCallbacks(new Vector3(0, 0, 0)));
            
            _threadDispatcher = new SimpleThreadDispatcher(Environment.ProcessorCount);

            _staticContexts = new Dictionary<int, object>();
            _dynamicContexts = new Dictionary<int, object>();
            _shapeContexts = new Dictionary<TypedIndex, object>();
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

        public T GetShape<T>(TypedIndex typedIndex) where T : unmanaged, IShape
        {
            return Simulation.Shapes.GetShape<T>(typedIndex.Index);
        }

        public void Raycast<THitHandler>(in Vector3 origin, in Vector3 direction, float maximumT, ref THitHandler hitHandler, int id = 0) where THitHandler : IRayHitHandler
        {
            Simulation.RayCast(origin, direction, maximumT, ref hitHandler, id);
        }

        public Entity? Raycast(Transform transform)
        {
            var handler = new FirstHitHandler(CollidableMobility.Static | CollidableMobility.Dynamic);
            var forward = transform.Orientation.GetForwardVector();
            Raycast(transform.WorldPosition, forward, float.MaxValue, ref handler);
            if (handler.Hit)
            {
                object context;
                if (handler.Collidable.Mobility == CollidableMobility.Dynamic)
                {
                    context = GetDynamicContext(handler.Collidable.Handle);
                }
                else
                {
                    context = GetStaticContext(handler.Collidable.Handle);
                }

                if (context is Entity entity)
                {
                    return entity;
                }
            }

            return null;
        }

        public void Update(double time)
        {
            Simulation.Timestep((float)time, _threadDispatcher);
        }

        public void Dispose()
        {
            //If you intend to reuse the BufferPool, disposing the simulation is a good idea- it returns all the buffers to the pool for reuse.
            //Here, we dispose it, but it's not really required; we immediately thereafter clear the BufferPool of all held memory.
            //Note that failing to dispose buffer pools can result in memory leaks.
            Simulation.Dispose();
            _threadDispatcher.Dispose();
            Pool.Clear();
        }
    }
}
