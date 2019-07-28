using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Clunker.Graphics;
using Clunker.Math;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
using Clunker.SceneGraph.Core;
using Clunker.Voxels;
using ImGuiNET;

namespace Clunker.Tooling
{
    public abstract class VoxelAddingTool : VoxelEditingTool, IComponentEventListener
    {
        private VoxelGrid _displayGrid;
        private static MaterialInstance _materialInstance;
        private VoxelTypes _types;

        protected VoxelSide Orientation { get; private set; }
        protected ushort VoxelType { get; private set; }

        public VoxelAddingTool(ushort voxelType, VoxelTypes types, MaterialInstance materialInstance)
        {
            VoxelType = voxelType;
            _types = types;
            _materialInstance = materialInstance;
        }

        public void ComponentStarted()
        {
            if(_displayGrid == null)
            {
                _displayGrid = new VoxelGrid(new VoxelGridData(1, 1, 1, 1), new Dictionary<Vector3i, GameObject>());
                var gameObject = new GameObject($"{Name} Display Object");
                gameObject.Transform.InheiritParentTransform = false;
                gameObject.AddComponent(_displayGrid);
                gameObject.AddComponent(new VoxelMeshRenderable(_types, _materialInstance));
                GameObject.AddChild(_displayGrid.GameObject);
            }
            _displayGrid.GameObject.IsActive = true;
        }

        public void ComponentStopped()
        {
            _displayGrid.GameObject.IsActive = false;
        }

        protected override void DoVoxelAction(VoxelSpace space, Vector3 hitLocation, Vector3i index)
        {
            var addIndex = CalculateAddIndex(space, hitLocation, index);
            if(addIndex.HasValue)
            {
                AddVoxel(space, addIndex.Value);
            }
        }

        protected override void DrawVoxelChange(VoxelSpace space, Vector3 hitLocation, Vector3i index)
        {
            if(space == null)
            {
                _displayGrid.GameObject.IsActive = false;
            }
            else
            {
                _displayGrid.GameObject.IsActive = true;
                var displayVoxel = _displayGrid.GetVoxel(new Vector3i(0, 0, 0));
                var newVoxel = new Voxel()
                {
                    Exists = true,
                    BlockType = VoxelType,
                    Orientation = Orientation
                };
                if (displayVoxel != newVoxel)
                {
                    _displayGrid.SetVoxel(new Vector3i(0, 0, 0), newVoxel);
                }
                var addIndex = CalculateAddIndex(space, hitLocation, index);
                if (addIndex.HasValue)
                {
                    var localPosition = addIndex.Value * space.VoxelSize;
                    var worldPosition = space.GameObject.Transform.GetWorld(localPosition);
                    _displayGrid.GameObject.Transform.Position = worldPosition;
                    _displayGrid.GameObject.Transform.Orientation = space.GameObject.Transform.WorldOrientation;
                }
            }
        }

        private Vector3i? CalculateAddIndex(VoxelSpace space, Vector3 hitLocation, Vector3i index)
        {
            var size = space.VoxelSize;
            var voxelLocation = index * size;
            var relativeLocation = space.GameObject.Transform.GetLocal(hitLocation);
            if (NearlyEqual(relativeLocation.X, voxelLocation.X))
            {
                return new Vector3i(index.X - 1, index.Y, index.Z);
            }
            else if (NearlyEqual(relativeLocation.X, voxelLocation.X + size))
            {
                return new Vector3i(index.X + 1, index.Y, index.Z);
            }
            else if (NearlyEqual(relativeLocation.Y, voxelLocation.Y))
            {
                return new Vector3i(index.X, index.Y - 1, index.Z);
            }
            else if (NearlyEqual(relativeLocation.Y, voxelLocation.Y + size))
            {
                return new Vector3i(index.X, index.Y + 1, index.Z);
            }
            else if (NearlyEqual(relativeLocation.Z, voxelLocation.Z))
            {
                return new Vector3i(index.X, index.Y, index.Z - 1);
            }
            else if (NearlyEqual(relativeLocation.Z, voxelLocation.Z + size))
            {
                return new Vector3i(index.X, index.Y, index.Z + 1);
            }
            else
            {
                return null;
            }
        }

        public override void BuildMenu()
        {
            var sides = Enum.GetNames(typeof(VoxelSide));
            var selectedOrientation = (int)Orientation;
            ImGui.Combo("Orientation", ref selectedOrientation, sides, sides.Length);
            Orientation = (VoxelSide)selectedOrientation;
        }

        public abstract void AddVoxel(VoxelSpace space, Vector3i index);

        public static bool NearlyEqual(float f1, float f2) => System.Math.Abs(f1 - f2) < 0.01;
    }
}
