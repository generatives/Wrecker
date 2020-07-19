using Clunker;
using Clunker.Core;
using Clunker.ECS;
using Clunker.Editor;
using Clunker.Editor.EditorConsole;
using Clunker.Editor.Logging.Metrics;
using Clunker.Editor.Scene;
using Clunker.Editor.SelectedEntity;
using Clunker.Editor.Toolbar;
using Clunker.Editor.VoxelEditor;
using Clunker.Editor.VoxelSpaceLoader;
using Clunker.Geometry;
using Clunker.Graphics;
using Clunker.Graphics.Materials;
using Clunker.Graphics.Systems;
using Clunker.Networking;
using Clunker.Networking.EntityExistence;
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
using MessagePack;
using MessagePack.Resolvers;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
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

        private static MessagingChannel _clientMessagingChannel;
        private static ClunkerClientApp _client;

        private static ClunkerServerApp _server;

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

            Message<TransformMessageApplier>();
            Message<InputForceApplier>();
            Message<SimpleCameraMover>();
            Message<EntityAdder>();
            Message<CameraMessageApplier>();
            Message<VoxelSpaceMessageApplier>();
            Message<VoxelGridMessageApplier>();
            Message<EntityRemover>();
            Message<VoxelEditReceiver>();

            var messageTargetMap = new MessageTargetMap(_byType, _byNum);

            _clientMessagingChannel = new MessagingChannel(messageTargetMap);
            _client = new ClunkerClientApp(new ResourceLoader(), messageTargetMap, _clientMessagingChannel);
            _client.Started += _client_Started;

            _server = new ClunkerServerApp(messageTargetMap);
            _server.Started += _server_Started;

            var serverTask = _server.Start();
            _client.Start(wci, options).Wait();
        }

        private static void _server_Started()
        {
            var world = new World();
            var rendererSystems = new List<ISystem<RenderingContext>>();
            var logicSystems = new List<ISystem<double>>();
            var commandRecorder = new EntityCommandRecorder();

            var networkedEntities = new NetworkedEntities(world);
            var physicsSystem = new PhysicsSystem();

            var parallelRunner = new DefaultParallelRunner(4);

            _server.AddListener(new TransformMessageApplier(networkedEntities));
            _server.AddListener(new InputForceApplier(physicsSystem, networkedEntities));
            _server.AddListener(new SimpleCameraMover(physicsSystem, networkedEntities));

            var voxelTypes = LoadVoxelTypes();

            var player = world.CreateEntity();
            var playerTransform = new Transform();
            playerTransform.Position = new Vector3(0, 40, 0);
            player.Set(playerTransform);
            player.Set(new Camera());
            player.Set(new NetworkedEntity() { Id = Guid.NewGuid() });

            var worldVoxelSpace = world.CreateEntity();
            worldVoxelSpace.Set(new NetworkedEntity() { Id = Guid.NewGuid() });
            worldVoxelSpace.Set(new Transform());
            worldVoxelSpace.Set(new VoxelSpace(32, 1, worldVoxelSpace));

            logicSystems.Add(new WorldSpaceLoader((e) => { }, world, worldVoxelSpace, 10, 3, 32));
            logicSystems.Add(new ChunkGeneratorSystem(commandRecorder, parallelRunner, new ChunkGenerator(), world));

            logicSystems.Add(new VoxelSpaceExpanderSystem((e) => { }, world));

            logicSystems.Add(new PhysicsBlockFinder(world, parallelRunner));

            logicSystems.Add(new VoxelSpaceChangePropogator(world));
            logicSystems.Add(new VoxelStaticBodyGenerator(physicsSystem, world));
            logicSystems.Add(new VoxelSpaceDynamicBodyGenerator(physicsSystem, world));
            logicSystems.Add(physicsSystem);
            logicSystems.Add(new DynamicBodyPositionSync(world));

            logicSystems.Add(new CharacterInputSystem(physicsSystem, world));

            logicSystems.Add(new EntityExistenceSync(world));
            logicSystems.Add(new TransformChangeServerSystem(world));
            logicSystems.Add(new VoxelSpaceAddedServerSystem(world));
            logicSystems.Add(new VoxelGridChangeServerSystem(world));
            logicSystems.Add(new CameraServerSystem(world));

            logicSystems.Add(new FlagClearingSystem<NeighbourMemberChanged>(world));

            var scene = new Scene(world, rendererSystems, logicSystems, commandRecorder);
            _server.SetScene(scene);
        }

        private static void _client_Started()
        {
            var world = new World();
            var rendererSystems = new List<ISystem<RenderingContext>>();
            var logicSystems = new List<ISystem<double>>();
            var commandRecorder = new EntityCommandRecorder();

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

            _client.AddListener(new EntityAdder(world));
            _client.AddListener(new TransformMessageApplier(networkedEntities));
            _client.AddListener(new CameraMessageApplier(networkedEntities));
            _client.AddListener(new VoxelSpaceMessageApplier(networkedEntities));
            _client.AddListener(new VoxelGridMessageApplier(setVoxelRender, networkedEntities));
            _client.AddListener(new EntityRemover(networkedEntities));

            var parallelRunner = new DefaultParallelRunner(8);

            var px = Image.Load("Assets\\Textures\\cloudtop_rt.png");
            var nx = Image.Load("Assets\\Textures\\cloudtop_lf.png");
            var py = Image.Load("Assets\\Textures\\cloudtop_up.png");
            var ny = Image.Load("Assets\\Textures\\cloudtop_dn.png");
            var pz = Image.Load("Assets\\Textures\\cloudtop_bk.png");
            var nz = Image.Load("Assets\\Textures\\cloudtop_ft.png");
            rendererSystems.Add(new SkyboxRenderer(_client.GraphicsDevice, _client.MainSceneFramebuffer, px, nx, py, ny, pz, nz));

            rendererSystems.Add(new MeshGeometryRenderer(_client.GraphicsDevice, materialInputLayouts, world));
            rendererSystems.Add(new LightMeshGeometryRenderer(_client.GraphicsDevice, materialInputLayouts, world));

            var voxelTypes = LoadVoxelTypes();

            //var tools = new List<ITool>()
            //{
            //    new RemoveVoxelEditingTool((e) => { e.Set(lightMeshMaterial); e.Set(redVoxelTexture); }, _client.Scene.World, physicsSystem, player)
            //};
            //tools.AddRange(voxelTypes.Select((type, i) => new BasicVoxelAddingTool(type.Name, (ushort)i, (e) => { e.Set(lightMeshMaterial); e.Set(semiTransVoxelColour); }, Scene.World, physicsSystem, player)));

            //_client.Scene.LogicSystems.Add(new EditorMenu(_client.Scene, new List<IEditor>()
            //{
            //    new EditorConsole(_client.Scene),
            //    new Toolbar(tools.ToArray()),
            //    new SelectedEntitySystem(_client.Scene.World),
            //    new PhysicsEntitySelector(_client.Scene.World, physicsSystem, playerTransform),
            //    new EntityInspector(_client.Scene.World),
            //    new EntityList(_client.Scene.World),
            //    new SystemList(_client.Scene),
            //    new VoxelSpaceLoader(_client.Scene.World, playerTransform, setVoxelRender)
            //}));

            var editorMenu = new EditorMenu(new List<IEditor>()
            {
                //new EditorConsole(_client.Scene),
                new SelectedEntitySystem(world),
                new EntityInspector(world),
                new EntityList(world),
                //new SystemList(world, _client.Scene),
                new AverageMetricValue(),
                new MetricGraph(),
                new VoxelEditor(_clientMessagingChannel, world, voxelTypes.Select((t, i) => (t.Name, new Voxel() { Exists = true, BlockType = (ushort)i })).ToArray())
            });

            logicSystems.Add(editorMenu);
            logicSystems.AddRange(editorMenu.Editors);

            logicSystems.Add(new SunLightPropogationSystem(world, new VoxelTypes(voxelTypes)));

            logicSystems.Add(new VoxelGridMesher(commandRecorder, world, new VoxelTypes(voxelTypes), _client.GraphicsDevice, parallelRunner));

            logicSystems.Add(new MeshGeometryCleaner(world));
            logicSystems.Add(new LightVertexCleaner(world));

            logicSystems.Add(new TransformLerper(networkedEntities, world));

            logicSystems.Add(new FlagClearingSystem<NeighbourMemberChanged>(world));

            logicSystems.Add(new InputForceApplierInputSystem(_clientMessagingChannel, world));
            logicSystems.Add(new SimpleCameraMoverInputSystem(_clientMessagingChannel, world));

            world.SubscribeEntityDisposed((in Entity e) =>
            {
                if(e.Has<VoxelGrid>())
                {
                    var voxelGrid = e.Get<VoxelGrid>();
                    voxelGrid.VoxelSpace.Remove(voxelGrid.MemberIndex);
                }
            });

            var scene = new Scene(world, rendererSystems, logicSystems, commandRecorder);
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
            entity.Set(new Transform());
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
