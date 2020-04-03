using Clunker.Geometry;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
using Clunker.Voxels;
using Hyperion;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Clunker.Voxels
{
    public class VoxelGrid : Component
    {
        public event Action<VoxelGrid> VoxelsChanged;

        public VoxelGridData Data { get; private set; }

        private Dictionary<Vector3i, GameObject> _voxelEntities;
        public IEnumerable<GameObject> VoxelEntities => _voxelEntities.Values;

        [Ignore]
        private bool _requestedVoxelsChanged;

        public VoxelGrid(VoxelGridData voxels, Dictionary<Vector3i, GameObject> voxelEntities)
        {
            Data = voxels;
            Data.Changed += Data_Changed;

            _voxelEntities = voxelEntities;
        }

        private void Data_Changed()
        {
            if (!_requestedVoxelsChanged && VoxelsChanged != null)
            {
                this.EnqueueFrameJob(StartVoxelsChanged);
                _requestedVoxelsChanged = true;
            }
        }

        private void StartVoxelsChanged()
        {
            VoxelsChanged?.Invoke(this);
            _requestedVoxelsChanged = false;
        }

        public Voxel GetVoxel(Vector3i index)
        {
            return Data[index];
        }

        public void SetVoxel(Vector3i index, Voxel voxel, params VoxelEntity[] entities)
        {
            if(Data.SetVoxel(index, voxel))
            {
                if (_voxelEntities.ContainsKey(index))
                {
                    var oldEntity = _voxelEntities[index];
                    GameObject.RemoveChild(oldEntity);
                    GameObject.CurrentScene.RemoveGameObject(oldEntity);
                    _voxelEntities.Remove(index);
                }

                if(entities.Any())
                {
                    var gameObject = new GameObject();
                    gameObject.Transform.Position = index * Data.VoxelSize + Vector3.One * Data.VoxelSize / 2f;
                    gameObject.Transform.Orientation = voxel.Orientation.GetQuaternion();
                    foreach (var entity in entities)
                    {
                        if (entity.Space != null)
                        {
                            throw new Exception("Tried setting a VoxelEntity which is already in a VoxelSpace");
                        }
                        entity.Space = this;
                        entity.Index = index;
                        entity.Voxel = voxel;
                        gameObject.AddComponent(entity);
                    }
                    GameObject.AddChild(gameObject);
                    _voxelEntities[index] = gameObject;
                }
            }
        }
    }
}
