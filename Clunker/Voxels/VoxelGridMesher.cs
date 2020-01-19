﻿using Clunker.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.Utilities;
using DefaultEcs.System;
using DefaultEcs;
using DefaultEcs.Threading;

namespace Clunker.Voxels
{
    /// <summary>
    /// Creates a GeometryMesh of exposed sides
    /// </summary>
    public class VoxelGridMesher : AEntitySystem<double>
    {
        private Scene _scene;
        private VoxelTypes _types;

        public VoxelGridMesher(Scene scene, VoxelTypes types, IParallelRunner runner) : base(scene.World.GetEntities().With<MaterialInstance>().WhenAdded<VoxelGridData>().WhenChanged<VoxelGridData>().AsSet(), runner)
        {
            _scene = scene;
            _types = types;
        }

        protected override void Update(double state, in Entity entity)
        {
            ref var data = ref entity.Get<VoxelGridData>();
            ref var materialInstance = ref entity.Get<MaterialInstance>();

            var vertices = new List<VertexPositionTextureNormal>(data.GridSize * data.GridSize * data.GridSize);
            var indices = new List<ushort>(data.GridSize * data.GridSize * data.GridSize);
            var imageSize = new Vector2(materialInstance.ImageWidth, materialInstance.ImageHeight);

            GreedyMeshGenerator.GenerateMesh(data, (typeNum, orientation, side, quad, size) =>
            {
                var type = _types[typeNum];
                var textureOffset = GetTexCoords(type, orientation, side);
                indices.Add((ushort)(vertices.Count + 0));
                indices.Add((ushort)(vertices.Count + 1));
                indices.Add((ushort)(vertices.Count + 3));
                indices.Add((ushort)(vertices.Count + 1));
                indices.Add((ushort)(vertices.Count + 2));
                indices.Add((ushort)(vertices.Count + 3));
                vertices.Add(new VertexPositionTextureNormal(quad.A, (textureOffset + new Vector2(0, 128)) / imageSize, quad.Normal));
                vertices.Add(new VertexPositionTextureNormal(quad.B, (textureOffset + new Vector2(0, 0)) / imageSize, quad.Normal));
                vertices.Add(new VertexPositionTextureNormal(quad.C, (textureOffset + new Vector2(128, 0)) / imageSize, quad.Normal));
                vertices.Add(new VertexPositionTextureNormal(quad.D, (textureOffset + new Vector2(128, 128)) / imageSize, quad.Normal));
            });

            var mesh = new MeshGeometry()
            {
                Vertices = vertices.ToArray(),
                Indices = indices.ToArray()
            };
            var entityRecord = _scene.CommandRecorder.Record(entity);
            entityRecord.Set(mesh);
        }

        private Vector2 GetTexCoords(VoxelType type, VoxelSide orientation, VoxelSide side)
        {
            switch(orientation)
            {
                case VoxelSide.TOP:
                    return
                        side == VoxelSide.TOP ? type.TopTexCoords :
                        side == VoxelSide.BOTTOM ? type.BottomTexCoords :
                        side == VoxelSide.NORTH ? type.NorthTexCoords :
                        side == VoxelSide.SOUTH ? type.SouthTexCoords :
                        side == VoxelSide.EAST ? type.EastTexCoords :
                        side == VoxelSide.WEST ? type.WestTexCoords : type.TopTexCoords;
                case VoxelSide.BOTTOM:
                    return
                        side == VoxelSide.TOP ? type.BottomTexCoords :
                        side == VoxelSide.BOTTOM ? type.TopTexCoords :
                        side == VoxelSide.NORTH ? type.NorthTexCoords :
                        side == VoxelSide.SOUTH ? type.SouthTexCoords :
                        side == VoxelSide.EAST ? type.EastTexCoords :
                        side == VoxelSide.WEST ? type.WestTexCoords : type.TopTexCoords;
                case VoxelSide.NORTH:
                    return
                        side == VoxelSide.TOP ? type.SouthTexCoords :
                        side == VoxelSide.BOTTOM ? type.NorthTexCoords :
                        side == VoxelSide.NORTH ? type.TopTexCoords :
                        side == VoxelSide.SOUTH ? type.BottomTexCoords :
                        side == VoxelSide.EAST ? type.EastTexCoords :
                        side == VoxelSide.WEST ? type.WestTexCoords : type.TopTexCoords;
                case VoxelSide.SOUTH:
                    return
                        side == VoxelSide.TOP ? type.NorthTexCoords :
                        side == VoxelSide.BOTTOM ? type.SouthTexCoords :
                        side == VoxelSide.NORTH ? type.BottomTexCoords :
                        side == VoxelSide.SOUTH ? type.TopTexCoords :
                        side == VoxelSide.EAST ? type.EastTexCoords :
                        side == VoxelSide.WEST ? type.WestTexCoords : type.TopTexCoords;
                case VoxelSide.EAST:
                    return
                        side == VoxelSide.TOP ? type.WestTexCoords :
                        side == VoxelSide.BOTTOM ? type.EastTexCoords :
                        side == VoxelSide.NORTH ? type.NorthTexCoords :
                        side == VoxelSide.SOUTH ? type.SouthTexCoords :
                        side == VoxelSide.EAST ? type.TopTexCoords :
                        side == VoxelSide.WEST ? type.BottomTexCoords : type.TopTexCoords;
                case VoxelSide.WEST:
                    return
                        side == VoxelSide.TOP ? type.EastTexCoords :
                        side == VoxelSide.BOTTOM ? type.WestTexCoords :
                        side == VoxelSide.NORTH ? type.NorthTexCoords :
                        side == VoxelSide.SOUTH ? type.SouthTexCoords :
                        side == VoxelSide.EAST ? type.BottomTexCoords :
                        side == VoxelSide.WEST ? type.TopTexCoords : type.TopTexCoords;
                default:
                    return type.TopTexCoords;
            }
        }
    }
}