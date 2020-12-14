using Clunker;
using Clunker.Core;
using Clunker.ECS;
using Clunker.Editor;
using Clunker.Editor.Logging.Metrics;
using Clunker.Editor.Scene;
using Clunker.Editor.SelectedEntity;
using Clunker.Editor.VoxelEditor;
using Clunker.Editor.VoxelSpaceLoader;
using Clunker.Geometry;
using Clunker.Graphics;
using Clunker.Graphics.Systems;
using Clunker.Networking;
using Clunker.Networking.EntityExistence;
using Clunker.Networking.Sync;
using Clunker.Physics;
using Clunker.Physics.Character;
using Clunker.Physics.Voxels;
using Clunker.Resources;
using Clunker.Voxels;
using Clunker.Voxels.Lighting;
using Clunker.Voxels.Meshing;
using Clunker.Voxels.Space;
using Clunker.WorldSpace;
using DefaultEcs;
using DefaultEcs.Command;
using DefaultEcs.System;
using DefaultEcs.Threading;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Xml.Linq;
using Veldrid;
using Veldrid.StartupUtilities;
using Wrecker;

namespace ClunkerECSDemo
{
    class Program
    {
        private static byte _messageId = 0;
        private static Dictionary<int, Type> _byNum = new Dictionary<int, Type>();
        private static Dictionary<Type, int> _byType = new Dictionary<Type, int>();

        private static MessageTargetMap _messageTargetMap;

        private static MessagingChannel _clientMessagingChannel;
        private static ClunkerClientRunner _client;

        private static ClunkerServerRunner _server;

        private static EditorMenu _editorMenu;

        static void Main(string[] args)
        {
            WindowCreateInfo wci = new WindowCreateInfo
            {
                X = 100,
                Y = 100,
                WindowWidth = 1280,
                WindowHeight = 720,
                WindowTitle = "Tortuga Demo"
            };
            GraphicsDeviceOptions options = new GraphicsDeviceOptions(
                debug: false,
                swapchainDepthFormat: PixelFormat.R16_UNorm,
                syncToVerticalBlank: true,
                resourceBindingModel: ResourceBindingModel.Improved,
                preferDepthRangeZeroToOne: true,
                preferStandardClipSpaceYDirection: true);
#if DEBUG
            options.Debug = true;
#endif

            _editorMenu = new EditorMenu();

            Message<TransformMessageApplier>();
            Message<InputForceApplier>();
            Message<SimpleCameraMover>();
            Message<ClientEntityAssignmentApplier>();
            Message<VoxelSpaceMessageApplier>();
            Message<VoxelGridMessageApplier>();
            Message<VoxelGridChangeMessageApplier>();
            Message<EntityRemover>();
            Message<VoxelEditReceiver>();
            Message<VoxelSpaceLoadReciever>();
            Message<ComponentSyncMessageApplier<EntityMetaData>>();

            _messageTargetMap = new MessageTargetMap(_byType, _byNum);

            _clientMessagingChannel = new MessagingChannel(_messageTargetMap);
            _client = new ClunkerClientRunner(new ResourceLoader(), _messageTargetMap, _clientMessagingChannel);
            _client.Started += _client_Started;

            _server = new ClunkerServerRunner();
            _server.Started += _server_Started;

            var serverTask = _server.Start();
            _client.Start(wci, options).Wait();
        }

