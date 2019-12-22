using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.UtilityComponents
{
    public class ObjectFollower : Component, IUpdateable
    {
        public GameObject ToFollow { get; set; }
        public Vector3 Distance { get; set; }

        public void Update(float time)
        {
            if(ToFollow != null)
            {
                GameObject.Transform.WorldPosition = ToFollow.Transform.GetWorld(Distance);
                //GameObject.Transform.Orientation = ToFollow.Transform.Orientation;
            }
        }
    }
}
