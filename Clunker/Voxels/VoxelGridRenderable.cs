using Clunker.Graphics;
using Clunker.Graphics.Materials;
using Clunker.Math;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
using Hyperion;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Veldrid;

namespace Clunker.Voxels
{
    public class VoxelGridRenderable : Component, IRenderable
    {
        [Ignore]
        private VoxelGrid grid;

        private static MaterialInstance _materialInstance;

        [Ignore]
        private MeshGeometry _meshGeometry;

        private VoxelTypes _types;

        public RenderingPass Pass => RenderingPass.SCENE;

        public bool Transparent => false;

        public Vector3 Position => GameObject.Transform.WorldPosition;

        public VoxelGridRenderable(VoxelTypes types, MaterialInstance materialInstance)
        {
            _types = types;
            _materialInstance = materialInstance;
        }

        public void Initialize(GraphicsDevice device, CommandList commandList, RenderableInitialize initialize)
        {
            grid = GameObject.GetComponent<VoxelGrid>();
            grid.VoxelsChanged += GenerateMesh;
            GenerateMesh(grid);
        }

        public void Render(GraphicsDevice device, CommandList commandList, RenderingContext context)
        {
            if(_meshGeometry != null)
            {
                var copy = new RenderingContext()
                {
                    Renderer = context.Renderer,
                    RenderWireframes = true
                };
                _materialInstance.Bind(device, commandList, copy);
                commandList.UpdateBuffer(copy.Renderer.WorldBuffer, 0, GameObject.Transform.WorldMatrix);
                _meshGeometry.Render(device, commandList);
            }
        }

        public void Remove(GraphicsDevice device, CommandList commandList)
        {
            _meshGeometry?.Dispose();
            grid.VoxelsChanged -= GenerateMesh;
        }

        private void GenerateMesh(VoxelGrid grid)
        {
            this.EnqueueWorkerJob(() =>
            {
                //Thread.Sleep(750);
                var voxels = this.grid.Data;
                var vertices = new List<VertexPositionTextureNormal>(voxels.XLength * voxels.YLength * voxels.ZLength);
                var indices = new List<ushort>(voxels.XLength * voxels.YLength * voxels.ZLength);
                
                MeshGenerator.GenerateGridMesh(voxels, (voxel, side, quad) =>
                {
                    var textureOffset = GetTexCoords(voxel, side);
                    indices.Add((ushort)(vertices.Count + 0));
                    indices.Add((ushort)(vertices.Count + 1));
                    indices.Add((ushort)(vertices.Count + 3));
                    indices.Add((ushort)(vertices.Count + 1));
                    indices.Add((ushort)(vertices.Count + 2));
                    indices.Add((ushort)(vertices.Count + 3));
                    vertices.Add(new VertexPositionTextureNormal(quad.A, (textureOffset + new Vector2(0, 128)) / new Vector2(1024, 2048), quad.Normal));
                    vertices.Add(new VertexPositionTextureNormal(quad.B, (textureOffset + new Vector2(0, 0)) / new Vector2(1024, 2048), quad.Normal));
                    vertices.Add(new VertexPositionTextureNormal(quad.C, (textureOffset + new Vector2(128, 0)) / new Vector2(1024, 2048), quad.Normal));
                    vertices.Add(new VertexPositionTextureNormal(quad.D, (textureOffset + new Vector2(128, 128)) / new Vector2(1024, 2048), quad.Normal));
                });
                if (_meshGeometry == null) _meshGeometry = new MeshGeometry();
                _meshGeometry.UpdateMesh(vertices.ToArray(), indices.ToArray());
            });
        }