        private static void _server_Started()
        {
            var world = new World();
            var commandRecorder = new EntityCommandRecorder();

            var scene = new Scene("Server", world, commandRecorder);

            var networkedEntities = new NetworkedEntities(world);
            var physicsSystem = new PhysicsSystem();

            var editors = new List<IEditor>()
            {
                //new EditorConsole(_client.Scene),
                new SelectedEntitySystem(world),
                new EntityInspector(world),
                new EntityList(world),
                new SystemList(scene),
                new AverageMetricValue(),
                new MetricGraph()
            };

            _editorMenu.AddEditorSet("Server", editors);
            scene.AddSystems(editors);

            var parallelRunner = new DefaultParallelRunner(4);

            var serverSystem = new ServerSystem(world, _messageTargetMap);

            serverSystem.AddListener(new TransformMessageApplier(networkedEntities));
            serverSystem.AddListener(new InputForceApplier(physicsSystem, world));
            serverSystem.AddListener(new SimpleCameraMover(physicsSystem, networkedEntities));
            serverSystem.AddListener(new VoxelEditReceiver(physicsSystem));
            serverSystem.AddListener(new VoxelSpaceLoadReciever(world));

            scene.AddSystem(serverSystem);

            var voxelTypes = LoadVoxelTypes();

            var worldVoxelSpace = world.CreateEntity();
            worldVoxelSpace.Set(new NetworkedEntity() { Id = Guid.NewGuid() });
            worldVoxelSpace.Set(new Transform(worldVoxelSpace));
            worldVoxelSpace.Set(new VoxelSpace(32, 1, worldVoxelSpace));
            worldVoxelSpace.Set(new EntityMetaData() { Name = "Voxel Space" });

            scene.AddSystem(new WorldSpaceLoader((e) => { }, world, worldVoxelSpace, 8, 3, 32));
            scene.AddSystem(new ChunkGeneratorSystem(commandRecorder, parallelRunner, new ChunkGenerator(), world));

            scene.AddSystem(new VoxelSpaceExpanderSystem((e) => { }, world));

            scene.AddSystem(new PhysicsBlockFinder(world, parallelRunner));

            scene.AddSystem(new VoxelSpaceChangePropogator(world));
            scene.AddSystem(new VoxelStaticBodyGenerator(physicsSystem, world));
            scene.AddSystem(new VoxelSpaceDynamicBodyGenerator(physicsSystem, world));
            scene.AddSystem(physicsSystem);
            scene.AddSystem(new DynamicBodyPositionSync(world));

            scene.AddSystem(new CharacterInputSystem(physicsSystem, world));

            scene.AddSystem(new EntityRemovalSync(world));
            scene.AddSystem(new ClientEntityAssignmentSystem());
            scene.AddSystem(new TransformChangeServerSystem(world));
            scene.AddSystem(new VoxelSpaceAddedServerSystem(world));
            scene.AddSystem(new VoxelGridExistenceServerSystem(world));
            scene.AddSystem(new VoxelGridChangeServerSystem(world));
            scene.AddSystem(new ComponentSyncServerSystem<EntityMetaData>(world));

            scene.AddSystem(new FlagClearingSystem<NeighbourMemberChanged>(world));

            _server.SetScene(scene);
        }

        private static void _client_Started()
        {
            var world = new World();
            var commandRecorder = new EntityCommandRecorder();

            var scene = new Scene("Client", world, commandRecorder);

            var factory = _client.GraphicsDevice.ResourceFactory;

            var materialInputLayouts = new MaterialInputLayouts();

            var textureLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("SurfaceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SurfaceSampler", ResourceKind.Sampler, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("TextureColour", ResourceKind.UniformBuffer, ShaderStages.Fragment)));

            materialInputLayouts.ResourceLayouts["Texture"] = textureLayout;

