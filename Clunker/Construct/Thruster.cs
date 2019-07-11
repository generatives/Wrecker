using Clunker.Input;
using Clunker.SceneGraph.ComponentsInterfaces;
using Clunker.Voxels;
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
            if(InputTracker.IsKeyPressed(Veldrid.Key.T))
            {
                var body = GameObject.Parent.GetComponent<DynamicVoxelBody>();
                var localOffset = GameObject.Transform.Position - body.BodyOffset;
                var worldOffset = Vector3.Transform(localOffset, GameObject.Parent.Transform.WorldOrientation);
                body.VoxelBody.ApplyImpulse(-Voxel.Orientation.GetDirection() * Force, worldOffset);
            }
        }
    }
}
