using Clunker.Graphics;
using Clunker.Input;
using Clunker.Physics.Voxels;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
using Clunker.SceneGraph.Core;
using Clunker.Voxels;
using Hyperion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Veldrid;

namespace Clunker.Construct
{
    public class Construct : Component, IUpdateable
    {
        public float MoveForce { get; set; } = 0.5f;

        public void Update(float time)
        {
            MoveForce += GameInputTracker.WheelDelta;
            var body = GameObject.GetComponent<DynamicVoxelSpaceBody>();

            if (body.VoxelBody.Exists)
            {
                var force = Vector3.Zero;

                if (GameInputTracker.IsKeyPressed(Key.Keypad6))
                {
                    force += new Vector3(MoveForce, 0, 0);
                }

                if (GameInputTracker.IsKeyPressed(Key.Keypad8))
                {
                    force += new Vector3(0, 0, -MoveForce);
                }

                if (GameInputTracker.IsKeyPressed(Key.Keypad4))
                {
                    force += new Vector3(-MoveForce, 0, 0);
                }

                if (GameInputTracker.IsKeyPressed(Key.Keypad2))
                {
                    force += new Vector3(0, 0, MoveForce);
                }

                if (GameInputTracker.IsKeyPressed(Key.KeypadPlus))
                {
                    force += new Vector3(0, MoveForce, 0);
                }

                if (GameInputTracker.IsKeyPressed(Key.KeypadMinus))
                {
                    force += new Vector3(0, -MoveForce, 0);
                }

                var delta = GameInputTracker.MouseDelta;
                if(delta != Vector2.Zero && (MathF.Abs(delta.X) == 1 || MathF.Abs(delta.Y) == 1))
                {
                    var turn = delta * 0.01f;
                    body.VoxelBody.ApplyAngularImpulse(new Vector3(turn.Y, turn.X, 0));
                }
                else
                {
                    //body.VoxelBody.ApplyAngularImpulse(-body.VoxelBody.Velocity.Angular * 0.1f);
                }

                force = Vector3.Transform(force, GameObject.Transform.WorldOrientation);
                body.VoxelBody.ApplyLinearImpulse(force);
            }
        }
    }
}
