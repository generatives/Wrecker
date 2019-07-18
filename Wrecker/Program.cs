using Clunker;
using Clunker.Construct;
using Clunker.Graphics;
using Clunker.Graphics.Materials;
using Clunker.Math;
using Clunker.Physics;
using Clunker.Physics.CharacterController;
using Clunker.Physics.Voxels;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentsInterfaces;
using Clunker.SceneGraph.Core;
using Clunker.Tooling;
using Clunker.Voxels;
using Clunker.World;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
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

            var tools = new Component[]
            {
                new RemoveVoxelEditingTool(),
                new BasicVoxelEditingTool("DarkStone", 0),
                new BasicVoxelEditingTool("Metal", 1),
                new ThrusterVoxelEditingTool(2)
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
            camera.AddComponent(new Skybox(px, nx, py, ny, pz, nz));
            scene.AddGameObject(camera);

            var voxelTextures = Image.Load("Assets\\spritesheet_tiles.png");
            var voxelMaterialInstance = new MaterialInstance(new Material(Mesh3d.VertexCode, Mesh3d.FragmentCode), voxelTextures, new ObjectProperties() { Colour = RgbaFloat.White });

            scene.AddGameObject(CreateShip(types, voxelMaterialInstance));

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

            var app = new ClunkerApp(scene);

            app.Start(wci, options).Wait();
        }

        private static GameObject CreateShip(VoxelTypes types, MaterialInstance materialInstance)
        {
            var gridLength = 8;
            var voxelSize = 1;
            var voxelSpaceData = new VoxelGridData(gridLength, gridLength, gridLength, voxelSize);
            int gap = 2;
            for (int x = gap; x < voxelSpaceData.XLength - gap; x++)
                for (int y = gap; y < voxelSpaceData.YLength - gap; y++)
                    for (int z = gap; z < voxelSpaceData.ZLength - gap; z++)
                    {
                        voxelSpaceData[x, y, z] = new Voxel() { Exists = true };
                    }

            var voxelSpace = new VoxelSpace(new Vector3i(gridLength, gridLength, gridLength), voxelSize);
            var spaceShip = new GameObject("Space Ship");
            spaceShip.AddComponent(voxelSpace);
            spaceShip.AddComponent(new DynamicVoxelSpaceBody());
            spaceShip.AddComponent(new Construct());
            spaceShip.AddComponent(new ConstructVoxelSpaceExpander(types, materialInstance));

            var voxelGridObj = new GameObject("Spaceship Voxel Grid");
            voxelGridObj.AddComponent(new VoxelGrid(voxelSpaceData, new Dictionary<Vector3i, GameObject>()));
            voxelGridObj.AddComponent(new VoxelMeshRenderable(types, materialInstance));
            //voxelGridObj.AddComponent(new VoxelGridRenderable(types, materialInstance));

            voxelSpace.Add(new Vector3i(0, 0, 0), voxelGridObj);

            return spaceShip;
        }
    }
}