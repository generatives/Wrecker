using Clunker.Graphics;
using Clunker.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DefaultEcs.System;
using DefaultEcs;
using Clunker.Voxels.Space;
using Clunker.Core;
using System.Numerics;
using Clunker.Physics.Voxels;
using Clunker.Voxels.Lighting;
using Clunker.Voxels.Meshing;

namespace Clunker.Voxels.Space
{
    public class VoxelSpaceExpanderSystem : AEntitySystem<double>
    {
        private Action<Entity> _setVoxelRender;

        public VoxelSpaceExpanderSystem(Action<Entity> setVoxelRender, World world) : base(world.GetEntities().With<VoxelSpaceExpander>().WhenAddedEither<VoxelGrid>().WhenChangedEither<VoxelGrid>().AsSet())
        {
            _setVoxelRender = setVoxelRender;
        }

        protected override void Update(double state, in Entity entity)
        {
            ref var grid = ref entity.Get<VoxelGrid>();
            var spaceEntity = grid.VoxelSpace;
            ref var space = ref spaceEntity.Get<VoxelSpace>();

            if (grid.HasExistingVoxels)
            {
                AddSurrounding(spaceEntity, space, grid.SpaceIndex);
            }
            else
            {
                RemoveSurrounding(spaceEntity, space, grid.SpaceIndex);
            }
        }

        private void AddSurrounding(Entity voxelSpaceEntity, VoxelSpace space, Vector3i addSurrounding)
        {
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    for (int z = -1; z <= 1; z++)
                    {
                        var index = new Vector3i(addSurrounding.X + x, addSurrounding.Y + y, addSurrounding.Z + z);
                        if(index != addSurrounding && !space.Members.ContainsKey(index))
                        {
                            var spaceTransform = voxelSpaceEntity.Get<Transform>();
                            var voxelGridObj = voxelSpaceEntity.World.CreateEntity();
                            var transform = new Transform();
                            transform.Position = new Vector3(index.X * space.GridSize * space.VoxelSize, index.Y * space.GridSize * space.VoxelSize, index.Z * space.GridSize * space.VoxelSize);
                            spaceTransform.AddChild(transform);
                            voxelGridObj.Set(transform);
                            _setVoxelRender(voxelGridObj);
                            voxelGridObj.Set(new PhysicsBlocks());
                            voxelGridObj.Set(new VoxelSpaceExpander());
                            voxelGridObj.Set(new LightField(space.GridSize));
                            voxelGridObj.Set(new LightVertexResources());
                            voxelGridObj.Set(new VoxelGrid(space.GridSize, space.VoxelSize, voxelSpaceEntity, index));

                            space.Members[index] = voxelGridObj;
                            voxelSpaceEntity.Set(space);
                        }
                    }
        }

        private void RemoveSurrounding(Entity voxelSpaceEntity, VoxelSpace space, Vector3i removeSurrounding)
        {
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    for (int z = -1; z <= 1; z++)
                    {
                        var index = new Vector3i(removeSurrounding.X + x, removeSurrounding.Y + y, removeSurrounding.Z + z);
                        if (index != removeSurrounding && space.Members.ContainsKey(index) && !ShouldStay(space, index))
                        {
                            var voxelGridObj = space.Members[index];
                            space.Members.Remove(index);
                            voxelGridObj.Dispose();
                            voxelSpaceEntity.Set(space);
                        }
                    }
        }

        private bool ShouldStay(VoxelSpace space, Vector3i check)
        {
            for(int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    for (int z = -1; z <= 1; z++)
                    {
                        var index = new Vector3i(check.X + x, check.Y + y, check.Z + z);

                        if (space.Members.ContainsKey(index) && space.Members[index].Get<VoxelGrid>().HasExistingVoxels)
                        {
                            return true;
                        }
                    }
            return false;
        }
    }
}
