using Clunker.Core;
using Clunker.Geometry;
using Clunker.Graphics;
using Clunker.Physics;
using Clunker.Voxels;
using Clunker.Voxels.Lighting;
using Clunker.Voxels.Meshing;
using Clunker.Voxels.Space;
using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Editor.Toolbar
{
    public class RemoveVoxelEditingTool : VoxelEditingTool
    {
        public override string Name => "Remove";

        private Entity _displaySpaceEntity;
        private Entity _displayGridEntity;

        public RemoveVoxelEditingTool(Action<Entity> setVoxelRender, World world, PhysicsSystem physicsSystem, Entity entity) : base(world, physicsSystem, entity)
        {
            _displaySpaceEntity = world.CreateEntity();
            var voxelSpace = new VoxelSpace(1, 1, _displaySpaceEntity);
            _displaySpaceEntity.Set(voxelSpace);
            var spaceTransform = new Transform();
            _displaySpaceEntity.Set(spaceTransform);

            _displayGridEntity = world.CreateEntity();
            _displayGridEntity.Set(new VoxelGrid(1, 1, voxelSpace, Vector3i.Zero));
            voxelSpace[Vector3i.Zero] = _displayGridEntity;
            setVoxelRender(_displayGridEntity);
            var gridTransform = new Transform();
            spaceTransform.AddChild(gridTransform);
            _displayGridEntity.Set(gridTransform);
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

        protected override void DoVoxelAction(VoxelSpace voxels, Transform hitTransform, Vector3 hitLocation, Vector3i index)
        {
            voxels.SetVoxel(index, new Voxel() { Exists = false });
        }

        protected override void DrawVoxelChange(VoxelSpace voxels, Transform hitTransform, Vector3 hitLocation, Vector3i index)
        {
            var currentVoxel = voxels.GetVoxel(index);
            if (currentVoxel.HasValue)
            {
                ref var displayVoxels = ref _displayGridEntity.Get<VoxelGrid>();
                displayVoxels.SetVoxel(new Vector3i(0, 0, 0), currentVoxel.Value);
                _displayGridEntity.Set(displayVoxels);
            }

            var localPosition = index * voxels.VoxelSize;
            var worldPosition = hitTransform.GetWorld(localPosition);

            ref var displayTransform = ref _displaySpaceEntity.Get<Transform>();
            displayTransform.WorldPosition = worldPosition;
            displayTransform.WorldOrientation = hitTransform.WorldOrientation;
            _displaySpaceEntity.Set(displayTransform);
        }
    }
}
