using Clunker;
using Clunker.Core;
using Clunker.ECS;
using Clunker.Editor;
using Clunker.Editor.EditorConsole;
using Clunker.Editor.Logging.Metrics;
using Clunker.Editor.Scene;
using Clunker.Editor.SelectedEntity;
using Clunker.Editor.Toolbar;
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
        private static Dictionary<int, Action<Stream, World>> _recievers = new Dictionary<int, Action<Stream, World>>();
        private static Dictionary<Type, Action<object, Stream>> _serializers = new Dictionary<Type, Action<object, Stream>>();
        private static MessagePackSerializerOptions _serializerOptions;

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

            var resolver = CompositeResolver.Create(CustomResolver.Instance, StandardResolver.Instance);
            _serializerOptions = MessagePackSerializerOptions.Standard.WithResolver(resolver);

            Message<EntityMessage<EntityAdded>>();
            Message<EntityMessage<EntityRemoved>>();
            Message<EntityMessage<TransformMessage>>();
            Message<EntityMessage<SimpleCameraMoverMessage>>();
            Message<EntityMessage<InputForceApplierMessage>>();
            Message<EntityMessage<CameraMessage>>();
            Message<EntityMessage<VoxelSpaceMessage>>();
            Message<EntityMessage<VoxelGridMessage>>();

            var clientSystems = new List<ISystem<ClientSystemUpdate>>();

            _client = new ClunkerClientApp(new ResourceLoader(), new Scene(), _recievers, _serializers);
            _client.Started += _client_Started;

            _server = new ClunkerServerApp(new Scene(), _recievers, _serializers);
            _server.Started += _server_Started;

            var serverTask = _server.Start();
            _client.Start(wci, options).Wait();
        }

        private static void _server_Started()
        {
            var networkedEntities = new NetworkedEntities(_server.Scene.World);
            var physicsSystem = new PhysicsSystem();

            var parallelRunner = new DefaultParallelRunner(4);

            _server.ServerSystems.Add(new EntityExistenceSender(_server.Scene.World));
            _server.ServerSystems.Add(new TransformInitServerSystem(_server.Scene.World));
            _server.ServerSystems.Add(new TransformChangeServerSystem(_server.Scene.World));
            _server.ServerSystems.Add(new VoxelSpaceInitServerSystem(_server.Scene.World));
            _server.ServerSystems.Add(new VoxelSpaceAddedServerSystem(_server.Scene.World));
            _server.ServerSystems.Add(new VoxelGridInitServerSystem(_server.Scene.World));
            _server.ServerSystems.Add(new VoxelGridChangeServerSystem(_server.Scene.World));
            _server.ServerSystems.Add(new CameraServerSystem(_server.Scene.World));

            _server.MessageListeners.Add(new TransformMessageApplier(networkedEntities));
            _server.MessageListeners.Add(new InputForceApplier(physicsSystem, networkedEntities));
            _server.MessageListeners.Add(new SimpleCameraMover(physicsSystem, networkedEntities));

            var voxelTypes = LoadVoxelTypes();

            var player = _server.Scene.World.CreateEntity();
            var playerTransform = new Transform();
            playerTransform.Position = new Vector3(0, 40, 0);
            player.Set(playerTransform);
            player.Set(new Camera());
            player.Set(new NetworkedEntity() { Id = Guid.NewGuid() });

            var worldVoxelSpace = _server.Scene.World.CreateEntity();
            worldVoxelSpace.Set(new NetworkedEntity() { Id = Guid.NewGuid() });
            worldVoxelSpace.Set(new Transform());
            worldVoxelSpace.Set(new VoxelSpace(32, 1, worldVoxelSpace));

            _server.Scene.LogicSystems.Add(new WorldSpaceLoader((e) => { }, _server.Scene.World, worldVoxelSpace, 2, 2, 32));
            _server.Scene.LogicSystems.Add(new ChunkGeneratorSystem(_server.Scene, parallelRunner, new ChunkGenerator()));

            _server.Scene.LogicSystems.Add(new VoxelSpaceExpanderSystem((e) => { }, _server.Scene.World));

            _server.Scene.LogicSystems.Add(new PhysicsBlockFinder(_server.Scene.World, parallelRunner));

            _server.Scene.LogicSystems.Add(/*new ParallelSystem<double>(parallelRunner,*/
                new SequentialSystem<double>(
                    new VoxelSpaceChangePropogator(_server.Scene.World),
                    new VoxelStaticBodyGenerator(physicsSystem, _server.Scene.World),
                    new VoxelSpaceDynamicBodyGenerator(physicsSystem, _server.Scene.World),
                    physicsSystem,
                    new DynamicBodyPositionSync(_server.Scene.World)
                /*new SunLightPropogationSystem(new VoxelTypes(voxelTypes), _server.Scene)*/));

            _server.Scene.LogicSystems.Add(new CharacterInputSystem(physicsSystem, _server.Scene.World));

            _server.Scene.LogicSystems.Add(new FlagClearingSystem<NeighbourMemberChanged>(_server.Scene.World));
        }

        private static void _client_Started()
        {
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

            var networkedEntities = new NetworkedEntities(_client.Scene.World);

            _client.MessageListeners.Add(new EntityAdder(_client.Scene.World));
            _client.MessageListeners.Add(new TransformMessageApplier(networkedEntities));
            _client.MessageListeners.Add(new CameraMessageApplier(networkedEntities));
            _client.MessageListeners.Add(new VoxelSpaceMessageApplier(networkedEntities));
            _client.MessageListeners.Add(new VoxelGridMessageApplier(setVoxelRender, networkedEntities));
            _client.MessageListeners.Add(new EntityRemover(networkedEntities));

            _client.ClientSystems.Add(new InputForceApplierInputSystem(_client.Scene.World));
            _client.ClientSystems.Add(new SimpleCameraMoverInputSystem(_client.Scene.World));

            var parallelRunner = new DefaultParallelRunner(4);

            var px = Image.Load("Assets\\Textures\\cloudtop_rt.png");
            var nx = Image.Load("Assets\\Textures\\cloudtop_lf.png");
            var py = Image.Load("Assets\\Textures\\cloudtop_up.png");
            var ny = Image.Load("Assets\\Textures\\cloudtop_dn.png");
            var pz = Image.Load("Assets\\Textures\\cloudtop_bk.png");
            var nz = Image.Load("Assets\\Textures\\cloudtop_ft.png");
            _client.Scene.RendererSystems.Add(new SkyboxRenderer(_client.GraphicsDevice, _client.MainSceneFramebuffer, px, nx, py, ny, pz, nz));

            _client.Scene.RendererSystems.Add(new MeshGeometryRenderer(_client.GraphicsDevice, materialInputLayouts, _client.Scene.World));
            _client.Scene.RendererSystems.Add(new LightMeshGeometryRenderer(_client.GraphicsDevice, materialInputLayouts, _client.Scene.World));

            var voxelTypes = LoadVoxelTypes();

            var worldVoxelSpace = _client.Scene.World.CreateEntity();
            worldVoxelSpace.Set(new Transform());
            worldVoxelSpace.Set(new VoxelSpace(32, 1, worldVoxelSpace));

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

            _client.Scene.LogicSystems.Add(new EditorMenu(_client.Scene, new List<IEditor>()
            {
                new EditorConsole(_client.Scene),
                new SelectedEntitySystem(_client.Scene.World),
                new EntityInspector(_client.Scene.World),
                new EntityList(_client.Scene.World),
                new SystemList(_client.Scene),
                new AverageMetricValue(),
                new MetricGraph()
            }));

            _client.Scene.LogicSystems.Add(new SunLightPropogationSystem(new VoxelTypes(voxelTypes), _client.Scene));

            _client.Scene.LogicSystems.Add(new VoxelGridMesher(_client.Scene, new VoxelTypes(voxelTypes), _client.GraphicsDevice, parallelRunner));

            _client.Scene.LogicSystems.Add(new MeshGeometryCleaner(_client.Scene.World));
            _client.Scene.LogicSystems.Add(new LightVertexCleaner(_client.Scene.World));

            _client.Scene.LogicSystems.Add(new TransformLerper(networkedEntities, _client.Scene.World));

            _client.Scene.LogicSystems.Add(new FlagClearingSystem<NeighbourMemberChanged>(_client.Scene.World));
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
            var messageId = _messageId;
            _recievers[messageId] = (Stream stream, World world) =>
            {
                var message = MessagePackSerializer.Deserialize<T>(stream, _serializerOptions);
                world.Publish(in message);
            };
            _serializers[typeof(T)] = (object message, Stream stream) =>
            {
                stream.WriteByte(messageId);
                MessagePackSerializer.Serialize(stream, (T)message, _serializerOptions);
            };
            _messageId++;
        }
    }

    
}
