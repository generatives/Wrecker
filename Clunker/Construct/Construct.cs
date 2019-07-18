using Clunker.Input;
using Clunker.Physics.Voxels;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentsInterfaces;
using Clunker.Voxels;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;

namespace Clunker.Construct
{
    public class Construct : Component, IUpdateable
    {
        public float MoveForce { get; set; } = 2f;

        public void Update(float time)
        {
            MoveForce += InputTracker.WheelDelta;
            var body = GameObject.GetComponent<DynamicVoxelSpaceBody>();

            if (body.VoxelBody.Exists)
            {
                var force = Vector3.Zero;

                if (InputTracker.IsKeyPressed(Key.Right))
                {
                    force += new Vector3(MoveForce, 0, 0);
                }

                if (InputTracker.IsKeyPressed(Key.Up))
                {
                    force += new Vector3(0, 0, -MoveForce);
                }

                if (InputTracker.IsKeyPressed(Key.Left))
                {
                    force += new Vector3(-MoveForce, 0, 0);
                }

                if (InputTracker.IsKeyPressed(Key.Down))
                {
                    force += new Vector3(0, 0, MoveForce);
                }

                if (InputTracker.IsKeyPressed(Key.P))
                {
                    force += new Vector3(0, MoveForce, 0);
                }

                if (InputTracker.IsKeyPressed(Key.L))
                {
                    force += new Vector3(0, -MoveForce, 0);
                }

                force = Vector3.Transform(force, GameObject.Transform.WorldOrientation);
                body.VoxelBody.ApplyLinearImpulse(force);
            }
        }
    }
}
