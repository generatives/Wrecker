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
    [With(typeof(RenderableMeshGeometry))]
    [With(typeof(LightVertexResources))]
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

        private int _numRunning;

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

            var imageSize = new Vector2(materialTexture.ImageWidth, materialTexture.ImageHeight);

            watch.Stop();
            if (watch.Elapsed.TotalMilliseconds > 1) Utilties.Logging.Metrics.LogMetric($"LogicSystems:VoxelGridMesher:Prep", watch.Elapsed.TotalMilliseconds, TimeSpan.FromSeconds(5));
            watch.Restart();

            var builder = new MeshBuilder()
            {
                VoxelTypes = _types,
                Voxels = data,
                Vertices = _vertices.Value,
                Indices = _indices.Value,
                TransparentIndices = _transIndices.Value,
                Lights = _lights.Value,
                ImageSize = imageSize
            };

            MeshGenerator<MeshBuilder>.FindExposedSides(ref data, _types, builder);

            watch.Stop();
            if (watch.Elapsed.TotalMilliseconds > 1) Utilties.Logging.Metrics.LogMetric($"LogicSystems:VoxelGridMesher:Algo", watch.Elapsed.TotalMilliseconds, TimeSpan.FromSeconds(5));
            watch.Restart();

            ref var meshGeometry = ref entity.Get<RenderableMeshGeometry>();
            ref var lightVertexResources = ref entity.Get<LightVertexResources>();

            if(_vertices.Value.Count > 0)
            {
                meshGeometry.Vertices = meshGeometry.Vertices.Exists ?
                    meshGeometry.Vertices :
                    new ResizableBuffer<VertexPositionTextureNormal>(_device, VertexPositionTextureNormal.SizeInBytes, BufferUsage.VertexBuffer);

                meshGeometry.Indices = meshGeometry.Indices.Exists ?
                    meshGeometry.Indices :
                    new ResizableBuffer<ushort>(_device, sizeof(ushort), BufferUsage.IndexBuffer);

                meshGeometry.TransparentIndices = meshGeometry.TransparentIndices.Exists ?
                    meshGeometry.TransparentIndices :
                    new ResizableBuffer<ushort>(_device, sizeof(ushort), BufferUsage.IndexBuffer);

                meshGeometry.Vertices.Update(_vertices.Value.Span);
                meshGeometry.Indices.Update(_indices.Value.Span);
                meshGeometry.TransparentIndices.Update(_transIndices.Value.Span);

                var centerOffset = Vector3.One * (data.GridSize * data.VoxelSize / 2f);

                meshGeometry.BoundingRadius = centerOffset.Length();
                meshGeometry.BoundingRadiusOffset = centerOffset;

                var entityRecord = _commandRecorder.Record(entity);
                entityRecord.Set(meshGeometry);

                var lightBuffer = lightVertexResources.LightLevels.Exists ?
                    lightVertexResources.LightLevels :
                    new ResizableBuffer<float>(_device, sizeof(float), BufferUsage.VertexBuffer);

                lightBuffer.Update(_lights.Value.Span);
                lightVertexResources.LightLevels = lightBuffer;

                entityRecord.Set(lightVertexResources);
            }
            else
            {
                if(meshGeometry.Vertices.Exists)
                {
                    meshGeometry.Vertices.Dispose();
                    meshGeometry.Vertices = default;
                }

                if (meshGeometry.Indices.Exists)
                {
                    meshGeometry.Indices.Dispose();
                    meshGeometry.Indices = default;
                }

                if (meshGeometry.TransparentIndices.Exists)
                {
                    meshGeometry.TransparentIndices.Dispose();
                    meshGeometry.TransparentIndices = default;
                }

                if (lightVertexResources.LightLevels.Exists)
                {
                    lightVertexResources.LightLevels.Dispose();
                    lightVertexResources.LightLevels = default;
                }
            }

            _vertices.Value.Clear();
            _indices.Value.Clear();
            _transIndices.Value.Clear();
            _lights.Value.Clear();


            watch.Stop();
            if(watch.Elapsed.TotalMilliseconds > 1) Utilties.Logging.Metrics.LogMetric($"LogicSystems:VoxelGridMesher:Finish", watch.Elapsed.TotalMilliseconds, TimeSpan.FromSeconds(5));
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

        private void AddQuad(Quad quad, Vector3i voxelIndex, VoxelGrid voxels, VoxelSide side, Vector2 textureOffset, Vector2 imageSize,
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
                return (float)voxels.GetLight(lightIndex);
            }
            else
            {
                var spaceIndex = voxels.VoxelSpace.GetSpaceIndexFromVoxelIndex(voxels.MemberIndex, lightIndex);

                var light = voxels.VoxelSpace.GetLight(spaceIndex);
                return light.HasValue ? light.Value : 15;
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
