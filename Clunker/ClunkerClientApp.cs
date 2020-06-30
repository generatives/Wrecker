using Clunker.Graphics;
using Clunker.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using System.Linq;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Clunker.Resources;
using DefaultEcs;
using Clunker.Core;
using ImGuiNET;
using Clunker.Networking;
using Ruffles.Configuration;
using Ruffles.Core;
using Ruffles.Connections;
using DefaultEcs.System;
using System.Net;

namespace Clunker
{
    public class ClunkerClientApp
    {
        public event Action Started;
        public event Action Tick;

        private Sdl2Window _window;
        private bool _windowResized;
        public GraphicsDevice GraphicsDevice { get; private set; }
        public CommandList CommandList { get; private set; }
        private Texture _mainSceneColourTexture;
        private Texture _mainSceneDepthTexture;
        public Framebuffer MainSceneFramebuffer { get; private set; }

        private EntitySet _cameras;
        public Entity CameraEntity => _cameras.GetEntities().ToArray().FirstOrDefault();
        public Transform CameraTransform => CameraEntity.World == Scene.World ? CameraEntity.Get<Transform>() : null;
        public Scene Scene { get; private set; }

        private Matrix4x4 _projectionMatrix;


        public List<ISystem<ClientSystemUpdate>> ClientSystems { get; private set; }
        public List<object> MessageListeners { get; private set; }

        private SocketConfig _clientConfig = new SocketConfig()
        {
            ChallengeDifficulty = 20, // Difficulty 20 is fairly hard
            DualListenPort = 0, // Port 0 means we get a port by the operating system
            SimulatorConfig = new Ruffles.Simulation.SimulatorConfig()
            {
                DropPercentage = 0.05f,
                MaxLatency = 10,
                MinLatency = 0
            },
            UseSimulator = false
        };

        private RuffleSocket _client;
        private Connection _server;
        private ulong _messagesSent;
        private Stopwatch _messageTimer;

        private Dictionary<int, Action<MemoryStream, World>> _messageRecievers;
        private Dictionary<Type, Action<object, MemoryStream>> _messageSerializer;
        public ResourceLoader Resources { get; private set; }

        public ClunkerClientApp(ResourceLoader resourceLoader, Scene initialScene,
            Dictionary<int, Action<MemoryStream, World>> recievers, Dictionary<Type, Action<object, MemoryStream>> serializers)
        {
            Resources = resourceLoader;
            Scene = initialScene;

            ClientSystems = new List<ISystem<ClientSystemUpdate>>();
            MessageListeners = new List<object>();

            _client = new RuffleSocket(_clientConfig);
            _messageTimer = new Stopwatch();

            _messageRecievers = recievers;
            _messageSerializer = serializers;

            _cameras = Scene.World.GetEntities().With<Camera>().With<Transform>().AsSet();
        }

        protected virtual void Initialize()
        {

        }

