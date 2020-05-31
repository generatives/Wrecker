using DefaultEcs;
using DynamicExpresso;
using ImGuiNET;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Clunker.Editor.EditorConsole
{
    public class EditorConsole : IEditor
    {
        private string _input = "";
        private bool _scrollOutputDown;

        private List<string> _outputs;
        private Interpreter _interpreter;

        private Stack<string> _inputs;
        private int _inputPosition;

        public EditorConsole(Scene scene)
        {
            _outputs = new List<string>();
            _inputs = new Stack<string>();
            _interpreter = new Interpreter();
            _interpreter.SetVariable("Scene", scene);
            _interpreter.SetVariable("this", new ExpandoObject());
        }

        public string Name => "Console";

        public string Category => "Console";

        public bool Active { get; set; }

        public void Run()
        {
            ImGui.Begin(Name);

            ImGui.BeginChild("Output", new System.Numerics.Vector2(0, -ImGui.GetTextLineHeightWithSpacing() - 8), false);
            foreach (var output in _outputs)
            {
                ImGui.Text(output);
            }
            if (_scrollOutputDown)
            {
                ImGui.SetScrollHereY();
                _scrollOutputDown = false;
            }
            ImGui.EndChild();

            //ImGui.Separator();

            if (ImGui.InputText("Input", ref _input, 100, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                try
                {
                    var output = _interpreter.Eval(_input);
                    _outputs.Add(output.ToString());
                }
                catch (Exception ex)
                {
                    _outputs.Add(ex.Message);
                }
                _input = "";
                _scrollOutputDown = true;
            }

            if (ImGui.IsItemHovered() || ((ImGui.IsAnyItemFocused() || ImGui.IsWindowFocused()) && !ImGui.IsAnyItemActive() && !ImGui.IsMouseClicked(0)))
                ImGui.SetKeyboardFocusHere(-1);

            ImGui.End();
        }
    }
}
