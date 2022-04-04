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
    public class ClunkerClientRunner
    {
        public event Action Started;

        private Sdl2Window _window;
        private bool _windowResized;
        public int WindowWidth => _window.Width;
        public int WindowHeight => _window.Height;
        public GraphicsDevice GraphicsDevice { get; private set; }
        public CommandList CommandList { get; private set; }
        public Framebuffer MainSceneFramebuffer { get; private set; }

        private EntitySet _cameras;
        public Entity CameraEntity => _cameras.GetEntities().ToArray().FirstOrDefault();
        public Transform CameraTransform => CameraEntity.World == Scene.World ? CameraEntity.Get<Transform>() : null;
        public Scene Scene { get; private set; }

        private Matrix4x4 _projectionMatrix;

        public ResourceLoader Resources { get; private set; }

        public ClunkerClientRunner(ResourceLoader resourceLoader, MessageTargetMap messageTargetMap, MessagingChannel messagingChannel)
        {
            Resources = resourceLoader;
        }

        protected virtual void Initialize()
        {

        }

        public void SetScene(Scene scene)
        {
            Scene = scene;
            _cameras = Scene.World.GetEntities().With<Camera>().With<Transform>().AsSet();
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

                MainSceneFramebuffer = GraphicsDevice.SwapchainFramebuffer;

                Initialize();

                _windowResized = true;

                Started?.Invoke();

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
                            1024f);
                        if (GraphicsDevice.IsClipSpaceYInverted)
                        {
                            _projectionMatrix *= Matrix4x4.CreateScale(1, -1, 1);
                        }

                        Scene.World.Publish(new WindowResized() { Width = _window.Width, Height = _window.Height });
                    }

                    var frameTime = frameWatch.Elapsed.TotalSeconds;
                    Utilties.Logging.Metrics.LogMetric("Client:Frame:Time", frameTime, TimeSpan.FromSeconds(5));
                    Utilties.Logging.Metrics.LogMetric("Client:Frame:Rate", 1f / frameTime, TimeSpan.FromSeconds(5));
                    frameTime = Math.Min(frameTime, 0.033333);
                    frameWatch.Restart();

                    Scene?.Update(frameTime);

                    CommandList.Begin();
                    CommandList.SetFramebuffer(MainSceneFramebuffer);
                    CommandList.ClearColorTarget(0, RgbaFloat.CornflowerBlue);
                    CommandList.ClearDepthStencil(1f);

                    var displaySize = ImGui.GetIO().DisplaySize;
                    ImGui.GetBackgroundDrawList().AddCircleFilled(displaySize / 2, 2, ImGui.GetColorU32(new Vector4(1, 1, 1, 1)));

                    if (CameraEntity.IsAlive && CameraTransform != null)
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
                    CommandList.End();
                    GraphicsDevice.SubmitCommands(CommandList);
                    GraphicsDevice.SwapBuffers(GraphicsDevice.MainSwapchain);

                    Resources.Update();
                }

            }, TaskCreationOptions.LongRunning);
        }
    }
}