            materialInputLayouts.ResourceLayouts["WorldTransform"] = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("WorldBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            materialInputLayouts.ResourceLayouts["SceneInputs"] = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ProjectionBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ViewBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("SceneLighting", ResourceKind.UniformBuffer, ShaderStages.Fragment)));

            materialInputLayouts.ResourceLayouts["CameraInputs"] = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("CameraInfo", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            materialInputLayouts.VertexLayouts["Model"] = new VertexLayoutDescription(
                    new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float3),
                    new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                    new VertexElementDescription("Normal", VertexElementSemantic.Normal, VertexElementFormat.Float3));

            materialInputLayouts.VertexLayouts["Lighting"] = new VertexLayoutDescription(
                    new VertexElementDescription("Light", VertexElementSemantic.Color, VertexElementFormat.Float1));

            var mesh3dMaterial = new Material(_client.GraphicsDevice, _client.MainSceneFramebuffer, _client.Resources.LoadText("Shaders\\Mesh.vs"), _client.Resources.LoadText("Shaders\\Mesh.fg"),
                new string[] { "Model" }, new string[] { "SceneInputs", "WorldTransform", "Texture" }, materialInputLayouts);

            var lightMeshMaterial = new Material(_client.GraphicsDevice, _client.MainSceneFramebuffer, _client.Resources.LoadText("Shaders\\LightMesh.vs"), _client.Resources.LoadText("Shaders\\LightMesh.fg"),
                new string[] { "Model", "Lighting" }, new string[] { "SceneInputs", "WorldTransform", "Texture", "CameraInputs" }, materialInputLayouts);

            var voxelTexturesResource = _client.Resources.LoadImage("Textures\\spritesheet_tiles.png");
            var voxelTexture = new MaterialTexture(_client.GraphicsDevice, textureLayout, voxelTexturesResource, RgbaFloat.White);
            var redVoxelTexture = new MaterialTexture(_client.GraphicsDevice, textureLayout, voxelTexturesResource, RgbaFloat.Red);
            var semiTransVoxelColour = new MaterialTexture(_client.GraphicsDevice, textureLayout, voxelTexturesResource, new RgbaFloat(1.0f, 1.0f, 1.0f, 0.8f));

            Action<Entity> setVoxelRender = (Entity e) =>
            {
                e.Set(new LightVertexResources());
                e.Set(lightMeshMaterial);
                e.Set(voxelTexture);
            };

            var networkedEntities = new NetworkedEntities(world);

            var clientSystem = new ClientSystem(world, _messageTargetMap, _clientMessagingChannel);

            clientSystem.AddListener(new TransformMessageApplier(networkedEntities));
            clientSystem.AddListener(new ClientEntityAssignmentApplier(networkedEntities));
            clientSystem.AddListener(new VoxelSpaceMessageApplier(networkedEntities));
            clientSystem.AddListener(new VoxelGridMessageApplier(setVoxelRender, networkedEntities));
            clientSystem.AddListener(new VoxelGridChangeMessageApplier(networkedEntities));
            clientSystem.AddListener(new EntityRemover(networkedEntities));
            clientSystem.AddListener(new ComponentSyncMessageApplier<EntityMetaData>(networkedEntities));

            scene.AddSystem(clientSystem);

            var parallelRunner = new DefaultParallelRunner(8);

            var px = Image.Load("Assets\\Textures\\cloudtop_rt.png");
            var nx = Image.Load("Assets\\Textures\\cloudtop_lf.png");
            var py = Image.Load("Assets\\Textures\\cloudtop_up.png");
            var ny = Image.Load("Assets\\Textures\\cloudtop_dn.png");
            var pz = Image.Load("Assets\\Textures\\cloudtop_bk.png");
            var nz = Image.Load("Assets\\Textures\\cloudtop_ft.png");
            scene.AddSystem(new SkyboxRenderer(_client.GraphicsDevice, _client.MainSceneFramebuffer, px, nx, py, ny, pz, nz));

            scene.AddSystem(new MeshGeometryRenderer(_client.GraphicsDevice, materialInputLayouts, world));
            scene.AddSystem(new LightMeshGeometryRenderer(_client.GraphicsDevice, materialInputLayouts, world));

            var voxelTypes = LoadVoxelTypes();

            var editors = new List<IEditor>()
            {
                new SelectedEntitySystem(world),
                new EntityInspector(world),
                new EntityList(world),
                new SystemList(scene),
                new VoxelEditor(_clientMessagingChannel, world, voxelTypes.Select((t, i) => (t.Name, new Voxel() { Exists = true, BlockType = (ushort)i })).ToArray()),
                new VoxelSpaceLoader(_clientMessagingChannel, world)
            };

            _editorMenu.AddEditorSet("Client", editors);

            scene.AddSystem(_editorMenu);
            scene.AddSystems(editors);

            scene.AddSystem(new SunLightPropogationSystem(world, new VoxelTypes(voxelTypes)));

            scene.AddSystem(new VoxelGridMesher(commandRecorder, world, new VoxelTypes(voxelTypes), _client.GraphicsDevice, parallelRunner));

            scene.AddSystem(new MeshGeometryCleaner(world));
            scene.AddSystem(new LightVertexCleaner(world));

            scene.AddSystem(new TransformLerper(networkedEntities, world));

            scene.AddSystem(new FlagClearingSystem<NeighbourMemberChanged>(world));

            scene.AddSystem(new InputForceApplierInputSystem(_clientMessagingChannel, world));
            scene.AddSystem(new SimpleCameraMoverInputSystem(_clientMessagingChannel, world));

            world.SubscribeEntityDisposed((in Entity e) =>
            {
                if(e.Has<VoxelGrid>())
                {
                    var voxelGrid = e.Get<VoxelGrid>();
                    voxelGrid.VoxelSpace.Remove(voxelGrid.MemberIndex);
                }
            });

            _client.SetScene(scene);
        }

