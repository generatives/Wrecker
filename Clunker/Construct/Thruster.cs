using Clunker.Input;
using Clunker.Physics.Voxels;
using Clunker.SceneGraph.ComponentInterfaces;
using Clunker.Voxels;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Construct
{
    public class Thruster : VoxelEntity, IUpdateable
    {
        public float Force { get; set; }

        public void Update(float time)
        {
            if(GameInputTracker.IsKeyPressed(Veldrid.Key.T))
            {
                var voxelSpaceObject = GameObject.Parent.Parent;
                var body = voxelSpaceObject.GetComponent<DynamicVoxelSpaceBody>();
                var worldOffset = GameObject.Transform.WorldPosition - voxelSpaceObject.Transform.WorldPosition - body.RelativeBodyOffset;
                var direction = -Voxel.Orientation.GetDirection();
                body.VoxelBody.ApplyImpulse(Vector3.Transform(direction, voxelSpaceObject.Transform.WorldOrientation) * Force, worldOffset);
            }
        }
    }
}
