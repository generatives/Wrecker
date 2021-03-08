using Clunker.Core;
using Clunker.ECS;
using Clunker.Editor.SelectedEntity;
using Clunker.Editor.Utilities;
using Clunker.Geometry;
using Clunker.Graphics;
using Clunker.Graphics.Components;
using Clunker.Networking;
using Clunker.Physics;
using Clunker.Physics.Voxels;
using Clunker.Voxels;
using Clunker.Voxels.Lighting;
using Clunker.Voxels.Meshing;
using Clunker.Voxels.Serialization;
using Clunker.Voxels.Space;
using DefaultEcs;
using ImGuiNET;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Clunker.Editor.VoxelSpaceLoader
{
    [MessagePackObject]
    public struct VoxelSpaceLoadMessage
    {
        [Key(0)]
        public VoxelSpaceData VoxelSpaceData;
        [Key(1)]
        public Vector3 Position;
        [Key(2)]
        public string Name { get; set; }
    }

    public class VoxelSpaceLoader : Editor
    {
        public override string Name => "Voxel Space Loader";
        public override string Category => "Voxels";
        public override char? HotKey => 'L';

        private EntitySet _selectedEntities;
        private EntitySet _cameraEntities;
        private Transform CameraTransform => _cameraEntities.GetEntities()[0].Get<Transform>();
        private MessagingChannel _serverChannel;

        private string _fileLocation = "C:\\Clunker";

        public VoxelSpaceLoader(MessagingChannel serverChannel, World world)
        {
            _serverChannel = serverChannel;
            _selectedEntities = world.GetEntities().With<SelectedEntityFlag>().AsSet();
            _cameraEntities = world.GetEntities().With<Camera>().AsSet();
            PopulateQuickMenu();
        }

        public override void DrawEditor(double delta)
        {
            foreach(var entity in _selectedEntities.GetEntities())
            {
                if(entity.Has<VoxelSpace>())
                {
                    ImGui.Text($"Entity {entity}");
                    ImGui.SameLine();
                    if (ImGui.Button("Save"))
                    {
                        FilePicker.Open("save-voxel-space");
                    }

                    if(FilePicker.Window("save-voxel-space", ref _fileLocation, new[] { ".cvx" }))
                    {
                        if(!Directory.Exists(_fileLocation))
                        {
                            ref var space = ref entity.Get<VoxelSpace>();
                            var voxelSpaceData = new VoxelSpaceData()
                            {
                                VoxelSize = space.VoxelSize,
                                GridSize = space.GridSize,
                                Grids = space.Select(kvp => (kvp.Key, kvp.Value.Get<VoxelGrid>().Voxels)).ToArray()
                            };
                            VoxelSpaceDataSerializer.Serialize(voxelSpaceData, File.OpenWrite(_fileLocation));
                        }
                    }

                    ImGui.Separator();
                }
            }

            if (ImGui.Button("Load File"))
            {
                FilePicker.Open("load-voxel-space");
            }

            if (FilePicker.Window("load-voxel-space", ref _fileLocation, new[] { ".cvx" }))
            {
                LoadFile(_fileLocation);
                PopulateQuickMenu();
            }

            if(ImGui.Button("Load Empty"))
            {
            }
        }

        private void LoadFile(string fileLocation)
        {
            if (File.Exists(fileLocation))
            {
                var name = Path.GetFileNameWithoutExtension(fileLocation);
                var voxelSpaceData = VoxelSpaceDataSerializer.Deserialize(File.OpenRead(fileLocation));
                _serverChannel.AddBuffered<VoxelSpaceLoadReciever, VoxelSpaceLoadMessage>(new VoxelSpaceLoadMessage()
                {
                    VoxelSpaceData = voxelSpaceData,
                    Position = CameraTransform.WorldPosition,
                    Name = name
                });
            }
        }

        private void LoadEmpty()
        {
            var voxels = new Voxel[8 * 8 * 8];
            voxels[0] = new Voxel() { Exists = true };
            var voxelSpaceData = new VoxelSpaceData()
            {
                VoxelSize = 1,
                GridSize = 8,
                Grids = new[]
                {
                        (new Vector3i(0, 0, 0), voxels)
                    }
            };
            _serverChannel.AddBuffered<VoxelSpaceLoadReciever, VoxelSpaceLoadMessage>(new VoxelSpaceLoadMessage()
            {
                VoxelSpaceData = voxelSpaceData,
                Position = CameraTransform.WorldPosition,
                Name = "Empty Voxel Space"
            });
        }

        private void PopulateQuickMenu()
        {
            var menu = new List<(string, Action)>();
            menu.Add(("Load Empty", LoadEmpty));

            var currentDirectory = Directory.Exists(_fileLocation) ? _fileLocation : (new FileInfo(_fileLocation)).DirectoryName;
            var files = Directory.EnumerateFiles(currentDirectory);
            menu.AddRange(files.Select<string, (string, Action)>(f => (Path.GetFileName(f), () => LoadFile(f))));

            QuickMenu = menu.ToArray();
        }
    }

    public class VoxelSpaceLoadReciever : MessagePackMessageReciever<VoxelSpaceLoadMessage>
    {
        private World _world;

        public VoxelSpaceLoadReciever(World world)
        {
            _world = world;
        }

        protected override void MessageReceived(in VoxelSpaceLoadMessage message)
        {
            LoadAsDynamic(message.VoxelSpaceData, message.Position, message.Name);
        }

        private void LoadAsDynamic(VoxelSpaceData voxelSpaceData, Vector3 position, string name)
        {
            var spaceEntity = _world.CreateEntity();
            spaceEntity.Set(new NetworkedEntity() { Id = Guid.NewGuid() });
            spaceEntity.Set(new EntityMetaData() { Name = name });

            var space = new VoxelSpace(voxelSpaceData.GridSize, voxelSpaceData.VoxelSize, spaceEntity);
            var spaceTransform = new Transform(spaceEntity)
            {
                WorldPosition = position
            };

            foreach (var (index, voxels) in voxelSpaceData.Grids)
            {
                var gridEntity = _world.CreateEntity();
                gridEntity.Set(new NetworkedEntity() { Id = Guid.NewGuid() });

                var gridTransform = new Transform(gridEntity);
                gridTransform.Position = Vector3.One * voxelSpaceData.GridSize * voxelSpaceData.VoxelSize * index;
                spaceTransform.AddChild(gridTransform);
                gridEntity.Set(gridTransform);
                gridEntity.Set(new PhysicsBlocks());
                gridEntity.Set(new VoxelSpaceExpander());
                gridEntity.Set(new VoxelGrid(voxelSpaceData.VoxelSize, voxelSpaceData.GridSize, space, index, voxels));

                space[index] = gridEntity;
            }

            spaceEntity.Set(spaceTransform);
            spaceEntity.Set(new VoxelSpaceDynamicBody());
            spaceEntity.Set(new DynamicBody());
            spaceEntity.Set(space);

            _world.Publish(new SelectEntityRequest() { Entity = spaceEntity });
        }
    }
}
