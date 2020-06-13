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

namespace Clunker
{
    public class ClunkerApp
    {
        public event Action Started;
        public event Action Tick;

        private Sdl2Window _window;
        private bool _windowResized;
        private GraphicsDevice _graphicsDevice;
        private CommandList _commandList;
        private EntitySet _cameras;
        public Entity CameraEntity => _cameras.GetEntities().ToArray().FirstOrDefault();
        public Transform CameraTransform => CameraEntity.World == Scene.World ? CameraEntity.Get<Transform>() : null;
        private Scene Scene { get; set; }

        private List<IRenderer> _renderers;

        public ResourceLoader Resources { get; private set; }

        public ClunkerApp(ResourceLoader resourceLoader, Scene initialScene)
        {
            Resources = resourceLoader;
            Scene = initialScene;
            _renderers = new List<IRenderer>();
            _cameras = Scene.World.GetEntities().With<Camera>().With<Transform>().AsSet();
        }

        public void AddRenderer(IRenderer renderer)
        {
            _renderers.Add(renderer);
            _renderers = _renderers.OrderBy(r => r.Order).ToList();
            if(_graphicsDevice != null && _commandList != null && _window != null)
            {
                renderer.Initialize(_graphicsDevice, _commandList, _window.Width, _window.Height);
            }
        }

        public void RemoveRenderer(IRenderer renderer)
        {
            _renderers.Remove(renderer);
            _renderers = _renderers.OrderBy(r => r.Order).ToList();
        }

        public T GetRenderer<T>() where T : class, IRenderer
        {
            var type = typeof(T);
            return _renderers.FirstOrDefault(r => r is T) as T;
        }

        public Task Start(WindowCreateInfo wci, GraphicsDeviceOptions graphicsDeviceOptions)
        {
            return Task.Factory.StartNew(() =>
            {
                AddRenderer(new Renderer());

                _window = VeldridStartup.CreateWindow(ref wci);
                //Window.CursorVisible = false;
                //Window.SetMousePosition(Window.Width / 2, Window.Height / 2);
                _window.Resized += () => _windowResized = true;
                _graphicsDevice = VeldridStartup.CreateGraphicsDevice(_window, graphicsDeviceOptions, GraphicsBackend.Vulkan);
                _commandList = _graphicsDevice.ResourceFactory.CreateCommandList();
                _renderers.ForEach(r => r.Initialize(_graphicsDevice, _commandList, _window.Width, _window.Height));
                var imGuiRenderer = new ImGuiRenderer(_graphicsDevice, _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height);

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
                        _graphicsDevice.ResizeMainWindow((uint)_window.Width, (uint)_window.Height);
                        _renderers.ForEach(r => r.WindowResized(_window.Width, _window.Height));
                        imGuiRenderer.WindowResized(_window.Width, _window.Height);
                    }

                    var frameTime = frameWatch.Elapsed.TotalSeconds;
                    frameWatch.Restart();

                    imGuiRenderer.Update((float)frameTime, InputTracker.LockMouse ? new EmptyInputSnapshot() : InputTracker.FrameSnapshot );

                    Scene.Update(frameTime);

                    Tick?.Invoke();

                    _commandList.Begin();
                    _commandList.SetFramebuffer(_graphicsDevice.MainSwapchain.Framebuffer);
                    //commandList.ClearColorTarget(0, new RgbaFloat(25f / 255, 25f / 255, 112f / 255, 1.0f));
                    _commandList.ClearColorTarget(0, RgbaFloat.CornflowerBlue);
                    _commandList.ClearDepthStencil(1f);

                    _renderers.ForEach(r => r.Render(Scene, CameraTransform, _graphicsDevice, _commandList, RenderWireframes.NO));

                    var displaySize = ImGui.GetIO().DisplaySize;
                    ImGui.GetBackgroundDrawList().AddCircleFilled(displaySize / 2, 2, ImGui.GetColorU32(new Vector4(1, 1, 1, 1)));
                    imGuiRenderer.Render(_graphicsDevice, _commandList);

                    _commandList.End();
                    _graphicsDevice.SubmitCommands(_commandList);
                    _graphicsDevice.SwapBuffers(_graphicsDevice.MainSwapchain);

                    _graphicsDevice.WaitForIdle();
                }
            }, TaskCreationOptions.LongRunning);
        }
    }
}
