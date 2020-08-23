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
using DefaultEcs.Command;

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
        private EntityCommandRecorder _commandRecorder;
        private VoxelTypes _types;
        private GraphicsDevice _device;

        private ThreadLocal<PooledList<VertexPositionTextureNormal>> _vertices;
        private ThreadLocal<PooledList<ushort>> _indices;
        private ThreadLocal<PooledList<ushort>> _transIndices;
        private ThreadLocal<PooledList<float>> _lights;

        public VoxelGridMesher(EntityCommandRecorder commandRecorder, World world, VoxelTypes types, GraphicsDevice device, IParallelRunner runner) : base(world, runner)
        {
            _commandRecorder = commandRecorder;
            _types = types;
            _device = device;

            _vertices = new ThreadLocal<PooledList<VertexPositionTextureNormal>>(() => new PooledList<VertexPositionTextureNormal>());
            _indices = new ThreadLocal<PooledList<ushort>>(() => new PooledList<ushort>());
            _transIndices = new ThreadLocal<PooledList<ushort>>(() => new PooledList<ushort>());
            _lights = new ThreadLocal<PooledList<float>>(() => new PooledList<float>());
        }

        protected override void Update(double state, in Entity entity)
        {
            var watch = Stopwatch.StartNew();
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

            var imageSize = new Vector2(materialTexture.ImageWidth, materialTexture.ImageHeight);

            watch.Stop();
            Utilties.Logging.Metrics.LogMetric($"LogicSystems:VoxelGridMesher:Prep", watch.Elapsed.TotalMilliseconds, TimeSpan.FromSeconds(5));
            watch.Restart();

            var textureOffset = _types["sand"].TopTexCoords;

            MarchingCubesGenerator.GenerateMesh(data, (Triangle tri) =>
            {
                _indices.Value.Add((ushort)(_vertices.Value.Count + 0));
                _indices.Value.Add((ushort)(_vertices.Value.Count + 1));
                _indices.Value.Add((ushort)(_vertices.Value.Count + 2));
                _vertices.Value.Add(new VertexPositionTextureNormal(tri.A, (textureOffset + new Vector2(0f, 126f)) / imageSize, tri.Normal));
                _vertices.Value.Add(new VertexPositionTextureNormal(tri.B, (textureOffset + new Vector2(0f, 0f)) / imageSize, tri.Normal));
                _vertices.Value.Add(new VertexPositionTextureNormal(tri.C, (textureOffset + new Vector2(126f, 0f)) / imageSize, tri.Normal));
                _lights.Value.Add(1.0f);
                _lights.Value.Add(1.0f);
                _lights.Value.Add(1.0f);
            });

            //var builder = new MeshBuilder()
            //{
            //    VoxelTypes = _types,
            //    Voxels = data,
            //    Vertices = _vertices.Value,
            //    Indices = _indices.Value,
            //    TransparentIndices = _transIndices.Value,
            //    Lights = _lights.Value,
            //    ImageSize = imageSize
            //};

            //MeshGenerator<MeshBuilder>.FindExposedSides(ref data, _types, builder);

            watch.Stop();
            Utilties.Logging.Metrics.LogMetric($"LogicSystems:VoxelGridMesher:Algo", watch.Elapsed.TotalMilliseconds, TimeSpan.FromSeconds(5));
            watch.Restart();

            vertexBuffer.Update(_vertices.Value.Span);
            indexBuffer.Update(_indices.Value.Span);
            transparentIndexBuffer.Update(_transIndices.Value.Span);

            var centerOffset = Vector3.One * (data.GridSize * data.VoxelSize / 2f);

            var mesh = new RenderableMeshGeometry()
            {
                Vertices = vertexBuffer,
                Indices = indexBuffer,
                TransparentIndices = transparentIndexBuffer,
                BoundingRadius = centerOffset.Length(),
                BoundingRadiusOffset = centerOffset
            };
            var entityRecord = _commandRecorder.Record(entity);
            entityRecord.Set(mesh);

            lightBuffer.Update(_lights.Value.Span);
            lightVertexResources.LightLevels = lightBuffer;
            entityRecord.Set(lightVertexResources);

            _vertices.Value.Clear();
            _indices.Value.Clear();
            _transIndices.Value.Clear();
            _lights.Value.Clear();


            watch.Stop();
            Utilties.Logging.Metrics.LogMetric($"LogicSystems:VoxelGridMesher:Finish", watch.Elapsed.TotalMilliseconds, TimeSpan.FromSeconds(5));
            watch.Restart();
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
            var textureOffset = type.TextureCoords[(int)side];

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
            vertices.Add(new VertexPositionTextureNormal(quad.A, (textureOffset + new Vector2(0f, 126f)) / imageSize, quad.Normal));
            vertices.Add(new VertexPositionTextureNormal(quad.B, (textureOffset + new Vector2(0f, 0f)) / imageSize, quad.Normal));
            vertices.Add(new VertexPositionTextureNormal(quad.C, (textureOffset + new Vector2(126f, 0f)) / imageSize, quad.Normal));
            vertices.Add(new VertexPositionTextureNormal(quad.D, (textureOffset + new Vector2(126f, 126f)) / imageSize, quad.Normal));

            var occlusionSide = AmbientOcclusionTable[(int)side];
            for(int i = 0; i < occlusionSide.Length; i++)
            {
                var cornerOffsets = occlusionSide[i];
                var side0 = GetLightValue(voxels, voxelIndex + cornerOffsets[0]);
                var side1 = GetLightValue(voxels, voxelIndex + cornerOffsets[1]);
                var side2 = GetLightValue(voxels, voxelIndex + cornerOffsets[2]);
                var side3 = GetLightValue(voxels, voxelIndex + cornerOffsets[3]);
                var averageLight = (side0 + side1 + side2 + side3) / 4f;
                lights.Add(averageLight / 15f);
            }
        }

        private float GetLightValue(VoxelGrid voxels, Vector3i lightIndex)
        {
            if (voxels.ContainsIndex(lightIndex))
            {
                var voxel = voxels[lightIndex];
                return (voxel.Exists && !VoxelTypes[voxel.BlockType].Transparent) ? 0f : 15f;
            }
            else
            {
                var spaceIndex = voxels.VoxelSpace.GetSpaceIndexFromVoxelIndex(voxels.MemberIndex, lightIndex);

                var voxel = voxels.VoxelSpace.GetVoxel(spaceIndex);
                return (voxel.HasValue && voxel.Value.Exists && !VoxelTypes[voxel.Value.BlockType].Transparent) ? 0f : 15f;
            };
        }

        private Vector2 GetOrientedTexCoords(VoxelType type, VoxelSide orientation, VoxelSide side)
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
