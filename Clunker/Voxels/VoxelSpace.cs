using Clunker.Geometry;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text;

namespace Clunker.Voxels
{
    public class VoxelSpace : Component, IEnumerable<KeyValuePair<Vector3i, VoxelGrid>>, IComponentEventListener
    {
        public event Action<Vector3i, VoxelGrid> VoxelsChanged;
        public event Action<Vector3i, VoxelGrid> GridAdded;
        public event Action<Vector3i, VoxelGrid> GridRemoved;

        private Dictionary<Vector3i, VoxelGrid> _grids;
        private Dictionary<VoxelGrid, Vector3i> _indices;

        public Vector3i GridSize { get; private set; }
        public float VoxelSize { get; private set; }

        public VoxelGrid this[Vector3i index]
        {
            get
            {
                return _grids.ContainsKey(index) ? _grids[index] : null;
            }
            set
            {
                Add(index, value);
            }
        }

        public Vector3i? this[VoxelGrid grid]
        {
            get
            {
                if(_indices.ContainsKey(grid))
                {
                    return _indices[grid];
                }
                else
                {
                    return null;
                }
            }
        }

        public IEnumerable<GameObject> VoxelEntities => _grids.Values.SelectMany(g => g.VoxelEntities);

        public VoxelSpace(Vector3i gridSize, float voxelSize)
        {
            _grids = new Dictionary<Vector3i, VoxelGrid>();
            _indices = new Dictionary<VoxelGrid, Vector3i>();
            GridSize = gridSize;
            VoxelSize = voxelSize;
        }

        public GameObject Add(Vector3i index, VoxelGrid grid)
        {
            var gameObject = new GameObject();
            gameObject.AddComponent(grid);
            Add(index, gameObject);
            return gameObject;
        }

        public void Add(Vector3i index, GameObject gameObject)
        {
            var grid = gameObject.GetComponent<VoxelGrid>();
            if(grid != null)
            {
                Remove(index);
                gameObject.Transform.Position = new Vector3(index.X * GridSize.X * VoxelSize, index.Y * GridSize.Y * VoxelSize, index.Z * GridSize.Z * VoxelSize);
                GameObject.AddChild(gameObject);
                _grids[index] = grid;
                _indices[grid] = index;
                if(IsAlive) grid.VoxelsChanged += Grid_VoxelsChanged;
                GridAdded?.Invoke(index, grid);
            }
        }

        private void Grid_VoxelsChanged(VoxelGrid grid)
        {
            VoxelsChanged?.Invoke(_indices[grid], grid);
        }

        public GameObject Remove(Vector3i index)
        {
            if(_grids.ContainsKey(index))
            {
                var grid = _grids[index];
                _grids.Remove(index);
                _indices.Remove(grid);
                grid.VoxelsChanged -= Grid_VoxelsChanged;
                GameObject.RemoveChild(grid.GameObject);
                GridRemoved?.Invoke(index, grid);
                return grid.GameObject;
            }
            else
            {
                return null;
            }
        }

        public Voxel? GetVoxel(Vector3i index)
        {
            var gridsIndex = new Vector3i(
                (int)MathF.Floor((float)index.X / GridSize.X),
                (int)MathF.Floor((float)index.Y / GridSize.Y),
                (int)MathF.Floor((float)index.Z / GridSize.Z));

            var voxelIndex = new Vector3i(
                index.X - gridsIndex.X * GridSize.X,
                index.Y - gridsIndex.Y * GridSize.Y,
                index.Z - gridsIndex.Z * GridSize.Z);

            var grid = this[gridsIndex];

            return grid?.Data[voxelIndex];
        }

        public void SetVoxel(Vector3i index, Voxel voxel, params VoxelEntity[] entities)
        {
            var gridsIndex = new Vector3i(
                (int)MathF.Floor((float)index.X / GridSize.X),
                (int)MathF.Floor((float)index.Y / GridSize.Y),
                (int)MathF.Floor((float)index.Z / GridSize.Z));

            var voxelIndex = new Vector3i(
                index.X - gridsIndex.X * GridSize.X,
                index.Y - gridsIndex.Y * GridSize.Y,
                index.Z - gridsIndex.Z * GridSize.Z);

            var grid = this[gridsIndex];

            grid?.SetVoxel(voxelIndex, voxel, entities);
        }

        public Vector3i GetGridIndexFromLocalPosition(Vector3 position)
        {
            var spaceIndex = new Vector3i(
                (int)MathF.Floor(position.X / VoxelSize),
                (int)MathF.Floor(position.Y / VoxelSize),
                (int)MathF.Floor(position.Z / VoxelSize));

            return new Vector3i(
                (int)MathF.Floor((float)spaceIndex.X / GridSize.X),
                (int)MathF.Floor((float)spaceIndex.Y / GridSize.Y),
                (int)MathF.Floor((float)spaceIndex.Z / GridSize.Z));
        }

        public Vector3i GetGridIndexFromWorldPosition(Vector3 position)
        {
            return GetGridIndexFromLocalPosition(GameObject.Transform.GetLocal(position));
        }

        public Vector3i GetSpaceIndexFromVoxelIndex(Vector3i gridsIndex, Vector3i voxelIndex)
        {
            return gridsIndex * GridSize + voxelIndex;
        }

        public IEnumerator<KeyValuePair<Vector3i, VoxelGrid>> GetEnumerator()
        {
            return _grids.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void ComponentStarted()
        {
            foreach (var grid in _grids.Values)
            {
                grid.VoxelsChanged += Grid_VoxelsChanged;
            }
        }

        public void ComponentStopped()
        {
        }
    }
}
