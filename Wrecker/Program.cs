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
            var transparentVoxelMaterialInstance = new MaterialInstance(mesh3dMaterial, voxelTexturesResource, new ObjectProperties() { Colour = new RgbaFloat(1, 1, 1, 0.85f) });
            var redVoxelMaterialInstance = new MaterialInstance(mesh3dMaterial, voxelTexturesResource, new ObjectProperties() { Colour = RgbaFloat.Red });

            var scene = new Scene();
            var parrallelRunner = new DefaultParallelRunner(1);

            var camera = scene.World.CreateEntity();
            var cameraTransform = new Transform();
            cameraTransform.Position = new Vector3(0, 0, 3);
            camera.Set(cameraTransform);
            camera.Set(new Camera());

            scene.RendererSystems.Add(new MeshGeometryInitializer(scene.World));

            var px = Image.Load("Assets\\skybox_px.png");
            var nx = Image.Load("Assets\\skybox_nx.png");
            var py = Image.Load("Assets\\skybox_py.png");
            var ny = Image.Load("Assets\\skybox_ny.png");
            var pz = Image.Load("Assets\\skybox_pz.png");
            var nz = Image.Load("Assets\\skybox_nz.png");
            //scene.RendererSystems.Add(new SkyboxRenderer(px, nx, py, ny, pz, nz));

            scene.RendererSystems.Add(new MeshGeometryRenderer(scene.World));

            scene.LogicSystems.Add(new SimpleCameraMover(scene.World));
            scene.LogicSystems.Add(new WorldSpaceLoader(scene.World, cameraTransform, 5, 32));
            scene.LogicSystems.Add(new ChunkGeneratorSystem(scene, parrallelRunner, new ChunkGenerator(voxelMaterialInstance, 32, 1)));

            var physicsSystem = new PhysicsSystem();

            scene.LogicSystems.Add(new InputForceApplier(physicsSystem, scene.World));

            scene.LogicSystems.Add(new ExposedVoxelFinder(scene.World));
            scene.LogicSystems.Add(new VoxelSpaceChangePropogator(scene.World));
            scene.LogicSystems.Add(new VoxelStaticBodyGenerator(physicsSystem, scene.World));
            scene.LogicSystems.Add(new VoxelSpaceDynamicBodyGenerator(physicsSystem, scene.World));
            scene.LogicSystems.Add(new VoxelSpaceExpanderSystem(voxelMaterialInstance, scene.World));

            scene.LogicSystems.Add(physicsSystem);
            scene.LogicSystems.Add(new DynamicBodyPositionSync(scene.World));

            var voxelTypes = new VoxelTypes(new[]
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
                    new Vector2(650, 1300)),
                new VoxelType(
                    "Dirt",
                    new Vector2(650, 130),
                    new Vector2(650, 130),
                    new Vector2(650, 130)),
                new VoxelType(
                    "Grass",
                    new Vector2(389, 909),
                    new Vector2(389, 909),
                    new Vector2(389, 909))
            });

            scene.LogicSystems.Add(new VoxelGridMesher(scene, voxelTypes, parrallelRunner));

            scene.LogicSystems.Add(new EditorMenu(scene, new List<IEditor>()
            {
                new EditorConsole(scene),
                new Toolbar(new ITool[]
                {
                    new RemoveVoxelEditingTool(redVoxelMaterialInstance, scene.World, physicsSystem, camera),
                    new BasicVoxelAddingTool("DarkStone", 0, transparentVoxelMaterialInstance, scene.World, physicsSystem, camera),
                    new BasicVoxelAddingTool("Metal", 1, transparentVoxelMaterialInstance, scene.World, physicsSystem, camera),
                    new BasicVoxelAddingTool("Dirt", 3, transparentVoxelMaterialInstance, scene.World, physicsSystem, camera),
                    new BasicVoxelAddingTool("Grass", 4, transparentVoxelMaterialInstance, scene.World, physicsSystem, camera),
                }),
                new SelectedEntitySystem(scene.World),
                new PhysicsEntitySelector(scene.World, physicsSystem, cameraTransform),
                new Inspector(scene.World),
                new EntityList(scene.World),
                new VoxelSpaceLoader(scene.World, cameraTransform, voxelMaterialInstance)
            }));

            var app = new ClunkerApp(resourceLoader, scene);

            app.Start(wci, options).Wait();
        }
    }
}
