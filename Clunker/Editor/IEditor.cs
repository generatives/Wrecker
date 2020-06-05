using DefaultEcs.System;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Editor
{
    public interface IEditor : ISystem<double>
    {
        string Name { get; }
        string Category { get; }
        bool IsActive { get; set; }
        void DrawEditor(double delta);
    }

    public abstract class Editor : IEditor
    {
        public abstract string Name { get; }
        public abstract string Category { get; }
        public bool IsEnabled { get; set; } = true;
        public bool IsActive { get; set; } = false;

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
