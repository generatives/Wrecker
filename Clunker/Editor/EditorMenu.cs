using DefaultEcs.System;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clunker.Editor
{
    public class EditorMenu : ISystem<double>
    {
        public List<IEditor> Editors { get; private set; }
        public bool IsEnabled { get; set; } = true;

        public EditorMenu(Scene scene, List<IEditor> editors)
        {
            Editors = editors;
            scene.LogicSystems.AddRange(editors);
        }

        public void Update(double delta)
        {
            var groupedEditors = Editors.GroupBy(e => e.Category);

            foreach(var editor in Editors.Where(e => e.HotKey.HasValue))
            {
                if(ImGui.IsKeyDown((int)Veldrid.Key.ShiftLeft) &&
                    ImGui.IsKeyDown((int)Veldrid.Key.ControlLeft) &&
                    ImGui.IsKeyPressed((int)Enum.Parse(typeof(Veldrid.Key), editor.HotKey.Value.ToString().ToUpper())))
                {
                    editor.IsActive = !editor.IsActive;
                }
            }

            if (ImGui.BeginMainMenuBar())
            {
                foreach (var group in groupedEditors)
                {
                    if (ImGui.BeginMenu(group.Key))
                    {
                        foreach (var editor in group)
                        {
                            bool active = editor.IsActive;
                            var shortcutStr = editor.HotKey.HasValue ? "Ctrl+Shft+" + editor.HotKey.Value : "";
                            ImGui.MenuItem(editor.Name, shortcutStr, ref active, true);
                            editor.IsActive = active;
                        }
                        ImGui.EndMenu();
                    }
                }
                ImGui.EndMainMenuBar();
            }

            foreach(var editor in Editors.Where(e => e.IsActive))
            {
                editor.DrawWindow(delta);
            }
        }

        public void Dispose()
        {

        }
    }
}
