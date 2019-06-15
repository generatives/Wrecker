using Clunker.Graphics;
using Clunker.Graphics.Materials;
using Clunker.Math;
using Clunker.SceneGraph;
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

namespace Clunker.Voxels
{
    public class VoxelMesh : Mesh
    {
        private VoxelSpace space;
        private static MaterialInstance _materialInstance;
        private MeshGeometry _meshGeometry;

        private VertexPositionTextureNormal[] _newVertices;
        private ushort[] _newIndices;

        private VoxelTypes _types;

        public VoxelMesh(VoxelTypes types, MaterialInstance materialInstance)
        {
            _types = types;
            _materialInstance = materialInstance;

            _meshGeometry = new MeshGeometry();
        }

        public override void ComponentStarted()
        {
            base.ComponentStarted();

            space = GameObject.GetComponent<VoxelSpace>();
            Debug.Assert(space != null, "VoxelMesh needs a VoxelSpace component");
            space.VoxelsChanged += GenerateMesh;
            GenerateMesh();
        }

        private void GenerateMesh()
        {
            this.EnqueueWorkerJob(() =>
            {
                var voxels = space.Data;
                var vertices = new List<VertexPositionTextureNormal>(voxels.XLength * voxels.YLength * voxels.ZLength);
                var indices = new List<ushort>(voxels.XLength * voxels.YLength * voxels.ZLength);
                MeshGenerator.GenerateMesh(voxels, (voxel, side, quad) =>
                {
                    var type = _types.Types[voxel.BlockType];
                    var textureOffset = side == VoxelSide.TOP ? type.TopTexCoords :
                        side == VoxelSide.BOTTOM ? type.BottomTexCoords : type.SideTexCoords;
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
                _newVertices = vertices.ToArray();
                _newIndices = indices.ToArray();
                this.EnqueueFrameJob(UpdateGeometry);
            });
        }

        private void UpdateGeometry()
        {
            _meshGeometry.UpdateMesh(_newVertices, _newIndices);
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

        public override (MeshGeometry, MaterialInstance) ProvideMeshAndMaterial()
        {
            return (_meshGeometry, _materialInstance);
        }

        public override void ComponentStopped()
        {
            base.ComponentStopped();
            _meshGeometry.Dispose();
            space.VoxelsChanged -= GenerateMesh;
        }
    }
}
