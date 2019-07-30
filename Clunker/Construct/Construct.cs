﻿using Clunker.Graphics;
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
        public float MoveForce { get; set; } = 2f;

        public void Update(float time)
        {
            MoveForce += GameInputTracker.WheelDelta;
            var body = GameObject.GetComponent<DynamicVoxelSpaceBody>();

            if (body.VoxelBody.Exists)
            {
                var force = Vector3.Zero;

                if (GameInputTracker.IsKeyPressed(Key.Right))
                {
                    force += new Vector3(MoveForce, 0, 0);
                }

                if (GameInputTracker.IsKeyPressed(Key.Up))
                {
                    force += new Vector3(0, 0, -MoveForce);
                }

                if (GameInputTracker.IsKeyPressed(Key.Left))
                {
                    force += new Vector3(-MoveForce, 0, 0);
                }

                if (GameInputTracker.IsKeyPressed(Key.Down))
                {
                    force += new Vector3(0, 0, MoveForce);
                }

                if (GameInputTracker.IsKeyPressed(Key.P))
                {
                    force += new Vector3(0, MoveForce, 0);
                }

                if (GameInputTracker.IsKeyPressed(Key.L))
                {
                    force += new Vector3(0, -MoveForce, 0);
                }

                force = Vector3.Transform(force, GameObject.Transform.WorldOrientation);
                body.VoxelBody.ApplyLinearImpulse(force);
            }


            if (GameInputTracker.WasKeyDowned(Key.M))
            {
                using(var memoryStream = new MemoryStream())
                {
                    CurrentScene.App.Serializer.Serialize(GameObject, memoryStream);
                    CurrentScene.App.ShipBin = memoryStream.ToArray();
                }
                CurrentScene.RemoveGameObject(GameObject);
            }
        }
    }
}
