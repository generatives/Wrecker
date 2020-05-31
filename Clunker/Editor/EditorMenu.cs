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

        public EditorMenu(List<IEditor> editors)
        {
            Editors = editors;
        }

        public void Update(double state)
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
                            bool active = editor.Active;
                            ImGui.MenuItem(editor.Name, "", ref active, true);
                            editor.Active = active;
                        }
                        ImGui.EndMenu();
                    }
                }
                ImGui.EndMainMenuBar();
            }

            foreach (var editor in Editors.Where(e => e.Active))
            {
                editor.Run();
            }
        }

        public void Dispose()
        {

        }
    }
}
