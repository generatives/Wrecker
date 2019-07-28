using Clunker.Graphics;
using Clunker.Input;
using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
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
using ImGuiNET;
using Hyperion;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Clunker.Resources;

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
        public Camera Camera { get; private set; }
        private Scene NextScene { get; set; }
        public Scene CurrentScene { get; private set; }

        private List<IRenderer> _renderers;

        public RoundRobinWorkQueue WorkQueue { get; private set; }
        public DrivenWorkQueue BestEffortFrameQueue { get; private set; }

        public ResourceLoader Resources { get; private set; }

        public Serializer Serializer { get; private set; }

        public byte[] ShipBin;

        public ClunkerApp(ResourceLoader resourceLoader, Scene initialScene)
        {
            Resources = resourceLoader;
            Serializer = new Serializer(new SerializerOptions(false, true,
                new[]
                {
                    Surrogate.Create<Resource<Image<Rgba32>>, ResourceSurrogate<Image<Rgba32>>>(r => new ResourceSurrogate<Image<Rgba32>>() { Id = r.Id }, s => resourceLoader.LoadImage(s.Id))
                }));
            NextScene = initialScene;
            _renderers = new List<IRenderer>();
            WorkQueue = new RoundRobinWorkQueue(new ThreadedWorkQueue(), new ThreadedWorkQueue(), new ThreadedWorkQueue(), new ThreadedWorkQueue(), new ThreadedWorkQueue(), new ThreadedWorkQueue());
            BestEffortFrameQueue = new DrivenWorkQueue();
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

        public void AddRenderable(IRenderable renderable)
        {
            _renderers.ForEach(r => r.AddRenderable(renderable));
        }

        public void RemoveRenderable(IRenderable renderable)
        {
            _renderers.ForEach(r => r.RemoveRenderable(renderable));
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
                var targetFrameTime = 0.13f;
                while (_window.Exists)
                {
                    var inputSnapshot = _window.PumpEvents();

                    if (!_window.Exists) break;

                    InputTracker.UpdateFrameInput(_window, inputSnapshot);

                    if (InputTracker.WasKeyDowned(Key.Escape)) break;
                    if (InputTracker.WasKeyDowned(Key.Tab)) InputTracker.LockMouse = !InputTracker.LockMouse;
                    if (InputTracker.WasKeyDowned(Key.T)) { StackedTiming.Enabled = !StackedTiming.Enabled; Timing.Enabled = !Timing.Enabled; }

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
                        if(InputTracker.WasKeyDowned(Key.N))
                        {
                            using (var memoryStream = new MemoryStream(ShipBin))
                            {
                                var gameObject = Serializer.Deserialize<GameObject>(memoryStream);
                                CurrentScene.AddGameObject(gameObject);
                            }
                        }

                        StackedTiming.PushFrameTimer("Scene Update");
                        CurrentScene.Update(frameTime);
                        StackedTiming.PopFrameTimer();
                    }

                    _commandList.Begin();

                    _commandList.SetFramebuffer(_graphicsDevice.MainSwapchain.Framebuffer);
                    //commandList.ClearColorTarget(0, new RgbaFloat(25f / 255, 25f / 255, 112f / 255, 1.0f));
                    _commandList.ClearColorTarget(0, RgbaFloat.CornflowerBlue);
                    _commandList.ClearDepthStencil(1f);

                    StackedTiming.PushFrameTimer("Render");
                    if (Camera != null)
                    {
                        _renderers.ForEach(r => r.Render(Camera, _graphicsDevice, _commandList, RenderWireframes.SOLID));
                    }
                    StackedTiming.PopFrameTimer();

                    StackedTiming.Render(frameTime);
                    Timing.Render(frameTime);

                    imGuiRenderer.Render(_graphicsDevice, _commandList);

                    _commandList.End();
                    _graphicsDevice.SubmitCommands(_commandList);
                    _graphicsDevice.SwapBuffers(_graphicsDevice.MainSwapchain);
                    _graphicsDevice.WaitForIdle();

                    bool jobsRemain = true;
                    while (jobsRemain && frameWatch.Elapsed.TotalSeconds < targetFrameTime)
                    {
                        jobsRemain = BestEffortFrameQueue.ConsumeActions(1);
                    }
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
