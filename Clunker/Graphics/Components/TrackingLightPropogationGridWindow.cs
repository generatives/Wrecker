using Clunker.Geometry;
using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Graphics.Components
{
    public struct TrackingLightPropogationGridWindow
    {
        /// <summary>
        /// Each dimension of the window will be WindowDistance * 2 + 1
        /// </summary>
        public Vector3i WindowDistance { get; set; }
        public Entity LightPropogationGridEntity { get; set; }
    }
}
