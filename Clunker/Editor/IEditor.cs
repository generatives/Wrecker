using DefaultEcs.System;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Editor
{
    public interface IEditor : ISystem<double>
    {
        string Name { get; }
        string Category { get; }
        char? HotKey { get; }
        bool IsActive { get; set; }
        void DrawWindow(double delta);
    }

    public abstract class Editor : IEditor
    {
        public abstract string Name { get; }
        public abstract string Category { get; }
        public virtual char? HotKey { get; } = null;
        public bool IsEnabled { get; set; } = true;
        public bool IsActive { get; set; } = false;

        public void DrawWindow(double delta)
        {
            var isActive = IsActive;
            IsActive = ImGui.Begin(Name, ref isActive);
            IsActive = isActive;

            if(IsActive)
            {
                DrawEditor(delta);
            }
        }

        public virtual void DrawEditor(double delta)
        {
        }

        public virtual void Update(double state)
        {
        }

        public virtual void Dispose()
        {
        }
    }
}
