using Clunker;
using Clunker.Core;
using Clunker.Graphics;
using Clunker.Graphics.Materials;
using Clunker.Physics;
using Clunker.Physics.Voxels;
using Clunker.Resources;
using Clunker.Voxels;
using Clunker.WorldSpace;
using DefaultEcs.Threading;
using SixLabors.ImageSharp;
using System;
using System.Numerics;
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

            var mesh3dMaterial = new Material(Mesh3d.VertexCode, Mesh3d.FragmentCode);
            var voxelMaterialInstance = new MaterialInstance(mesh3dMaterial, voxelTexturesResource, new ObjectProperties() { Colour = RgbaFloat.White });

            var scene = new Scene();
            var parrallelRunner = new DefaultParallelRunner(Environment.ProcessorCount);

            var camera = scene.World.CreateEntity();
            var transform = new Transform();
            transform.Position = new Vector3(0, 0, 3);
            camera.Set(transform);
            camera.Set(new Camera());

            scene.RendererSystems.Add(new MeshGeometryInitializer(scene.World));
            scene.RendererSystems.Add(new MeshGeometryDisposal(scene.World));

            var px = Image.Load("Assets\\skybox_px.png");
            var nx = Image.Load("Assets\\skybox_nx.png");
            var py = Image.Load("Assets\\skybox_py.png");
            var ny = Image.Load("Assets\\skybox_ny.png");
            var pz = Image.Load("Assets\\skybox_pz.png");
            var nz = Image.Load("Assets\\skybox_nz.png");
            scene.RendererSystems.Add(new SkyboxRenderer(px, nx, py, ny, pz, nz));

            scene.RendererSystems.Add(new MeshGeometryRenderer(scene.World));

            scene.LogicSystems.Add(new SimpleCameraMover(scene.World));
            scene.LogicSystems.Add(new WorldSpaceLoader(scene.World, transform, 3, 32));
            scene.LogicSystems.Add(new ChunkGeneratorSystem(scene, parrallelRunner, new ChunkGenerator(voxelMaterialInstance, 32, 1)));

            var physicsSystem = new PhysicsSystem();
            scene.LogicSystems.Add(physicsSystem);
            scene.LogicSystems.Add(new VoxelShapeGenerator(physicsSystem, scene.World));

            scene.LogicSystems.Add(new ClickVoxelRemover(physicsSystem, transform));

            var types = new VoxelTypes(new[]
            {
                new VoxelType(
                    "DarkStone",
                    new Vector2(390, 1690),
                    new Vector2(390, 1690),
                    new Vector2(390, 1690)),
                new VoxelType(
                    "Metal",
                    new Vector2(650, 1300),
                    new Vector2(650, 1300),
                    new Vector2(650, 1300)),
                new VoxelType(
                    "Thruster",
                    new Vector2(650, 1170),
                    new Vector2(650, 1300),
                    new Vector2(650, 1300))
            });

            scene.LogicSystems.Add(new VoxelGridMesher(scene, types, parrallelRunner));

            var app = new ClunkerApp(resourceLoader, scene);

            app.Start(wci, options).Wait();
        }
    }
}
