using Clunker.Core;
using Clunker.Geometry;
using Clunker.Graphics;
using Clunker.Input;
using Clunker.Networking;
using Clunker.Physics;
using Clunker.Voxels;
using Clunker.Voxels.Space;
using DefaultEcs;
using ImGuiNET;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Clunker.Editor.VoxelEditor
{
    [MessagePackObject]
    public struct VoxelEditMessage
    {
        [Key(0)]
        public Vector3 Position;
        [Key(1)]
        public Quaternion Orientation;
        [Key(2)]
        public Voxel Voxel;
    }

    public class VoxelEditor : Editor
    {
        public override string Name => "Voxel Editor";
        public override string Category => "Voxels";
        public override char? HotKey => 'T';

        private (string Name, Voxel Voxel)[] _voxels;
        private int _index;

        private MessagingChannel _serverChannel;
        private EntitySet _cameras;

        public VoxelEditor(MessagingChannel serverChannel, World world, (string, Voxel)[] voxels)
        {
            _serverChannel = serverChannel;
            _cameras = world.GetEntities().With<Camera>().AsSet();
            _voxels = voxels;
        }

        public override void DrawEditor(double delta)
        {
            var io = ImGui.GetIO();

            _index = Math.Max(Math.Min(_index - (int)io.MouseWheel, _voxels.Length - 1), 0);
            if (ImGui.BeginCombo("Tool", _voxels[_index].Name)) // The second parameter is the label previewed before opening the combo.
            {
                for (int i = 0; i < _voxels.Length; i++)
                {
                    bool is_selected = (i == _index); // You can store your selection however you want, outside or inside your objects
                    if (ImGui.Selectable(_voxels[i].Name, is_selected))
                        _index = i;
                    if (is_selected)
                        ImGui.SetItemDefaultFocus();   // You may set the initial focus when opening the combo (scrolling + for keyboard navigation support)
                }
                ImGui.EndCombo();
            }

            if (InputTracker.LockMouse && InputTracker.WasMouseButtonDowned(Veldrid.MouseButton.Left))
            {
                foreach (var entity in _cameras.GetEntities())
                {
                    var transform = entity.Get<Transform>();
                    var message = new VoxelEditMessage()
                    {
                        Position = transform.WorldPosition,
                        Orientation = transform.WorldOrientation,
                        Voxel = _voxels[_index].Voxel
                    };
                    _serverChannel.AddBuffered<VoxelEditReceiver, VoxelEditMessage>(message);
                }
            }
        }
    }

    public class VoxelEditReceiver : EntityMessageApplier<VoxelEditMessage>
    {
        public World World;
        public PhysicsSystem PhysicsSystem;

        public VoxelEditReceiver(NetworkedEntities entities) : base(entities)
        {
        }

        protected override void MessageReceived(in VoxelEditMessage messageData, in Entity entity)
        {
            base.MessageReceived(messageData, entity);
        }
        public void Run(Vector3 position, Quaternion orientation, Voxel voxel)
        {
            var transform = new Transform()
            {
                WorldPosition = position,
                WorldOrientation = orientation
            };
            var result = PhysicsSystem.Raycast(transform);
            if (result.Hit)
            {
                var forward = transform.WorldOrientation.GetForwardVector();
                var hitLocation = transform.WorldPosition + forward * result.T;
                var hitEntity = result.Entity;
                if (hitEntity.Has<VoxelSpace>())
                {
                    ref var space = ref hitEntity.Get<VoxelSpace>();
                    var hitTransform = hitEntity.Get<Transform>();

                    // Nudge forward a little so we are inside the block
                    var insideHitLocation = hitTransform.GetLocal(transform.WorldPosition + forward * result.T + forward * 0.01f);
                    var index = new Vector3i(
                        (int)Math.Floor(insideHitLocation.X),
                        (int)Math.Floor(insideHitLocation.Y),
                        (int)Math.Floor(insideHitLocation.Z));

                    space.SetVoxel(index, voxel);
                }
                if (hitEntity.Has<VoxelGrid>())
                {
                    ref var voxels = ref hitEntity.Get<VoxelGrid>();

                    var hitTransform = voxels.VoxelSpace.Self.Get<Transform>();
                    // Nudge forward a little so we are inside the block
                    var insideHitLocation = hitTransform.GetLocal(transform.WorldPosition + forward * result.T + forward * 0.01f);
                    var index = new Vector3i(
                        (int)Math.Floor(insideHitLocation.X),
                        (int)Math.Floor(insideHitLocation.Y),
                        (int)Math.Floor(insideHitLocation.Z));

                    voxels.VoxelSpace.SetVoxel(index, voxel);
                }
            }
        }
    }
}
