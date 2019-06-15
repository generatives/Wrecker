using Clunker.Graphics;
using Clunker.Input;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentsInterfaces;
using Clunker.SceneGraph.SceneSystemInterfaces;
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
using Clunker.Diagnostics;
using Clunker.Runtime;

namespace Clunker
{
    public class ClunkerApp
    {
        private Sdl2Window _window;
        private bool _windowResized;
        private GraphicsDevice _graphicsDevice;
        private CommandList _commandList;
        public Camera Camera { get; private set; }
        private Scene NextScene { get; set; }
        public Scene CurrentScene { get; private set; }

        private List<IRenderer> _renderers;

        public RoundRobinMetaQueue WorkQueue { get; private set; }

        public event Action Started;
        public event Action Tick;

        public ClunkerApp(Scene initialScene)
        {
            NextScene = initialScene;
            _renderers = new List<IRenderer>();
            WorkQueue = new RoundRobinMetaQueue(new ThreadedWorkMetaQueue(), new ThreadedWorkMetaQueue(), new ThreadedWorkMetaQueue(), new ThreadedWorkMetaQueue());
        }

        public void AddRenderer(IRenderer renderer)
        {
            _renderers.Add(renderer);
            if(_graphicsDevice != null && _commandList != null && _window != null)
            {
                renderer.Initialize(_graphicsDevice, _commandList, _window.Width, _window.Height);
            }
        }

        public void RemoveRenderer(IRenderer renderer)
        {
            _renderers.Remove(renderer);
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
                AddRenderer(new Renderer() { RenderWireframes = false });

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

                    if (InputTracker.WasKeyDowned(Key.Escape)) break;
                    if (InputTracker.WasKeyDowned(Key.Tab)) InputTracker.LockMouse = !InputTracker.LockMouse;

                    if (_windowResized)
                    {
                        _windowResized = false;
                        _graphicsDevice.ResizeMainWindow((uint)_window.Width, (uint)_window.Height);
                        _renderers.ForEach(r => r.WindowResized(_window.Width, _window.Height));
                        imGuiRenderer.WindowResized(_window.Width, _window.Height);
                    }

                    var frameTime = (float)frameWatch.Elapsed.TotalSeconds;
                    frameWatch.Restart();

                    imGuiRenderer.Update(frameTime, InputTracker.FrameSnapshot);

                    Tick?.Invoke();
                    if(NextScene != null)
                    {
                        if (CurrentScene != null)
                        {
                            CurrentScene.SceneStopped();
                        }
                        CurrentScene = NextScene;
                        NextScene = null;
                        CurrentScene.SceneStarted(this);
                    }
                    if (CurrentScene != null)
                    {
                        Timing.PushFrameTimer("Scene Update");
                        CurrentScene.Update(frameTime);
                        Timing.PopFrameTimer();
                        CurrentScene.RenderUpdate(frameTime);
                    }

                    Timing.PushFrameTimer("Render");
                    if (Camera != null)
                    {
                        _renderers.ForEach(r => r.Render(Camera, _graphicsDevice, _commandList));
                    }
                    Timing.PopFrameTimer();

                    Timing.Render(frameTime);

                    imGuiRenderer.Render(_graphicsDevice, _commandList);

                    _commandList.End();
                    _graphicsDevice.SubmitCommands(_commandList);
                    _graphicsDevice.SwapBuffers(_graphicsDevice.MainSwapchain);
                    _graphicsDevice.WaitForIdle();
                }
            }, TaskCreationOptions.LongRunning);
        }

        internal void CameraCreated(Camera camera)
        {
            if (Camera == null)
            {
                Camera = camera;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("A camera already exists, ignoring the new camera");
            }
        }
    }
}
