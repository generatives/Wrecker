using Clunker;
using Clunker.Construct;
using Clunker.Editor;
using Clunker.Graphics;
using Clunker.Graphics.Materials;
using Clunker.Math;
using Clunker.Physics;
using Clunker.Physics.CharacterController;
using Clunker.Physics.Voxels;
using Clunker.Resources;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
using Clunker.SceneGraph.Core;
using Clunker.Tooling;
using Clunker.Voxels;
using Clunker.World;
using Hyperion;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Veldrid;
using Veldrid.StartupUtilities;

namespace Wrecker
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
            var scene = new Scene();

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

            var images = new Dictionary<string, Resource<Image<Rgba32>>>();

            var resourceLoader = new ResourceLoader();
            var voxelTexturesResource = resourceLoader.LoadImage("Assets\\spritesheet_tiles.png");

            var mesh3dMaterial = new Material(Mesh3d.VertexCode, Mesh3d.FragmentCode);
            var voxelMaterialInstance = new MaterialInstance(mesh3dMaterial, voxelTexturesResource, new ObjectProperties() { Colour = RgbaFloat.White });

            var voxelChangeMaterialInstance = new MaterialInstance(mesh3dMaterial, voxelTexturesResource, new ObjectProperties() { Colour = RgbaFloat.White });

            var tools = new Tool[]
            {
                new RemoveVoxelEditingTool() { Name = "Remove" },
                new BasicVoxelAddingTool("DarkStone", 0, types, voxelChangeMaterialInstance),
                new BasicVoxelAddingTool("Metal", 1, types, voxelChangeMaterialInstance),
                new ThrusterVoxelEditingTool(2, types, voxelChangeMaterialInstance) { Name = "Thruster" }
            };

            var px = Image.Load("Assets\\skybox_px.png");
            var nx = Image.Load("Assets\\skybox_nx.png");
            var py = Image.Load("Assets\\skybox_py.png");
            var ny = Image.Load("Assets\\skybox_ny.png");
            var pz = Image.Load("Assets\\skybox_pz.png");
            var nz = Image.Load("Assets\\skybox_nz.png");

            var camera = new GameObject("Player");
            camera.AddComponent(new Camera());
            camera.AddComponent(new Character());
            camera.AddComponent(new CharacterInput());
            camera.AddComponent(new ComponentSwitcher(tools));
            camera.AddComponent(new EditorMenu());
            camera.AddComponent(new Skybox(px, nx, py, ny, pz, nz));
            scene.AddGameObject(camera);

            var ship = CreateShip(types, voxelMaterialInstance);

            scene.AddGameObject(ship);

            var chunkSize = 32;

            var worldSpaceObj = new GameObject("World Space");
            var worldSpace = new VoxelSpace(new Vector3i(chunkSize, chunkSize, chunkSize), 1);
            worldSpaceObj.AddComponent(worldSpace);
            scene.AddGameObject(worldSpaceObj);

            var worldSystem = new WorldSystem(
                camera,
                worldSpace,
                new ChunkStorage(),
                new ChunkGenerator(types, voxelMaterialInstance, chunkSize, 1),
                2, chunkSize);

            scene.AddSystem(worldSystem);
            scene.AddSystem(new PhysicsSystem());

            var app = new ClunkerApp(resourceLoader, scene);

            app.Start(wci, options).Wait();
        }

        private static GameObject CreateShip(VoxelTypes types, MaterialInstance materialInstance)
        {
            var gridLength = 4;
            var voxelSize = 1;
            var voxelSpaceData = new VoxelGridData(gridLength, gridLength, gridLength, voxelSize);
            voxelSpaceData[0, 0, 0] = new Voxel() { Exists = true };

            var voxelSpace = new VoxelSpace(new Vector3i(gridLength, gridLength, gridLength), voxelSize);
            var spaceShip = new GameObject("Single Block");
            spaceShip.AddComponent(voxelSpace);
            spaceShip.AddComponent(new DynamicVoxelSpaceBody());
            spaceShip.AddComponent(new Construct());
            spaceShip.AddComponent(new ConstructFlightControl());
            spaceShip.AddComponent(new ConstructVoxelSpaceExpander(types, materialInstance));

            var voxelGridObj = new GameObject($"{spaceShip.Name} Voxel Grid");
            voxelGridObj.AddComponent(new VoxelGrid(voxelSpaceData, new Dictionary<Vector3i, GameObject>()));
            voxelGridObj.AddComponent(new VoxelMeshRenderable(types, materialInstance));
            //voxelGridObj.AddComponent(new VoxelGridRenderable(types, materialInstance));

            voxelSpace.Add(new Vector3i(0, 0, 0), voxelGridObj);

            return spaceShip;
        }
    }
}