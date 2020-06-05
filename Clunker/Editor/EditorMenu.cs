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

            if (ImGui.BeginMainMenuBar())
            {
                foreach (var group in groupedEditors)
                {
                    if (ImGui.BeginMenu(group.Key))
                    {
                        foreach (var editor in group)
                        {
                            bool active = editor.IsActive;
                            ImGui.MenuItem(editor.Name, "", ref active, true);
                            editor.IsActive = active;
                        }
                        ImGui.EndMenu();
                    }
                }
                ImGui.EndMainMenuBar();
            }

            foreach(var editor in Editors.Where(e => e.IsActive))
            {
                editor.DrawEditor(delta);
            }
        }

        public void Dispose()
        {

        }
    }
}
