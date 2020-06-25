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
using Clunker.Geometry;
using System.Collections.Concurrent;
using System.Linq;
using Clunker.Voxels.Space;

namespace Clunker.Voxels.Meshing
{

    /// <summary>
    /// Creates a GeometryMesh of exposed sides
    /// </summary>
    [With(typeof(MaterialTexture))]
    [WhenAddedEither(typeof(VoxelGrid))]
    [WhenChangedEither(typeof(VoxelGrid))]
    public class VoxelGridMesher : AEntitySystem<double>
    {
        private Scene _scene;
        private VoxelTypes _types;
        private GraphicsDevice _device;

        public VoxelGridMesher(Scene scene, VoxelTypes types, GraphicsDevice device, IParallelRunner runner) : base(scene.World, runner)
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

            ref var lightVertexResources = ref entity.Get<LightVertexResources>();

            var lightBuffer = lightVertexResources.LightLevels.Exists ?
                lightVertexResources.LightLevels :
                new ResizableBuffer<float>(_device, sizeof(float), BufferUsage.VertexBuffer);

            using var vertices = new PooledList<VertexPositionTextureNormal>(data.GridSize * data.GridSize * data.GridSize);
            using var indices = new PooledList<ushort>(data.GridSize * data.GridSize * data.GridSize);
            using var transIndices = new PooledList<ushort>(data.GridSize * data.GridSize * data.GridSize);
            using var lights = new PooledList<float>(data.GridSize * data.GridSize * data.GridSize);
            var imageSize = new Vector2(materialTexture.ImageWidth, materialTexture.ImageHeight);

            var builder = new MeshBuilder()
            {
                VoxelTypes = _types,
                Voxels = data,
                Vertices = vertices,
                Indices = indices,
                TransparentIndices = transIndices,
                Lights = lights,
                ImageSize = imageSize
            };

            MeshGenerator<MeshBuilder>.FindExposedSides(ref data, _types, builder);

            vertexBuffer.Update(vertices.ToArray());
            indexBuffer.Update(indices.ToArray());
            transparentIndexBuffer.Update(transIndices.ToArray());

            var centerOffset = Vector3.One * (data.GridSize * data.VoxelSize / 2f);

            var mesh = new RenderableMeshGeometry()
            {
                Vertices = vertexBuffer,
                Indices = indexBuffer,
                TransparentIndices = transparentIndexBuffer,
                BoundingRadius = centerOffset.Length(),
                BoundingRadiusOffset = centerOffset
            };
            var entityRecord = _scene.CommandRecorder.Record(entity);
            entityRecord.Set(mesh);

            lightBuffer.Update(lights.ToArray());
            lightVertexResources.LightLevels = lightBuffer;
            entityRecord.Set(lightVertexResources);
        }
    }

    public struct MeshBuilder : IExposedSideProcessor
    {
        private static readonly Vector3i[][][] AmbientOcclusionTable = AmbientOcclusion.GenerateTable();

        public VoxelGrid Voxels;
        public VoxelTypes VoxelTypes;
        public PooledList<VertexPositionTextureNormal> Vertices;
        public PooledList<float> Lights;
        public PooledList<ushort> Indices;
        public PooledList<ushort> TransparentIndices;
        public Vector2 ImageSize;

        public void Process(int x, int y, int z, VoxelSide side)
        {
            var voxelSize = Voxels.VoxelSize;
            var position = new Vector3(x, y, z);
            var quad = side.GetQuad().Translate(position).Scale(voxelSize);
            var voxel = Voxels[x, y, z];
            var type = VoxelTypes[voxel.BlockType];
            var textureOffset = GetTexCoords(type, voxel.Orientation, side);

            if (type.Transparent)
            {
                AddQuad(quad, new Vector3i(x, y, z), Voxels, side, textureOffset, ImageSize, Vertices, Lights, TransparentIndices);
            }
            else
            {
                AddQuad(quad, new Vector3i(x, y, z), Voxels, side, textureOffset, ImageSize, Vertices, Lights, Indices);
            }
        }

        private void AddQuad(Geometry.Quad quad, Vector3i voxelIndex, VoxelGrid voxels, VoxelSide side, Vector2 textureOffset, Vector2 imageSize,
            PooledList<VertexPositionTextureNormal> vertices, PooledList<float> lights, PooledList<ushort> indices)
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

            var occlusionSide = AmbientOcclusionTable[(int)side];
            foreach(var cornerOffsets in occlusionSide)
            {
                var averageLight = cornerOffsets.Select(offset =>
                {
                    var lightIndex = voxelIndex + offset;
                    if (voxels.ContainsIndex(lightIndex))
                    {
                        return (float)voxels.GetLight(voxelIndex + offset);
                    }
                    else
                    {
                        var spaceIndex = voxels.VoxelSpace.GetSpaceIndexFromVoxelIndex(voxels.MemberIndex, lightIndex);
                        var light = voxels.VoxelSpace.GetLight(spaceIndex);
                        return light.HasValue ? light.Value : 15;
                    };
                }).Average();
                lights.Add(averageLight / 15f);
            }
        }

        private Vector2 GetTexCoords(VoxelType type, VoxelSide orientation, VoxelSide side)
        {
            switch (orientation)
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
