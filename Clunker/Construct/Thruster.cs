using Clunker.Input;
using Clunker.Physics.Voxels;
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
                var voxelSpaceObject = GameObject.Parent.Parent;
                var body = voxelSpaceObject.GetComponent<DynamicVoxelSpaceBody>();
                var worldOffset = GameObject.Transform.WorldPosition - body.RelativeBodyOffset;
                var direction = -Voxel.Orientation.GetDirection();
                body.VoxelBody.ApplyImpulse(Vector3.Transform(direction, voxelSpaceObject.Transform.WorldOrientation) * Force, worldOffset);
            }
        }
    }
}
