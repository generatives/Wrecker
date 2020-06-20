using Clunker.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.Utilities;
using DefaultEcs.System;
using DefaultEcs;
using DefaultEcs.Threading;
using Clunker.Voxels.Meshing;
using Collections.Pooled;
using System.Runtime.CompilerServices;

namespace Clunker.Voxels.Meshing
{
    /// <summary>
    /// Creates a GeometryMesh of exposed sides
    /// </summary>
    public class VoxelGridMesher : AEntitySystem<double>
    {
        private Scene _scene;
        private VoxelTypes _types;
        private GraphicsDevice _device;

        public VoxelGridMesher(Scene scene, VoxelTypes types, GraphicsDevice device, IParallelRunner runner) : base(scene.World.GetEntities().With<MaterialTexture>().WhenAdded<VoxelGrid>().WhenChanged<VoxelGrid>().AsSet(), runner)
        {
            _scene = scene;
            _types = types;
            _device = device;
        }

        protected override void Update(double state, in Entity entity)
        {
            var data = entity.Get<VoxelGrid>();
            ref var materialTexture = ref entity.Get<MaterialTexture>();

            ResizableBuffer<VertexPositionTextureNormal> vertexBuffer;
            ResizableBuffer<ushort> indexBuffer;
            ResizableBuffer<ushort> transparentIndexBuffer;

            if(entity.Has<RenderableMeshGeometry>())
            {
                ref var geometry = ref entity.Get<RenderableMeshGeometry>();
                vertexBuffer = geometry.Vertices;
                indexBuffer = geometry.Indices;
                transparentIndexBuffer = geometry.TransparentIndices;
            }
            else
            {
                vertexBuffer = new ResizableBuffer<VertexPositionTextureNormal>(_device, VertexPositionTextureNormal.SizeInBytes, BufferUsage.VertexBuffer);
                indexBuffer = new ResizableBuffer<ushort>(_device, sizeof(ushort), BufferUsage.IndexBuffer);
                transparentIndexBuffer = new ResizableBuffer<ushort>(_device, sizeof(ushort), BufferUsage.IndexBuffer);
            }

            using var vertices = new PooledList<VertexPositionTextureNormal>(data.GridSize * data.GridSize * data.GridSize);
            using var indices = new PooledList<ushort>(data.GridSize * data.GridSize * data.GridSize);
            using var transIndices = new PooledList<ushort>(data.GridSize * data.GridSize * data.GridSize);
            var imageSize = new Vector2(materialTexture.ImageWidth, materialTexture.ImageHeight);

            MeshGenerator.FindExposedSides(data, _types, (x, y, z, side) =>
            {
                var voxelSize = data.VoxelSize;
                var position = new Vector3(x, y, z);
                var quad = side.GetQuad().Translate(position).Scale(voxelSize);
                var voxel = data[x, y, z];
                var type = _types[voxel.BlockType];
                var textureOffset = GetTexCoords(type, voxel.Orientation, side);
                if (type.Transparent)
                {
                    AddQuad(quad, vertices, transIndices, textureOffset, imageSize);
                }
                else
                {
                    AddQuad(quad, vertices, indices, textureOffset, imageSize);
                }
            });

            vertexBuffer.Update(vertices.ToArray());
            indexBuffer.Update(indices.ToArray());
            transparentIndexBuffer.Update(transIndices.ToArray());

            var mesh = new RenderableMeshGeometry()
            {
                Vertices = vertexBuffer,
                Indices = indexBuffer,
                TransparentIndices = transparentIndexBuffer,
                BoundingSize = new Vector3(data.GridSize * data.VoxelSize)
            };
            var entityRecord = _scene.CommandRecorder.Record(entity);
            entityRecord.Set(mesh);
        }

        private void AddQuad(Geometry.Quad quad, PooledList<VertexPositionTextureNormal> vertices, PooledList<ushort> indices, Vector2 textureOffset, Vector2 imageSize)
        {
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