        public Task Start(WindowCreateInfo wci, GraphicsDeviceOptions graphicsDeviceOptions)
        {
            return Task.Factory.StartNew(() =>
            {
                _window = VeldridStartup.CreateWindow(ref wci);
                //Window.CursorVisible = false;
                //Window.SetMousePosition(Window.Width / 2, Window.Height / 2);
                _window.Resized += () => _windowResized = true;
                GraphicsDevice = VeldridStartup.CreateGraphicsDevice(_window, graphicsDeviceOptions, GraphicsBackend.Vulkan);
                CommandList = GraphicsDevice.ResourceFactory.CreateCommandList();

                CreateFramebuffer();

                Initialize();

                var imGuiRenderer = new ImGuiRenderer(GraphicsDevice, MainSceneFramebuffer.OutputDescription, _window.Width, _window.Height);

                _windowResized = true;

                Started?.Invoke();

                _client.Start();
                _client.Connect(new IPEndPoint(IPAddress.Loopback, 5674));

                var frameWatch = Stopwatch.StartNew();

                while (_window.Exists)
                {
                    var inputSnapshot = _window.PumpEvents();

                    if (!_window.Exists) break;

                    InputTracker.UpdateFrameInput(_window, inputSnapshot);

                    if (InputTracker.WasKeyDowned(Key.Tilde)) InputTracker.LockMouse = !InputTracker.LockMouse;
                    //if (InputTracker.WasKeyDowned(Key.T)) { StackedTiming.Enabled = !StackedTiming.Enabled; Timing.Enabled = !Timing.Enabled; }

                    if (_windowResized)
                    {
                        _windowResized = false;
                        GraphicsDevice.ResizeMainWindow((uint)_window.Width, (uint)_window.Height);

                        _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
                            0.90f,
                            (float)_window.Width / _window.Height,
                            0.1f,
                            256f);
                        if (GraphicsDevice.IsClipSpaceYInverted)
                        {
                            _projectionMatrix *= Matrix4x4.CreateScale(1, -1, 1);
                        }

                        imGuiRenderer.WindowResized(_window.Width, _window.Height);
                    }

                    var frameTime = frameWatch.Elapsed.TotalSeconds;
                    frameTime = Math.Min(frameTime, 0.033333);
                    frameWatch.Restart();

                    NetworkEvent networkEvent = _client.Poll();
                    while (networkEvent.Type != NetworkEventType.Nothing)
                    {
                        switch (networkEvent.Type)
                        {
                            case NetworkEventType.Connect:
                                _server = networkEvent.Connection;
                                break;
                            case NetworkEventType.Data:
                                var diff = _messageTimer.Elapsed.TotalMilliseconds;
                                //InfoViewer.Values["Message Time"] = Math.Round(diff, 4).ToString();
                                _messageTimer.Restart();
                                MessageRecieved(networkEvent.Data);
                                break;
                        }
                        networkEvent.Recycle();
                        networkEvent = _client.Poll();
                    }

                    imGuiRenderer.Update((float)frameTime, InputTracker.LockMouse ? new EmptyInputSnapshot() : InputTracker.FrameSnapshot );

                    Scene.Update(frameTime);

                    Tick?.Invoke();

                    var serverMessages = new List<object>();

                    var clientUpdate = new ClientSystemUpdate()
                    {
                        Messages = serverMessages,
                    };

                    foreach (var system in ClientSystems)
                    {
                        system.Update(clientUpdate);
                    }

                    if (_server != null)
                    {
                        SerializeMessages(serverMessages, (messages) =>
                        {
                            _server.Send(messages, 5, true, _messagesSent++);
                        });
                    }

                    CommandList.Begin();
                    CommandList.SetFramebuffer(MainSceneFramebuffer);
                    CommandList.ClearColorTarget(0, RgbaFloat.CornflowerBlue);
                    CommandList.ClearDepthStencil(1f);

                    var context = new RenderingContext()
                    {
                        GraphicsDevice = GraphicsDevice,
                        CommandList = CommandList,
                        CameraTransform = CameraTransform,
                        ProjectionMatrix = _projectionMatrix
                    };
                    Scene.Render(context);

                    var displaySize = ImGui.GetIO().DisplaySize;
                    ImGui.GetBackgroundDrawList().AddCircleFilled(displaySize / 2, 2, ImGui.GetColorU32(new Vector4(1, 1, 1, 1)));

                    imGuiRenderer.Render(GraphicsDevice, CommandList);

                    if (_mainSceneColourTexture != null && _mainSceneColourTexture.SampleCount != TextureSampleCount.Count1)
                    {
                        CommandList.ResolveTexture(_mainSceneColourTexture, GraphicsDevice.MainSwapchain.Framebuffer.ColorTargets.First().Target);
                    }

                    CommandList.End();
                    GraphicsDevice.SubmitCommands(CommandList);
                    GraphicsDevice.SwapBuffers(GraphicsDevice.MainSwapchain);
                    GraphicsDevice.WaitForIdle();

                    Resources.Update();
                }
            }, TaskCreationOptions.LongRunning);
        }

        private void CreateFramebuffer()
        {
            var factory = GraphicsDevice.ResourceFactory;

            //GraphicsDevice.GetPixelFormatSupport(
            //    PixelFormat.R16_G16_B16_A16_Float,
            //    TextureType.Texture2D,
            //    TextureUsage.RenderTarget,
            //    out PixelFormatProperties properties);

            //TextureSampleCount sampleCount = TextureSampleCount.Count2;
            //while (!properties.IsSampleCountSupported(sampleCount))
            //{
            //    sampleCount = sampleCount - 1;
            //}

            //TextureDescription mainColorDesc = TextureDescription.Texture2D(
            //    GraphicsDevice.SwapchainFramebuffer.Width,
            //    GraphicsDevice.SwapchainFramebuffer.Height,
            //    1,
            //    1,
            //    PixelFormat.R16_G16_B16_A16_Float,
            //    TextureUsage.RenderTarget | TextureUsage.Sampled,
            //    sampleCount);

            //_mainSceneColourTexture = factory.CreateTexture(ref mainColorDesc);
            //_mainSceneDepthTexture = factory.CreateTexture(TextureDescription.Texture2D(
            //    GraphicsDevice.SwapchainFramebuffer.Width,
            //    GraphicsDevice.SwapchainFramebuffer.Height,
            //    1,
            //    1,
            //    PixelFormat.R32_Float,
            //    TextureUsage.DepthStencil,
            //    sampleCount));
            //MainSceneFramebuffer = factory.CreateFramebuffer(new FramebufferDescription(_mainSceneDepthTexture, _mainSceneColourTexture));

            MainSceneFramebuffer = GraphicsDevice.SwapchainFramebuffer;
        }

        public void SerializeMessages(List<object> messages, Action<ArraySegment<byte>> send)
        {
            using (var stream = new MemoryStream())
            {
                byte[] lengthBytes = BitConverter.GetBytes(messages.Count);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lengthBytes);
                stream.Write(lengthBytes, 0, 4);

                foreach (var message in messages)
                {
                    _messageSerializer[message.GetType()](message, stream);
                }

                var segment = new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Position);

                send(segment);
            }
        }

        public void MessageRecieved(ArraySegment<byte> message)
        {
            using (var stream = new MemoryStream(message.Array, message.Offset, message.Count))
            {
                var lengthBytes = new byte[4];
                stream.Read(lengthBytes, 0, 4);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lengthBytes);
                var length = BitConverter.ToInt32(lengthBytes, 0);

                for (int i = 0; i < length; i++)
                {
                    var messageType = stream.ReadByte();
                    _messageRecievers[messageType](stream, Scene.World);
                }
            }
        }
    }
}