        private static string[] goodVoxelTypes = new string[]
        {
            "wood",       // 0
            "brick grey", // 1
            "brick red",  // 2
            "dirt",       // 3
            "ice",        // 4
            "stone",      // 5
            "sand",       // 6
            "cactus top", // 7
            "glass",      // 8
            "greysand"    // 9
        };

        private static void AddCylinder(Entity entity, GraphicsDevice device, Material material, MaterialTexture materialTexture)
        {
            entity.Set(new Transform(entity));
            entity.Set(material);
            entity.Set(materialTexture);

            var cylinder = PrimitiveMeshGenerator.GenerateCapsule(1, 0.3f);

            var mesh = new RenderableMeshGeometry()
            {
                Vertices = new ResizableBuffer<VertexPositionTextureNormal>(device, VertexPositionTextureNormal.SizeInBytes, BufferUsage.VertexBuffer,
                    cylinder
                        .Select(v => new VertexPositionTextureNormal(v.Vertex, new Vector2(20, 20), v.Normal))
                        .ToArray()),
                Indices = new ResizableBuffer<ushort>(device, sizeof(ushort), BufferUsage.IndexBuffer,
                    cylinder.Select((v, i) => (ushort)i).ToArray()),
                TransparentIndices = new ResizableBuffer<ushort>(device, sizeof(ushort), BufferUsage.IndexBuffer),
                BoundingRadius = 0
            };

            entity.Set(mesh);
        }

        private static VoxelType[] LoadVoxelTypes()
        {
            var filename = "Assets\\Textures\\spritesheet_tiles.xml";
            var currentDirectory = Directory.GetCurrentDirectory();
            var filePath = Path.Combine(currentDirectory, filename);
            var doc = XElement.Load(filePath);
            var byName = doc
                .Descendants("SubTexture")
                .Select(st => (st.Attribute("name").Value, st.Attribute("transparent") != null, new Vector2(int.Parse(st.Attribute("x").Value) + 1, int.Parse(st.Attribute("y").Value) + 1)))
                .Select(t => new VoxelType(
                    t.Item1.Substring(0, t.Item1.Length - 4).Replace('_', ' '),
                    t.Item2,
                    t.Item3,
                    t.Item3,
                    t.Item3
                ))
                .ToDictionary(v => v.Name);

            return goodVoxelTypes
                .Select(n => byName[n])
                .ToArray();
        }

        private static void Message<T>()
        {
            _byNum[_messageId] = typeof(T);
            _byType[typeof(T)] = _messageId;
            _messageId++;
        }
    }

    
}
