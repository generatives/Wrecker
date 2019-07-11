using Clunker.Graphics;
using Clunker.Graphics.Materials;
using Clunker.Math;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentsInterfaces;
using Clunker.Voxels;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Clunker.Voxels
{
    public class VoxelSpace : Component
    {
        public VoxelGrid Grid { get; private set; }
        private Dictionary<Vector3i, GameObject> _voxelEntities;

        public event Action VoxelsChanged;
        private bool _requestedVoxelsChanged;

        public VoxelSpace(VoxelGrid voxels, Dictionary<Vector3i, GameObject> voxelEntities)
        {
            Grid = voxels;
            Grid.Changed += Data_Changed;

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
            VoxelsChanged?.Invoke();
            _requestedVoxelsChanged = false;
        }

        public Vector3i GetCastVoxelIndex(Vector3 worldPosition)
        {
            var localPosition = GameObject.Transform.GetLocal(worldPosition);
            var voxelPosition = localPosition / Grid.VoxelSize;
            var voxelIndex = new Vector3i((int)voxelPosition.X, (int)voxelPosition.Y, (int)voxelPosition.Z);

            if(voxelIndex.X == voxelPosition.X)
            {
                if(Grid.Exists(voxelIndex.X - 1, voxelIndex.Y, voxelIndex.Z))
                {
                    voxelIndex.X = voxelIndex.X - 1;
                }
            }

            if (voxelIndex.Y == voxelPosition.Y)
            {
                if (Grid.Exists(voxelIndex.X , voxelIndex.Y - 1, voxelIndex.Z))
                {
                    voxelIndex.Y = voxelIndex.Y - 1;
                }
            }

            if (voxelIndex.Z == voxelPosition.Z)
            {
                if (Grid.Exists(voxelIndex.X, voxelIndex.Y, voxelIndex.Z - 1))
                {
                    voxelIndex.Z = voxelIndex.Z - 1;
                }
            }

            return voxelIndex;
        }

        public void SetVoxel(Vector3i index, Voxel voxel, VoxelEntity entity = null)
        {
            if(Grid.SetVoxel(index, voxel))
            {
                if (_voxelEntities.ContainsKey(index))
                {
                    var oldEntity = _voxelEntities[index];
                    GameObject.RemoveChild(oldEntity);
                    GameObject.CurrentScene.RemoveGameObject(oldEntity);
                    _voxelEntities.Remove(index);
                }

                if(entity != null)
                {
                    if(entity.Space != null)
                    {
                        throw new Exception("Tried setting a VoxelEntity which is already in a VoxelSpace");
                    }
                    entity.Space = this;
                    entity.Index = index;
                    entity.Voxel = voxel;
                    var gameObject = new GameObject();
                    gameObject.AddComponent(entity);
                    gameObject.Transform.Position = index * Grid.VoxelSize + Vector3.One * Grid.VoxelSize / 2f;
                    gameObject.Transform.Orientation = voxel.Orientation.GetQuaternion();
                    GameObject.AddChild(gameObject);
                    GameObject.CurrentScene.AddGameObject(gameObject);
                    _voxelEntities[index] = gameObject;
                }
            }
        }
    }
}
