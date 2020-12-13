using DefaultEcs.System;
using ImGuiNET;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clunker.Editor
{
    public class EditorMenu : ISystem<double>
    {
        private (string Name, List<IEditor> Editors) _currentEditorSet;
        private ConcurrentBag<(string Name, List<IEditor> Editors)> _editorSets;
        public bool IsEnabled { get; set; } = true;

        public EditorMenu()
        {
            _editorSets = new ConcurrentBag<(string Name, List<IEditor> Editors)>();
        }

        public void AddEditorSet(string name, List<IEditor> editors)
        {
            _editorSets.Add((name, editors));
            if (_currentEditorSet == default)
            {
                _currentEditorSet = (name, editors);
            }
        }

        public void Update(double delta)
        {
            if(_editorSets.Any())
            {
                foreach (var editor in _currentEditorSet.Editors.Where(e => e.HotKey.HasValue))
                {
                    if (ImGui.IsKeyDown((int)Veldrid.Key.ShiftLeft) &&
                        ImGui.IsKeyDown((int)Veldrid.Key.ControlLeft) &&
                        ImGui.IsKeyPressed((int)Enum.Parse(typeof(Veldrid.Key), editor.HotKey.Value.ToString().ToUpper())))
                    {
                        editor.IsActive = !editor.IsActive;
                    }
                }

                if (ImGui.BeginMainMenuBar())
                {
                    if (ImGui.BeginMenu($"Editor Sets ({_currentEditorSet.Name})"))
                    {
                        foreach (var editorSet in _editorSets)
                        {
                            var active = _currentEditorSet.Name == editorSet.Name;
                            ImGui.MenuItem(editorSet.Name, "", ref active, true);
                            if (active)
                            {
                                _currentEditorSet = editorSet;
                            }
                        }
                        ImGui.EndMenu();
                    }

                    foreach (var group in _currentEditorSet.Editors.GroupBy(e => e.Category))
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

                foreach (var editor in _currentEditorSet.Editors.Where(e => e.IsActive))
                {
                    editor.DrawWindow(delta);
                }
            }
        }

        public void Dispose()
        {

        }
    }
}
