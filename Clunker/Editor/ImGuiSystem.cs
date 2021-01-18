using Clunker.ECS;
using Clunker.Graphics;
using Clunker.Graphics.Systems;
using Clunker.Input;
using DefaultEcs;
using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Clunker.Editor
{
    public class ImGuiSystem : IPreSystem<double>, IRendererSystem
    {
        public bool IsEnabled { get; set; } = true;

        ImGuiRenderer _imGuiRenderer;

        public ImGuiSystem(GraphicsDevice graphicsDevice, Framebuffer mainSceneFramebuffer, int windowWidth, int windowHeight)
        {
            _imGuiRenderer = new ImGuiRenderer(graphicsDevice, mainSceneFramebuffer.OutputDescription, windowWidth, windowHeight);
        }

        public void CreateSharedResources(ResourceCreationContext context)
        {
        }

        public void CreateResources(ResourceCreationContext context)
        {
        }

        [Subscribe]
        public void On(in WindowResized request)
        {
            _imGuiRenderer.WindowResized(request.Width, request.Height);
        }

        public void PreUpdate(double frameTime)
        {
            _imGuiRenderer.Update((float)frameTime, InputTracker.LockMouse ? new EmptyInputSnapshot() : InputTracker.FrameSnapshot);
        }

        public void Update(RenderingContext context)
        {
            _imGuiRenderer.Render(context.GraphicsDevice, context.CommandList);
        }

        public void Dispose()
        {
            _imGuiRenderer.Dispose();
        }
    }
}
