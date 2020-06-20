using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Clunker.Core;
using Clunker.Geometry;
using Clunker.Physics.Bepu;
using Clunker.Physics.Character;
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

        private Dictionary<StaticHandle, object> _staticContexts;
        private Dictionary<BodyHandle, object> _dynamicContexts;
        private Dictionary<TypedIndex, object> _shapeContexts;

        public CharacterControllers Characters { get; private set; }

        public PhysicsSystem()
        {
            //The buffer pool is a source of raw memory blobs for the engine to use.
            Pool = new BufferPool();

            Characters = new CharacterControllers(Pool);

            //Note that you can also control the order of internal stage execution using a different ITimestepper implementation.
            //For the purposes of this demo, we just use the default by passing in nothing (which happens to be PositionFirstTimestepper at the time of writing).
            Simulation = Simulation.Create(Pool, new CharacterNarrowphaseCallbacks(Characters), new PoseIntegratorCallbacks(this, new Vector3(0, -10, 0)));
            
            _threadDispatcher = new SimpleThreadDispatcher(Environment.ProcessorCount);

            _staticContexts = new Dictionary<StaticHandle, object>();
            _dynamicContexts = new Dictionary<BodyHandle, object>();
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

        public object GetCollidableContext(CollidableReference collidable)
        {
            switch(collidable.Mobility)
            {
                case CollidableMobility.Dynamic:
                    return GetDynamicContext(collidable.BodyHandle);
                case CollidableMobility.Static:
                    return GetStaticContext(collidable.StaticHandle);
                case CollidableMobility.Kinematic:
                default:
                    return null;
            }
        }

        public object GetStaticContext(StaticHandle handle) => _staticContexts.ContainsKey(handle) ? _staticContexts[handle] : null;
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

        public object GetDynamicContext(BodyHandle handle) => _dynamicContexts.ContainsKey(handle) ? _dynamicContexts[handle] : null;
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

        public BodyReference GetBodyReference(BodyHandle handle)
        {
            return new BodyReference(handle, Simulation.Bodies);
        }

        public StaticReference GetStaticReference(StaticHandle handle)
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

        public RaycastResult Raycast(Transform transform, BodyHandle? bodyHandleFilter = null)
        {
            var handler = new MobilityBodyHitHandler(CollidableMobility.Static | CollidableMobility.Dynamic, bodyHandleFilter);
            var forward = transform.Orientation.GetForwardVector();
            Raycast(transform.WorldPosition, forward, float.MaxValue, ref handler);
            if (handler.Hit)
            {
                object context;
                if (handler.Collidable.Mobility == CollidableMobility.Dynamic)
                {
                    context = GetDynamicContext(handler.Collidable.BodyHandle);
                }
                else
                {
                    context = GetStaticContext(handler.Collidable.StaticHandle);
                }

                if (context is Entity entity)
                {
                    return new RaycastResult()
                    {
                        Hit = true,
                        Collidable = handler.Collidable,
                        Entity = entity,
                        T = handler.T,
                        ChildIndex = handler.ChildIndex
                    };
                }
            }

            return default;
        }

        public CharacterInput BuildCharacterInput(Vector3 initialPosition, Capsule shape,
            float speculativeMargin, float mass, float maximumHorizontalForce, float maximumVerticalGlueForce,
            float jumpVelocity, float speed, float maximumSlope = (float)Math.PI * 0.25f)
        {
            var shapeIndex = Simulation.Shapes.Add(shape);

            //Because characters are dynamic, they require a defined BodyInertia. For the purposes of the demos, we don't want them to rotate or fall over, so the inverse inertia tensor is left at its default value of all zeroes.
            //This is effectively equivalent to giving it an infinite inertia tensor- in other words, no torque will cause it to rotate.
            var bodyHandle = Simulation.Bodies.Add(BodyDescription.CreateDynamic(initialPosition, new BodyInertia { InverseMass = 1f / mass }, new CollidableDescription(shapeIndex, speculativeMargin), new BodyActivityDescription(shape.Radius * 0.02f)));
            ref var character = ref Characters.AllocateCharacter(bodyHandle);
            character.LocalUp = new Vector3(0, 1, 0);
            character.CosMaximumSlope = (float)Math.Cos(maximumSlope);
            character.JumpVelocity = jumpVelocity;
            character.MaximumVerticalForce = maximumVerticalGlueForce;
            character.MaximumHorizontalForce = maximumHorizontalForce;
            character.MinimumSupportDepth = shape.Radius * -0.01f;
            character.MinimumSupportContinuationDepth = -speculativeMargin;

            return new CharacterInput()
            {
                BodyHandle = bodyHandle,
                Shape = shape,
                Speed = speed,
            };
        }

        public void DisposeCharacterInput(CharacterInput characterInput)
        {
            RemoveShape(new BodyReference(characterInput.BodyHandle, Simulation.Bodies).Collidable.Shape);
            RemoveDynamic(new BodyReference(characterInput.BodyHandle, Simulation.Bodies));
            Characters.RemoveCharacterByBodyHandle(characterInput.BodyHandle);
        }

        public void Update(double time)
        {
            Simulation.Timestep((float)time, _threadDispatcher);
        }

        public void Dispose()
        {
            Characters.Dispose();
            //If you intend to reuse the BufferPool, disposing the simulation is a good idea- it returns all the buffers to the pool for reuse.
            //Here, we dispose it, but it's not really required; we immediately thereafter clear the BufferPool of all held memory.
            //Note that failing to dispose buffer pools can result in memory leaks.
            Simulation.Dispose();
            _threadDispatcher.Dispose();
            Pool.Clear();
        }
    }

    public struct RaycastResult
    {
        public bool Hit;
        public CollidableReference Collidable;
        public Entity Entity;
        public float T;
        public int ChildIndex;
    }
}
