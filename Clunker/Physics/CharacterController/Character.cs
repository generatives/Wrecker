using BepuPhysics.Collidables;
using Clunker.Math;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Physics.CharacterController
{

    public class Character : Component, IComponentEventListener, IUpdateable
    {
        public bool HasCharacter { get; private set; }
        private CharacterControllerRef _controller;

        public ForwardMovement ForwardMovement { get; set; }
        public LateralMovement LateralMovement { get; set; }
        public bool TryJump { get; set; }
        public bool Sprint { get; set; }

        public void ToggleCharacter()
        {
            if(HasCharacter)
            {
                _controller.Dispose();
                HasCharacter = false;
            }
            else
            {
                var physics = CurrentScene.GetSystem<PhysicsSystem>();
                _controller = physics.CreateCharacter(GameObject.Transform.WorldPosition, new Capsule(0.5f, 1), 0.1f, 1.25f, 50, 100, 8, 4, MathF.PI * 0.4f);
                HasCharacter = true;
            }
        }

        public void ComponentStarted()
        {
        }

        public void Update(float time)
        {
            if(HasCharacter)
            {
                _controller.UpdateCharacterGoals(ForwardMovement, LateralMovement, Sprint, TryJump, GameObject.Transform.Orientation.GetForwardVector(), time);
                TryJump = false;
                GameObject.Transform.WorldPosition = _controller.Body.Pose.Position + Vector3.UnitY;
            }
        }

        public void ComponentStopped()
        {
            _controller.Dispose();
        }
    }
}
