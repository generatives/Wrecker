﻿using Clunker.Graphics;
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
using DefaultEcs.System;
using System.Net;
using System.IO.Compression;
using LiteNetLib;

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

        public Dictionary<Type, IMessageReceiver> MessageListeners { get; private set; }

        private ulong _messagesSent;
        private Stopwatch _messageTimer;

        private MessageTargetMap _messageTargetMap;
        private MessagingChannel _messagingChannel;

        public ResourceLoader Resources { get; private set; }

        public ClunkerClientApp(ResourceLoader resourceLoader, MessageTargetMap messageTargetMap, MessagingChannel messagingChannel)
        {
            Resources = resourceLoader;

            MessageListeners = new Dictionary<Type, IMessageReceiver>();

            _messageTimer = new Stopwatch();

            _messageTargetMap = messageTargetMap;
            _messagingChannel = messagingChannel;
        }

        protected virtual void Initialize()
        {

        }

        public void SetScene(Scene scene)
        {
            Scene = scene;
            _cameras = Scene.World.GetEntities().With<Camera>().With<Transform>().AsSet();
        }

        public void AddListener(IMessageReceiver listener)
        {
            MessageListeners[listener.GetType()] = listener;
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

                using var stream = new MemoryStream();

                EventBasedNetListener listener = new EventBasedNetListener();
                NetManager client = new NetManager(listener);
                client.SimulatePacketLoss = true;
                client.SimulationPacketLossChance = 5;

                client.Start();
                client.Connect("localhost" /* host ip or name */, 9050 /* port */, "SomeConnectionKey" /* text key or NetDataWriter */);

                listener.PeerConnectedEvent += (netPeer) =>
                {
                    _messagingChannel.PeerAdded(netPeer);
                };

                listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) =>
                {
                    Utilties.Logging.Metrics.LogMetric("Client:Networking:RecievedMessages:Size", dataReader.UserDataSize, TimeSpan.FromSeconds(5));
                    MessageRecieved(new ArraySegment<byte>(dataReader.RawData, dataReader.UserDataOffset, dataReader.UserDataSize));
                    dataReader.Recycle();
                };

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
                            0.4f,
                            512f);
                        if (GraphicsDevice.IsClipSpaceYInverted)
                        {
                            _projectionMatrix *= Matrix4x4.CreateScale(1, -1, 1);
                        }

                        imGuiRenderer.WindowResized(_window.Width, _window.Height);
                    }

                    var frameTime = frameWatch.Elapsed.TotalSeconds;
                    Utilties.Logging.Metrics.LogMetric("Client:Frame:Time", frameTime, TimeSpan.FromSeconds(5));
                    frameTime = Math.Min(frameTime, 0.033333);
                    frameWatch.Restart();

                    client.PollEvents();

                    imGuiRenderer.Update((float)frameTime, InputTracker.LockMouse ? new EmptyInputSnapshot() : InputTracker.FrameSnapshot );

                    Scene?.Update(frameTime);

                    Tick?.Invoke();

                    _messagingChannel.SendBuffered();

                    CommandList.Begin();
                    CommandList.SetFramebuffer(MainSceneFramebuffer);
                    CommandList.ClearColorTarget(0, RgbaFloat.CornflowerBlue);
                    CommandList.ClearDepthStencil(1f);

                    if(CameraEntity.IsAlive && CameraTransform != null)
                    {
                        var context = new RenderingContext()
                        {
                            GraphicsDevice = GraphicsDevice,
                            CommandList = CommandList,
                            CameraTransform = CameraTransform,
                            ProjectionMatrix = _projectionMatrix
                        };
                        Scene?.Render(context);
                    }

                    var displaySize = ImGui.GetIO().DisplaySize;
                    ImGui.GetBackgroundDrawList().AddCircleFilled(displaySize / 2, 2, ImGui.GetColorU32(new Vector4(1, 1, 1, 1)));

                    imGuiRenderer.Render(GraphicsDevice, CommandList);

                    if (_mainSceneColourTexture != null && _mainSceneColourTexture.SampleCount != TextureSampleCount.Count1)
                    {
                        CommandList.ResolveTexture(_mainSceneColourTexture, GraphicsDevice.MainSwapchain.Framebuffer.ColorTargets.First().Target);
                    }

                    CommandList.End();
                    GraphicsDevice.WaitForIdle();
                    GraphicsDevice.SubmitCommands(CommandList);
                    GraphicsDevice.SwapBuffers(GraphicsDevice.MainSwapchain);

                    Resources.Update();
                }

                client.Stop();
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

        public void MessageRecieved(ArraySegment<byte> message)
        {
            using (var stream = new MemoryStream(message.Array, message.Offset, message.Count))
            {
                while(stream.Position < stream.Length)
                {
                    var target = _messageTargetMap.ReadType(stream);
                    var receiver = MessageListeners[target];
                    receiver.MessageReceived(stream);
                }
            }
        }
    }
}
