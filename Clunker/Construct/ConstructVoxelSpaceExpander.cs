using Clunker.Graphics;
using Clunker.Math;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentsInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clunker.Voxels
{
    public class ConstructVoxelSpaceExpander : Component, IComponentEventListener
    {
        private VoxelSpace _space;
        private VoxelTypes _types;
        private MaterialInstance _materialInstance;

        public ConstructVoxelSpaceExpander(VoxelTypes types, MaterialInstance materialInstance)
        {
            _types = types;
            _materialInstance = materialInstance;
        }

        public void ComponentStarted()
        {
            _space = GameObject.GetComponent<VoxelSpace>();
            _space.VoxelsChanged += Space_VoxelsChanged;
            foreach(var (gridIndex, grid) in _space.ToList())
            {
                Space_VoxelsChanged(gridIndex, grid);
            }
        }

        private void Space_VoxelsChanged(Vector3i index, VoxelGrid grid)
        {
            if(grid.Data.HasExistingVoxels)
            {
                AddSurrounding(index);
            }
            else
            {
                RemoveSurrounding(index);
            }
        }

        private void AddSurrounding(Vector3i addSurrounding)
        {
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    for (int z = -1; z <= 1; z++)
                    {
                        var index = new Vector3i(addSurrounding.X + x, addSurrounding.Y + y, addSurrounding.Z + z);
                        if(index != addSurrounding && _space[index] == null)
                        {
                            var voxelGridObj = new GameObject("Spaceship Voxel Grid");
                            voxelGridObj.AddComponent(new VoxelGrid(new VoxelGridData(8, 8, 8, 1), new Dictionary<Vector3i, GameObject>()));
                            voxelGridObj.AddComponent(new VoxelMeshRenderable(_types, _materialInstance));
                            voxelGridObj.AddComponent(new VoxelGridRenderable(_types, _materialInstance));

                            _space.Add(index, voxelGridObj);
                        }
                    }
        }

        private void RemoveSurrounding(Vector3i removeSurrounding)
        {
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    for (int z = -1; z <= 1; z++)
                    {
                        var index = new Vector3i(removeSurrounding.X + x, removeSurrounding.Y + y, removeSurrounding.Z + z);
                        if (index != removeSurrounding && !ShouldStayExcept(index, removeSurrounding))
                        {
                            _space.Remove(index);
                        }
                    }
        }

        private bool ShouldStayExcept(Vector3i check, Vector3i except)
        {
            for(int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    for (int z = -1; z <= 1; z++)
                    {
                        var index = new Vector3i(check.X + x, check.Y + y, check.Z + z);
                        if(index != except)
                        {
                            var grid = _space[index];
                            if (grid != null && grid.Data.HasExistingVoxels)
                            {
                                return false;
                            }
                        }
                    }
            return true;
        }

        public void ComponentStopped()
        {
            _space.VoxelsChanged -= Space_VoxelsChanged;
        }
    }
}
