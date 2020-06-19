using Clunker;
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

            var resourceLoader = new ResourceLoader();
            var voxelTexturesResource = resourceLoader.LoadImage("Assets\\spritesheet_tiles.png");

            var mesh3dMaterial = new Material(Mesh3d.VertexCode, Mesh3d.FragmentCode, false);
            var voxelMaterialInstance = new MaterialInstance(mesh3dMaterial, voxelTexturesResource, new ObjectProperties() { Colour = RgbaFloat.White });
            var transparentVoxelMaterialInstance = new MaterialInstance(mesh3dMaterial, voxelTexturesResource, new ObjectProperties() { Colour = new RgbaFloat(1, 1, 1, 0.85f) });
            var redVoxelMaterialInstance = new MaterialInstance(mesh3dMaterial, voxelTexturesResource, new ObjectProperties() { Colour = RgbaFloat.Red });

            var scene = new Scene();
            var parrallelRunner = new DefaultParallelRunner(8);

            var camera = scene.World.CreateEntity();
            var cameraTransform = new Transform();
            cameraTransform.Position = new Vector3(0, 40, 0);
            camera.Set(cameraTransform);
            camera.Set(new Camera());

            var worldVoxelSpace = scene.World.CreateEntity();
            worldVoxelSpace.Set(new Transform());
            worldVoxelSpace.Set(new VoxelSpace()
            {
                GridSize = 32,
                VoxelSize = 1,
                Members = new Dictionary<Vector3i, Entity>()
            });

            scene.RendererSystems.Add(new MeshGeometryInitializer(scene.World));

            var px = Image.Load("Assets\\skybox_px.png");
            var nx = Image.Load("Assets\\skybox_nx.png");
            var py = Image.Load("Assets\\skybox_py.png");
            var ny = Image.Load("Assets\\skybox_ny.png");
            var pz = Image.Load("Assets\\skybox_pz.png");
            var nz = Image.Load("Assets\\skybox_nz.png");
            //scene.RendererSystems.Add(new SkyboxRenderer(px, nx, py, ny, pz, nz));

            scene.RendererSystems.Add(new MeshGeometryRenderer(cameraTransform, scene.World));

            var physicsSystem = new PhysicsSystem();

            scene.LogicSystems.Add(new SimpleCameraMover(physicsSystem, scene.World));
            scene.LogicSystems.Add(new WorldSpaceLoader(scene.World, cameraTransform, worldVoxelSpace, 5, 32));
            scene.LogicSystems.Add(new ChunkGeneratorSystem(scene, parrallelRunner, new ChunkGenerator(voxelMaterialInstance)));

            scene.LogicSystems.Add(new InputForceApplier(physicsSystem, scene.World));

            scene.LogicSystems.Add(new PhysicsBlockFinder(scene.World, parrallelRunner));
            scene.LogicSystems.Add(new VoxelSpaceChangePropogator(scene.World));
            scene.LogicSystems.Add(new VoxelStaticBodyGenerator(physicsSystem, scene.World));
            scene.LogicSystems.Add(new VoxelSpaceDynamicBodyGenerator(physicsSystem, scene.World));
            scene.LogicSystems.Add(new VoxelSpaceExpanderSystem(voxelMaterialInstance, scene.World));

            scene.LogicSystems.Add(physicsSystem);
            scene.LogicSystems.Add(new DynamicBodyPositionSync(scene.World));

            var voxelTypes = LoadVoxelTypes();

            scene.LogicSystems.Add(new VoxelGridMesher(scene, new VoxelTypes(voxelTypes), parrallelRunner));

            scene.LogicSystems.Add(new CharacterInputSystem(physicsSystem, scene.World));

            var tools = new List<ITool>()
            {
                new RemoveVoxelEditingTool(redVoxelMaterialInstance, scene.World, physicsSystem, camera)
            };
            tools.AddRange(voxelTypes.Select((type, i) => new BasicVoxelAddingTool(type.Name, (ushort)i, transparentVoxelMaterialInstance, scene.World, physicsSystem, camera)));

            scene.LogicSystems.Add(new EditorMenu(scene, new List<IEditor>()
            {
                new EditorConsole(scene),
                new Toolbar(tools.ToArray()),
                new SelectedEntitySystem(scene.World),
                new PhysicsEntitySelector(scene.World, physicsSystem, cameraTransform),
                new Inspector(scene.World),
                new EntityList(scene.World),
                new VoxelSpaceLoader(scene.World, cameraTransform, voxelMaterialInstance)
            }));

            //var cylinder = scene.World.CreateEntity();
            //AddCylinder(cylinder, voxelMaterialInstance);

            var app = new ClunkerApp(resourceLoader, scene);

            app.Start(wci, options).Wait();
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

        private static void AddCylinder(Entity entity, MaterialInstance materialInstance)
        {
            entity.Set(new Transform());
            entity.Set(materialInstance);

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
