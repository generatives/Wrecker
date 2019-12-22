using Clunker.SceneGraph;
using Clunker.SceneGraph.ComponentInterfaces;
using DynamicExpresso;
using ImGuiNET;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace Clunker.Editor.Console
{
    public class Console : Component, IUpdateable, IComponentEventListener
    {
        private string _input = "";
        private bool _scrollOutputDown;

        private List<string> _outputs;
        private Interpreter _interpreter;

        private Stack<string> _inputs;
        private int _inputPosition;

        public Console()
        {
            _outputs = new List<string>();
            _inputs = new Stack<string>();
            _interpreter = new Interpreter();
            _interpreter.SetVariable("Scene", default(Scene));
            _interpreter.SetVariable("this", new ExpandoObject());
        }

        public void ComponentStarted()
        {
            _interpreter.SetVariable("Scene", GameObject.CurrentScene);
        }

        public void ComponentStopped()
        {
            _interpreter.SetVariable("Scene", default(Scene));
        }

        public void Update(float time)
        {
            ImGui.Begin("Console");

            ImGui.BeginChild("Output", new System.Numerics.Vector2(0, -ImGui.GetTextLineHeightWithSpacing() - 8), false);
            foreach(var output in _outputs)
            {
                ImGui.Text(output);
            }
            if(_scrollOutputDown)
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
                    if(output is string)
                    {
                    }
                    else if(output is IEnumerable)
                    {
                        output = string.Join(',', (output as IEnumerable));
                    }
                    _outputs.Add(output.ToString());
                }
                catch(Exception ex)
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
