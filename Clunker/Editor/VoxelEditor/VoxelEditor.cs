using Clunker.Core;
using Clunker.Geometry;
using Clunker.Graphics;
using Clunker.Input;
using Clunker.Networking;
using Clunker.Physics;
using Clunker.Utilties;
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
        [Key(3)]
        public bool Remove;
        [Key(4)]
        public int Size;
    }

    public class VoxelEditor : Editor
    {
        public override string Name => "Voxel Editor";
        public override string Category => "Voxels";
        public override char? HotKey => 'T';

        private (string Name, Voxel Voxel)[] _voxels;
        private int _index;
        private int _size;

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

            ImGui.DragInt("Size", ref _size, 0.25f, 0, 10);

            var added = InputTracker.WasMouseButtonDowned(Veldrid.MouseButton.Left);
            var removed = InputTracker.WasMouseButtonDowned(Veldrid.MouseButton.Right);

            if (InputTracker.LockMouse && (added || removed))
            {
                foreach (var entity in _cameras.GetEntities())
                {
                    var transform = entity.Get<Transform>();
                    var message = new VoxelEditMessage()
                    {
                        Position = transform.WorldPosition,
                        Orientation = transform.WorldOrientation,
                        Voxel = _voxels[_index].Voxel,
                        Remove = removed,
                        Size = _size
                    };
                    _serverChannel.AddBuffered<VoxelEditReceiver, VoxelEditMessage>(message);
                }
            }
        }
    }

    public class VoxelEditReceiver : MessagePackMessageReciever<VoxelEditMessage>
    {
        private PhysicsSystem _physicsSystem;

        public VoxelEditReceiver(PhysicsSystem physicsSystem)
        {
            _physicsSystem = physicsSystem;
        }

        protected override void MessageReceived(in VoxelEditMessage message)
        {
            Run(message.Position, message.Orientation, message.Voxel, message.Remove, message.Size);
        }

        public void Run(Vector3 position, Quaternion orientation, Voxel voxel, bool remove, int size)
        {
            var transform = new Transform(default)
            {
                WorldPosition = position,
                WorldOrientation = orientation
            };
            var result = _physicsSystem.Raycast(transform);
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

                    SetVoxel(space, hitTransform, hitLocation, index, voxel, remove, size);
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

                    SetVoxel(voxels.VoxelSpace, hitTransform, hitLocation, index, voxel, remove, size);
                }
            }
        }

        private static void SetVoxel(VoxelSpace space, Transform hitTransform, Vector3 hitLocation, Vector3i index, Voxel voxel, bool remove, int size)
        {
            if (remove)
            {
                foreach(var i in GeometricUtils.CenteredRectangle(index, size))
                {
                    space.SetVoxel(i, new Voxel() { Exists = false });
                }
            }
            else
            {
                var addIndex = CalculateAddIndex(space, hitTransform, hitLocation, index);
                if (addIndex.HasValue)
                {
                    foreach (var i in GeometricUtils.CenteredRectangle(addIndex.Value, size))
                    {
                        space.SetVoxel(i, voxel);
                    }
                }
            }
        }

        private static Vector3i? CalculateAddIndex(VoxelSpace voxels, Transform hitTransform, Vector3 hitLocation, Vector3i index)
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

        public static bool NearlyEqual(float f1, float f2) => System.Math.Abs(f1 - f2) < 0.01;
    }
}