        private Vector2 GetTexCoords(Voxel voxel, VoxelSide side)
        {
            var type = _types[voxel.BlockType];
            switch(voxel.Orientation)
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

        //public void GenerateMesh(out List<VertexPositionTextureNormal> vertices, out List<ushort> indices)
        //{
        //    vertices = new List<VertexPositionTextureNormal>(_voxels.XLength * _voxels.YLength * _voxels.ZLength);
        //    indices = new List<ushort>(_voxels.XLength * _voxels.YLength * _voxels.ZLength);
        //    for (int x = 0; x < _voxels.XLength; x++)
        //        for (int y = 0; y < _voxels.YLength; y++)
        //            for (int z = 0; z < _voxels.ZLength; z++)
        //            {
        //                Voxel voxel = _voxels[x, y, z];
        //                if (voxel.Exists)
        //                {
        //                    if (!_voxels.WithinBounds(x, y - 1, z) || !_voxels[x, y - 1, z].Exists)
        //                    {
        //                        var normal = -Vector3.UnitY;
        //                        AddTriangle(new Triangle(new Vector3(x, y, z) * _voxels.VoxelSize, new Vector3(x, y, z + 1) * _voxels.VoxelSize, new Vector3(x + 1, y, z) * _voxels.VoxelSize, normal), vertices, indices);
        //                        AddTriangle(new Triangle(new Vector3(x, y, z + 1) * _voxels.VoxelSize, new Vector3(x + 1, y, z + 1) * _voxels.VoxelSize, new Vector3(x + 1, y, z) * _voxels.VoxelSize, normal), vertices, indices);
        //                    }

        //                    if (!_voxels.WithinBounds(x + 1, y, z) || !_voxels[x + 1, y, z].Exists)
        //                    {
        //                        var normal = Vector3.UnitX;
        //                        AddTriangle(new Triangle(new Vector3(x + 1, y, z) * _voxels.VoxelSize, new Vector3(x + 1, y, z + 1) * _voxels.VoxelSize, new Vector3(x + 1, y + 1, z) * _voxels.VoxelSize, normal), vertices, indices);
        //                        AddTriangle(new Triangle(new Vector3(x + 1, y + 1, z + 1) * _voxels.VoxelSize, new Vector3(x + 1, y + 1, z) * _voxels.VoxelSize, new Vector3(x + 1, y, z + 1) * _voxels.VoxelSize, normal), vertices, indices);
        //                    }

        //                    if (!_voxels.WithinBounds(x - 1, y, z) || !_voxels[x - 1, y, z].Exists)
        //                    {
        //                        var normal = -Vector3.UnitX;
        //                        AddTriangle(new Triangle(new Vector3(x, y, z) * _voxels.VoxelSize, new Vector3(x, y + 1, z) * _voxels.VoxelSize, new Vector3(x, y, z + 1) * _voxels.VoxelSize, normal), vertices, indices);
        //                        AddTriangle(new Triangle(new Vector3(x, y + 1, z + 1) * _voxels.VoxelSize, new Vector3(x, y, z + 1) * _voxels.VoxelSize, new Vector3(x, y + 1, z) * _voxels.VoxelSize, normal), vertices, indices);
        //                    }

        //                    if (!_voxels.WithinBounds(x, y + 1, z) || !_voxels[x, y + 1, z].Exists)
        //                    {
        //                        var normal = Vector3.UnitY;
        //                        AddTriangle(new Triangle(new Vector3(x, y + 1, z) * _voxels.VoxelSize, new Vector3(x + 1, y + 1, z) * _voxels.VoxelSize, new Vector3(x, y + 1, z + 1) * _voxels.VoxelSize, normal), vertices, indices);
        //                        AddTriangle(new Triangle(new Vector3(x + 1, y + 1, z + 1) * _voxels.VoxelSize, new Vector3(x, y + 1, z + 1) * _voxels.VoxelSize, new Vector3(x + 1, y + 1, z) * _voxels.VoxelSize, normal), vertices, indices);
        //                    }

        //                    if (!_voxels.WithinBounds(x, y, z - 1) || !_voxels[x, y, z - 1].Exists)
        //                    {
        //                        var normal = -Vector3.UnitZ;
        //                        AddTriangle(new Triangle(new Vector3(x, y, z) * _voxels.VoxelSize, new Vector3(x + 1, y, z) * _voxels.VoxelSize, new Vector3(x, y + 1, z) * _voxels.VoxelSize, normal), vertices, indices);
        //                        AddTriangle(new Triangle(new Vector3(x + 1, y + 1, z) * _voxels.VoxelSize, new Vector3(x, y + 1, z) * _voxels.VoxelSize, new Vector3(x + 1, y, z) * _voxels.VoxelSize, normal), vertices, indices);
        //                    }

        //                    if (!_voxels.WithinBounds(x, y, z + 1) || !_voxels[x, y, z + 1].Exists)
        //                    {
        //                        var normal = Vector3.UnitZ;
        //                        AddTriangle(new Triangle(new Vector3(x, y, z + 1) * _voxels.VoxelSize, new Vector3(x, y + 1, z + 1) * _voxels.VoxelSize, new Vector3(x + 1, y, z + 1) * _voxels.VoxelSize, normal), vertices, indices);
        //                        AddTriangle(new Triangle(new Vector3(x + 1, y + 1, z + 1) * _voxels.VoxelSize, new Vector3(x + 1, y, z + 1) * _voxels.VoxelSize, new Vector3(x, y + 1, z + 1) * _voxels.VoxelSize, normal), vertices, indices);
        //                    }
        //                }
        //            }
        //}

        //private void AddTriangle(Triangle triangle, List<VertexPositionTextureNormal> vertices, List<ushort> indices)
        //{
        //    indices.Add((ushort)vertices.Count);
        //    vertices.Add(new VertexPositionTextureNormal(triangle.A, new System.Numerics.Vector2(0, 0), triangle.Normal));
        //    indices.Add((ushort)vertices.Count);
        //    vertices.Add(new VertexPositionTextureNormal(triangle.B, new System.Numerics.Vector2(0, 0), triangle.Normal));
        //    indices.Add((ushort)vertices.Count);
        //    vertices.Add(new VertexPositionTextureNormal(triangle.C, new System.Numerics.Vector2(0, 0), triangle.Normal));
        //}
    }
}
