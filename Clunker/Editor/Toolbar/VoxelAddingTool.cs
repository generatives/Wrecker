using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Clunker.Graphics;
using Clunker.Geometry;
using Clunker.Voxels;
using ImGuiNET;
using DefaultEcs;
using Clunker.Physics;
using Clunker.Core;

namespace Clunker.Editor.Toolbar
{
    public abstract class VoxelAddingTool : VoxelEditingTool
    {
        private Entity _displayGridEntity;
        protected VoxelSide Orientation { get; private set; }
        protected ushort VoxelType { get; private set; }

        public VoxelAddingTool(ushort voxelType, MaterialInstance materialInstance, World world, PhysicsSystem physicsSystem, Entity entity) : base(world, physicsSystem, entity)
        {
            VoxelType = voxelType;

            _displayGridEntity = world.CreateEntity();
            _displayGridEntity.Set(new VoxelGrid(1, 1));
            _displayGridEntity.Set(materialInstance);
            _displayGridEntity.Set(new Transform());
            _displayGridEntity.Disable();
        }

        public override void Selected()
        {
            _displayGridEntity.Enable();
        }

        public override void UnSelected()
        {
            _displayGridEntity.Disable();
        }

        protected override void DoVoxelAction(IVoxels voxels, Transform hitTransform, Vector3 hitLocation, Vector3i index)
        {
            var addIndex = CalculateAddIndex(voxels, hitTransform, hitLocation, index);
            if(addIndex.HasValue)
            {
                AddVoxel(voxels, addIndex.Value);
            }
        }

        protected override void DrawVoxelChange(IVoxels voxels, Transform hitTransform, Vector3 hitLocation, Vector3i index)
        {
            ref var displayVoxels = ref _displayGridEntity.Get<VoxelGrid>();
            var displayVoxel = displayVoxels.GetVoxel(new Vector3i(0, 0, 0));
            var newVoxel = new Voxel()
            {
                Exists = true,
                BlockType = VoxelType,
                Orientation = Orientation
            };
            if (displayVoxel != newVoxel)
            {
                displayVoxels.SetVoxel(new Vector3i(0, 0, 0), newVoxel);
                _displayGridEntity.Set(displayVoxels);
            }
            var addIndex = CalculateAddIndex(voxels, hitTransform, hitLocation, index);
            if (addIndex.HasValue)
            {
                var localPosition = addIndex.Value * voxels.VoxelSize;
                var worldPosition = hitTransform.GetWorld(localPosition);

                ref var displayTransform = ref _displayGridEntity.Get<Transform>();
                displayTransform.WorldPosition = worldPosition;
                displayTransform.WorldOrientation = hitTransform.WorldOrientation;
                _displayGridEntity.Set(displayTransform);
            }
        }

        private Vector3i? CalculateAddIndex(IVoxels voxels, Transform hitTransform, Vector3 hitLocation, Vector3i index)
        {
            var size = voxels.VoxelSize;
            var voxelLocation = index * size;
            var relativeLocation = hitTransform.GetLocal(hitLocation);
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

        public override void Run()
        {
            base.Run();
            var sides = Enum.GetNames(typeof(VoxelSide));
            var selectedOrientation = (int)Orientation;
            ImGui.Combo("Orientation", ref selectedOrientation, sides, sides.Length);
            Orientation = (VoxelSide)selectedOrientation;
        }

        public abstract void AddVoxel(IVoxels voxel, Vector3i index);

        public static bool NearlyEqual(float f1, float f2) => System.Math.Abs(f1 - f2) < 0.01;
    }
}
