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

namespace Clunker.Voxels.Space
{
    public class VoxelSpaceExpanderSystem : AEntitySystem<double>
    {
        private MaterialInstance _materialInstance;

        public VoxelSpaceExpanderSystem(MaterialInstance materialInstance, World world) : base(world.GetEntities().With<VoxelSpaceMember>().With<VoxelSpaceExpander>().WhenChanged<VoxelGrid>().AsSet())
        {
            _materialInstance = materialInstance;
        }

        protected override void Update(double state, in Entity entity)
        {
            ref var grid = ref entity.Get<VoxelGrid>();
            ref var spaceMember = ref entity.Get<VoxelSpaceMember>();
            var spaceEntity = spaceMember.Parent;
            ref var space = ref spaceEntity.Get<VoxelSpace>();

            if (grid.HasExistingVoxels)
            {
                AddSurrounding(spaceEntity, space, spaceMember.Index);
            }
            else
            {
                RemoveSurrounding(spaceEntity, space, spaceMember.Index);
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
                            var voxelGridObj = voxelSpaceEntity.World.CreateEntity();
                            var transform = new Transform();
                            transform.Position = new Vector3(index.X * space.GridSize * space.VoxelSize, index.Y * space.GridSize * space.VoxelSize, index.Z * space.GridSize * space.VoxelSize);
                            voxelGridObj.Set(transform);
                            voxelGridObj.Set(_materialInstance);
                            voxelGridObj.Set(new ExposedVoxels());
                            voxelGridObj.Set(new VoxelGrid(8, space.VoxelSize));
                            voxelGridObj.Set(new VoxelSpaceMember() { Parent = voxelSpaceEntity, Index = index });
                            voxelGridObj.Set(new VoxelSpaceExpander());

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
                        if (index != removeSurrounding && !ShouldStay(space, index))
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
