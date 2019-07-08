using Clunker;
using Clunker.Construct;
using Clunker.Graphics;
using Clunker.Graphics.Materials;
using Clunker.Physics;
using Clunker.Physics.CharacterController;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentsInterfaces;
using Clunker.SceneGraph.Core;
using Clunker.Voxels;
using Clunker.World;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
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

            var camera = new GameObject();
            camera.AddComponent(new Camera());
            //camera.AddComponent(new CylinderBody());
            //camera.AddComponent(new PhysicsMovement());
            //camera.AddComponent(new FreeMovement());
            camera.AddComponent(new Character());
            camera.AddComponent(new CharacterInput());
            camera.AddComponent(new LookRayCaster());
            scene.AddGameObject(camera);

            var types = new VoxelTypes(new[]
            {
                new VoxelType(new Vector2(650, 130), new Vector2(130, 1690), new Vector2(520, 0))
            });

            var voxelTextures = Image.Load("Assets\\spritesheet_tiles.png");
            var voxelMaterialInstance = new MaterialInstance(new Material(Mesh3d.VertexCode, Mesh3d.FragmentCode), voxelTextures);

            scene.AddGameObject(CreateShip(types, voxelMaterialInstance));

            var chunkSize = 32;
            var worldSystem = new WorldSystem(
                camera,
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
            var voxelSpaceData = new VoxelSpaceData(10, 10, 10, 1);
            int gap = 3;
            for (int x = gap; x < voxelSpaceData.XLength - gap; x++)
                for (int y = gap; y < voxelSpaceData.YLength - gap; y++)
                    for (int z = gap; z < voxelSpaceData.ZLength - gap; z++)
                    {
                        //voxelSpaceData[x, y, z] = new Voxel() { Exists = x == 0 || x == voxelSpaceData.XLength - 1 || y == 0 || y == voxelSpaceData.YLength - 1 || z == 0 || z == voxelSpaceData.ZLength - 1};
                        voxelSpaceData[x, y, z] = new Voxel() { Exists = true };
                    }

            var voxelSpace = new VoxelSpace(voxelSpaceData);
            var construct = new Construct();
            var gameObject = new GameObject();
            gameObject.AddComponent(voxelSpace);
            gameObject.AddComponent(construct);
            gameObject.AddComponent(new DynamicVoxelBody());
            gameObject.AddComponent(new VoxelMesh(types, materialInstance));

            return gameObject;
        }
    }
}