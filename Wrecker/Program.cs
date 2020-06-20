﻿using Clunker;
using Clunker.Core;
using Clunker.Editor;
using Clunker.Editor.EditorConsole;
using Clunker.Editor.Inspector;
using Clunker.Editor.SelectedEntity;
using Clunker.Editor.Toolbar;
using Clunker.Editor.VoxelSpaceLoader;
using Clunker.Geometry;
using Clunker.Graphics;
using Clunker.Graphics.Materials;
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

            var app = new WreckerApp(new ResourceLoader(), new Scene());

            app.Start(wci, options).Wait();
        }
    }

    public class WreckerApp : ClunkerApp
    {
        public WreckerApp(ResourceLoader resourceLoader, Scene initialScene) : base(resourceLoader, initialScene)
        {
        }

        protected override void Initialize()
        {
            var factory = GraphicsDevice.ResourceFactory;

            var materialInputLayouts = new MaterialInputLayouts();

            var textureLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("SurfaceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SurfaceSampler", ResourceKind.Sampler, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("TextureColour", ResourceKind.UniformBuffer, ShaderStages.Fragment)));

            var worldTransformLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("WorldBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            var sceneInputsLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ProjectionBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ViewBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("SceneLighting", ResourceKind.UniformBuffer, ShaderStages.Fragment)));

            materialInputLayouts.ResourceLayouts["Texture"] = textureLayout;
            materialInputLayouts.ResourceLayouts["WorldTransform"] = worldTransformLayout;
            materialInputLayouts.ResourceLayouts["SceneInputs"] = sceneInputsLayout;

            materialInputLayouts.VertexLayouts["Model"] = new VertexLayoutDescription(
                    new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float3),
                    new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                    new VertexElementDescription("Normal", VertexElementSemantic.Normal, VertexElementFormat.Float3));

            materialInputLayouts.VertexLayouts["Lighting"] = new VertexLayoutDescription(
                    new VertexElementDescription("Light", VertexElementSemantic.Color, VertexElementFormat.Float1));

            var mesh3dMaterial = Mesh3dMaterial.Build(GraphicsDevice, materialInputLayouts);
            var lightMeshMaterial = LightMeshMaterial.Build(GraphicsDevice, materialInputLayouts);

            var voxelTexturesResource = Resources.LoadImage("Assets\\spritesheet_tiles.png");
            var voxelTexture = new MaterialTexture(GraphicsDevice, textureLayout, voxelTexturesResource, RgbaFloat.White);
            var redVoxelTexture = new MaterialTexture(GraphicsDevice, textureLayout, voxelTexturesResource, RgbaFloat.Red);
            var semiTransVoxelColour = new MaterialTexture(GraphicsDevice, textureLayout, voxelTexturesResource, new RgbaFloat(1.0f, 1.0f, 1.0f, 0.8f));

            Action<Entity> setVoxelRender = (Entity e) =>
            {
                e.Set(lightMeshMaterial);
                e.Set(voxelTexture);
            };

            var parrallelRunner = new DefaultParallelRunner(8);

            var camera = Scene.World.CreateEntity();
            var cameraTransform = new Transform();
            cameraTransform.Position = new Vector3(0, 40, 0);
            camera.Set(cameraTransform);
            camera.Set(new Camera());

            var worldVoxelSpace = Scene.World.CreateEntity();
            worldVoxelSpace.Set(new Transform());
            worldVoxelSpace.Set(new VoxelSpace()
            {
                GridSize = 32,
                VoxelSize = 1,
                Members = new Dictionary<Vector3i, Entity>()
            });

            Scene.RendererSystems.Add(new MeshGeometryInitializer(Scene.World));

            //var px = Image.Load("Assets\\skybox_px.png");
            //var nx = Image.Load("Assets\\skybox_nx.png");
            //var py = Image.Load("Assets\\skybox_py.png");
            //var ny = Image.Load("Assets\\skybox_ny.png");
            //var pz = Image.Load("Assets\\skybox_pz.png");
            //var nz = Image.Load("Assets\\skybox_nz.png");
            //Scene.RendererSystems.Add(new SkyboxRenderer(px, nx, py, ny, pz, nz));

            Scene.RendererSystems.Add(new MeshGeometryRenderer(GraphicsDevice, materialInputLayouts, Scene.World));
            Scene.RendererSystems.Add(new LightMeshGeometryRenderer(GraphicsDevice, materialInputLayouts, Scene.World));

            var physicsSystem = new PhysicsSystem();

            Scene.LogicSystems.Add(new SimpleCameraMover(physicsSystem, Scene.World));
            Scene.LogicSystems.Add(new WorldSpaceLoader(setVoxelRender, Scene.World, cameraTransform, worldVoxelSpace, 5, 32));
            Scene.LogicSystems.Add(new ChunkGeneratorSystem(Scene, parrallelRunner, new ChunkGenerator()));

            Scene.LogicSystems.Add(new InputForceApplier(physicsSystem, Scene.World));

            Scene.LogicSystems.Add(new PhysicsBlockFinder(Scene.World, parrallelRunner));
            Scene.LogicSystems.Add(new VoxelSpaceChangePropogator(Scene.World));
            Scene.LogicSystems.Add(new VoxelStaticBodyGenerator(physicsSystem, Scene.World));
            Scene.LogicSystems.Add(new VoxelSpaceDynamicBodyGenerator(physicsSystem, Scene.World));
            Scene.LogicSystems.Add(new VoxelSpaceExpanderSystem(setVoxelRender, Scene.World));

            Scene.LogicSystems.Add(physicsSystem);
            Scene.LogicSystems.Add(new DynamicBodyPositionSync(Scene.World));

            var voxelTypes = LoadVoxelTypes();

            Scene.LogicSystems.Add(new VoxelGridMesher(Scene, new VoxelTypes(voxelTypes), parrallelRunner));

            Scene.LogicSystems.Add(new SunLightPropogator(new VoxelTypes(voxelTypes), Scene, parrallelRunner));
            Scene.LogicSystems.Add(new LightVertexMesher(GraphicsDevice, Scene, new VoxelTypes(voxelTypes)));

            Scene.LogicSystems.Add(new CharacterInputSystem(physicsSystem, Scene.World));

            var tools = new List<ITool>()
            {
                new RemoveVoxelEditingTool((e) => { e.Set(mesh3dMaterial); e.Set(redVoxelTexture); }, Scene.World, physicsSystem, camera)
            };
            tools.AddRange(voxelTypes.Select((type, i) => new BasicVoxelAddingTool(type.Name, (ushort)i, (e) => { e.Set(mesh3dMaterial); e.Set(semiTransVoxelColour); }, Scene.World, physicsSystem, camera)));

            Scene.LogicSystems.Add(new EditorMenu(Scene, new List<IEditor>()
            {
                new EditorConsole(Scene),
                new Toolbar(tools.ToArray()),
                new SelectedEntitySystem(Scene.World),
                new PhysicsEntitySelector(Scene.World, physicsSystem, cameraTransform),
                new Inspector(Scene.World),
                new EntityList(Scene.World),
                new VoxelSpaceLoader(Scene.World, cameraTransform, setVoxelRender)
            }));

            //var cylinder = Scene.World.CreateEntity();
            //AddCylinder(cylinder, mesh3dMaterial, voxelTexture);
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

        private static void AddCylinder(Entity entity, Material material, MaterialTexture materialTexture)
        {
            entity.Set(new Transform());
            entity.Set(material);
            entity.Set(materialTexture);

            var cylinder = PrimitiveMeshGenerator.GenerateCapsule(1, 0.3f);

            var mesh = new RenderableMeshGeometry()
            {
                Vertices = cylinder
                    .Select(v => new VertexPositionTextureNormal(v.Vertex, new Vector2(20, 20), v.Normal))
                    .ToArray(),
                Indices = cylinder.Select((v, i) => (ushort)i).ToArray(),
                TransparentIndices = new ushort[0],
                BoundingSize = null
            };

            entity.Set(mesh);
        }

        private static VoxelType[] LoadVoxelTypes()
        {
            var filename = "Assets\\spritesheet_tiles.xml";
            var currentDirectory = Directory.GetCurrentDirectory();
            var filePath = Path.Combine(currentDirectory, filename);
            var doc = XElement.Load(filePath);
            var byName = doc
                .Descendants("SubTexture")
                .Select(st => (st.Attribute("name").Value, st.Attribute("transparent") != null, new Vector2(int.Parse(st.Attribute("x").Value), int.Parse(st.Attribute("y").Value))))
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
    }
}
