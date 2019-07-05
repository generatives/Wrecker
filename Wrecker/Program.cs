using Clunker;
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
            camera.GetComponent<Transform>().Position = new Vector3(50, 0, 50);
            scene.AddGameObject(camera);

            var types = new VoxelTypes(new[]
            {
                new VoxelType(new Vector2(650, 130), new Vector2(130, 1690), new Vector2(520, 0))
            });

            var voxelTextures = Image.Load("Assets\\spritesheet_tiles.png");
            var voxelMaterialInstance = new MaterialInstance(new Material(Mesh3d.VertexCode, Mesh3d.FragmentCode), voxelTextures);

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
    }
}